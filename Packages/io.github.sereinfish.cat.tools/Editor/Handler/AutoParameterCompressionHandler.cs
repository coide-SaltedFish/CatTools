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
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.Parameter.Descriptor;
using io.github.sereinfish.cat.tools.editor.utils;
using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    /// <summary>
    /// 自动类型参数压缩处理器
    /// 由 PackageUtils.GetBuildHandlers() 反射自动发现，在 Optimizing 阶段执行。
    /// </summary>
    public class AutoParameterCompressionHandler : ComponentHandler<AutoParameterCompression>
    {
        public override void Execute(BuildContext context, AutoParameterCompression entity)
        {
            // 入口校验
            if (entity.asyncSyncParameterNames == null || entity.asyncSyncParameterNames.Count == 0)
            {
                Debug.LogWarning($"AutoParameterCompression {entity.name}: asyncSyncParameterNames 为空，将不会执行任何操作");
                return;
            }

            var fxController = context.GetAnimatorController(VRCAvatarDescriptor.AnimLayerType.FX);
            if (fxController == null)
            {
                Debug.LogWarning($"AutoParameterCompression {entity.name}: FX 动画控制器不存在，将不会执行任何操作");
                return;
            }
            
            // 4. 参数名称为空时生成默认名称
            var syncSignalName = string.IsNullOrEmpty(entity.syncSignalParameterName)
                ? $"AutoParameterCompression/syncSignalParameter/{StringHelper.GetRandomString()}"
                : entity.syncSignalParameterName;
            var dataExchangeName = string.IsNullOrEmpty(entity.dataExchangeParameterName)
                ? $"AutoParameterCompression/dataExchangeParameter/{StringHelper.GetRandomString()}"
                : entity.dataExchangeParameterName;

            // 1. 从 ExpressionParameters 中提取参数类型，跳过不存在的参数
            var expressionBuilder = context.VRChatAvatarDescriptor().ExpressionParameters();
            var parameterNames = new List<string>();
            var parameterTypes = new List<VRCExpressionParameters.ValueType>();
            var maxType = VRCExpressionParameters.ValueType.Bool;
            foreach (var parameterName in entity.asyncSyncParameterNames)
            {
                if (string.IsNullOrEmpty(parameterName)) continue;

                var contains = false;
                expressionBuilder.Contains(parameterName, ref contains);
                if (!contains)
                {
                    Debug.LogWarning($"AutoParameterCompression {entity.name}: 参数 {parameterName} 不存在于 ExpressionParameters 中，已跳过");
                    continue;
                }

                VRCExpressionParameters.ValueType? type = null;
                expressionBuilder.Find(parameterName, p => type = p.valueType);
                parameterNames.Add(parameterName);
                parameterTypes.Add(type!.Value);
                // bool 最小，int 其次，float 最大
                if (type.Value > maxType) maxType = type.Value;
            }
            if (parameterNames.Count == 0)
            {
                Debug.LogWarning($"AutoParameterCompression {entity.name}: 没有找到任何有效参数，将不会执行任何操作");
                return;
            }

            // 2. 同步信号位宽：2^bitWidth > 参数数量 且最接近（参考 CatSyncDance.GetControllerParameterWidth）
            var bitWidth = 1;
            while ((1 << bitWidth) <= parameterNames.Count) bitWidth++;
            if (bitWidth > 8)
            {
                Debug.LogWarning($"AutoParameterCompression {entity.name}: 参数数量 {parameterNames.Count} 超出 8 位可表示范围，将不会执行任何操作");
                return;
            }

            AutoParameterCompressionBuilder.Build(context, fxController, parameterNames, parameterTypes,
                syncSignalName, bitWidth, dataExchangeName, entity.syncInterval, maxType);
        }
    }
}
