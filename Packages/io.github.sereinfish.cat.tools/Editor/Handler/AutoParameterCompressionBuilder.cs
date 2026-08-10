#region LICENSE
// /*
//  * CatTools - A simple Unity plugin to assist in creating VRChat Avatars
//  * Copyright (C) 2025  一只大猫条
//  *
//  * This program is free software: you can redistribute it and/or modify
//  * it under the terms of the GNU General Public License as published by
//  * the Free Software Foundation, either version 3 of the License, or
//  * (at your option) any later version.
//  *
//  * This program is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  * GNU General Public License for more details.
//  *
//  * You should have received a copy of the GNU General Public License
//  * along with this program.  If not, see <https://www.gnu.org/licenses/>.
//  */
#endregion

using System.Collections.Generic;
using io.github.sereinfish.cat.tools.editor.animator.builder;
using io.github.sereinfish.cat.tools.editor.Conditions.Build;
using io.github.sereinfish.cat.tools.editor.utils;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using nadena.dev.ndmf.vrchat;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using PropertyName = io.github.sereinfish.cat.tools.editor.animator.builder.PropertyName;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    /// <summary>
    /// 自动类型参数压缩构建器（核心逻辑）
    /// 与 AutoParameterCompression 组件解耦，方便后续扩展。
    /// </summary>
    public static class AutoParameterCompressionBuilder
    {
        public static void Build(BuildContext context, VirtualAnimatorController fxController,
            IReadOnlyList<string> parameterNames, IReadOnlyList<VRCExpressionParameters.ValueType> parameterTypes,
            string syncSignalParameterName, int syncSignalBitWidth,
            string dataExchangeParameterName, float syncInterval, VRCExpressionParameters.ValueType dataExchangeType)
        {
            // 6. 给 FX 控制器注册 IsLocal 参数
            fxController.AddParameterIfNot(VRCSdkAnimatorParameters.IsLocal.Name, false);

            // 2. 注册同步信号参数（动态 Int，位宽保证能表示全部索引）
            DynamicIntParameterHandler.CreateDynamicInt(context, fxController, syncSignalParameterName, null,
                syncSignalBitWidth, false, true, 0, true, true, false);

            // 3. 按最大类型注册数据交换参数（ExpressionParameters）
            context.VRChatAvatarDescriptor().ExpressionParameters()
                .Add(dataExchangeParameterName, dataExchangeType, 0, false, true)
                .Build();
            //  FX 控制器
            fxController.AddParameterIfNot(new AnimatorControllerParameter
            {
                name = dataExchangeParameterName,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0f,
                defaultBool = false,
                defaultInt = 0
            });

            // 注册目标参数到 FX 控制器（供驱动 Copy 与条件使用）
            for (var i = 0; i < parameterNames.Count; i++)
            {
                fxController.AddParameterIfNot(new AnimatorControllerParameter
                {
                    name = parameterNames[i],
                    type = ToAnimatorType(parameterTypes[i]),
                    defaultFloat = 0f,
                    defaultBool = false,
                    defaultInt = 0
                });
            }

            // 7. 在 FX 控制器中创建图层并构建 R/W 状态机
            var layer = fxController.AddLayer($"AutoParameterCompression/{StringHelper.GetRandomString()}");
            BuildSyncLayer(context, fxController, layer, parameterNames, syncSignalParameterName, dataExchangeParameterName, syncInterval);

            // 8. 构建完成后，将目标参数的 networkSynced 置为 false（改由数据交换通道统一同步）
            //    必须重新获取 ExpressionParametersBuilder，避免使用过期列表覆盖已注册的 dataExchange/同步信号位参数
            var expressionBuilder = context.VRChatAvatarDescriptor().ExpressionParameters();
            foreach (var parameterName in parameterNames)
            {
                VRCExpressionParameters.Parameter found = null;
                expressionBuilder.Find(parameterName, p => found = p);
                if (found == null) continue;
                if (!found.networkSynced) continue;
                // Find 按值传递 struct，无法原地修改，需 Remove 后重新 Add（保留 valueType/defaultValue/saved）
                expressionBuilder.Remove(parameterName);
                var p = found;
                expressionBuilder.Add(parameterName, p.valueType, p.defaultValue, p.saved, false);
            }
            expressionBuilder.Build();
        }

        private static void BuildSyncLayer(BuildContext context, VirtualAnimatorController controller, VirtualLayer layer,
            IReadOnlyList<string> parameterNames, string syncSignalParameterName, string dataExchangeParameterName, float syncInterval)
        {
            const int stateHeight = 60;
            var stateY = -(stateHeight * parameterNames.Count / 2);
            VirtualState defaultState = null;

            for (var i = 0; i < parameterNames.Count; i++)
            {
                var targetName = parameterNames[i];
                var index = i;

                // 读取状态 R_i：将 dataExchangeParameterName 的值 Copy 给目标参数
                // 过渡条件：IsLocal == false 且 syncSignalParameterName == index
                var stateR = layer.AddState($"R_{i}", position: new Vector3(300, stateY + i * stateHeight));
                stateR.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
                {
                    driver.AddParameterDriverCopy(targetName, dataExchangeParameterName);
                });
                ConditionsBuilder.Create()
                    .If(VRCSdkAnimatorParameters.IsLocal.Name, false)
                    .Greater(syncSignalParameterName, index - 0.1f)
                    .Less(syncSignalParameterName, index + 0.1f)
                    .Build()
                    .CreateAnyStateConditionsTransition(context, controller, layer, stateR);

                // 写入状态 W_i：将目标参数的值 Copy 给 dataExchangeParameterName
                // 过渡条件：IsLocal == true 且 syncSignalParameterName == index
                var clip = AnimationBuilder.Create()
                    .FrameRate(100)
                    .SetCurve($"CatToolsEmptyObject_{StringHelper.GetRandomString()}", typeof(GameObject), PropertyName.ObjIsActive, curveBuilder =>
                    {
                        curveBuilder.AddKey(new Keyframe(0f, 0));
                        curveBuilder.AddKey(new Keyframe(syncInterval, 0));
                    })
                    .Build();
                var stateW = layer.AddState($"W_{i}", clip.ToVirtualMotion(context), position: new Vector3(-300, stateY + i * stateHeight));
                var i1 = i;
                stateW.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
                {
                    driver.AddParameterDriverCopy(dataExchangeParameterName, targetName);
                    if (i1 == parameterNames.Count - 1)
                    {
                        driver.AddParameterDriverSet(syncSignalParameterName, 0);
                    }
                    else
                    {
                        driver.AddParameterDriverAdd(syncSignalParameterName, 1);
                    }
                });
                ConditionsBuilder.Create()
                    .If(VRCSdkAnimatorParameters.IsLocal.Name, true)
                    .Greater(syncSignalParameterName, index - 0.1f)
                    .Less(syncSignalParameterName, index + 0.1f)
                    .Build()
                    .CreateAnyStateConditionsTransition(context, controller, layer, stateW, exitTime: 1);

                if (i == 0) defaultState = stateW;
            }

            // 默认状态为 W_0
            layer.GetStateMachine().DefaultState = defaultState;
        }

        /// <summary>
        /// 将表达式参数类型映射为 Animator 控制器参数类型
        /// </summary>
        private static AnimatorControllerParameterType ToAnimatorType(VRCExpressionParameters.ValueType type)
        {
            switch (type)
            {
                case VRCExpressionParameters.ValueType.Bool: return AnimatorControllerParameterType.Bool;
                case VRCExpressionParameters.ValueType.Int: return AnimatorControllerParameterType.Int;
                default: return AnimatorControllerParameterType.Float;
            }
        }
    }
}
