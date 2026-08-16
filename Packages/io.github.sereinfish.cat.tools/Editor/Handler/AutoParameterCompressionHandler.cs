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

            // 名称前缀：每个压缩层会追加 /{index} 后缀，保证多层之间互不覆盖
            var random = StringHelper.GetRandomString();
            var syncSignalBaseName = string.IsNullOrEmpty(entity.syncSignalParameterName)
                ? $"AutoParameterCompression/syncSignalParameter/{random}"
                : entity.syncSignalParameterName;
            var dataExchangeBaseName = string.IsNullOrEmpty(entity.dataExchangeParameterName)
                ? $"AutoParameterCompression/dataExchangeParameter/{random}"
                : entity.dataExchangeParameterName;

            // 从 ExpressionParameters 中提取参数类型，去重并按 Float/Int/Bool 分类，跳过不存在的参数
            var expressionBuilder = context.VRChatAvatarDescriptor().ExpressionParameters();
            var floatNames = new List<string>();
            var intNames = new List<string>();
            var boolNames = new List<string>();
            var seen = new HashSet<string>();
            foreach (var parameterName in entity.asyncSyncParameterNames)
            {
                if (string.IsNullOrEmpty(parameterName)) continue;
                if (!seen.Add(parameterName)) continue;

                var contains = false;
                expressionBuilder.Contains(parameterName, ref contains);
                if (!contains)
                {
                    Debug.LogWarning($"AutoParameterCompression {entity.name}: 参数 {parameterName} 不存在于 ExpressionParameters 中，已跳过");
                    continue;
                }

                VRCExpressionParameters.ValueType? type = null;
                expressionBuilder.Find(parameterName, p => type = p.valueType);
                switch (type!.Value)
                {
                    case VRCExpressionParameters.ValueType.Float:
                        floatNames.Add(parameterName);
                        break;
                    case VRCExpressionParameters.ValueType.Int:
                        intNames.Add(parameterName);
                        break;
                    default:
                        boolNames.Add(parameterName);
                        break;
                }
            }

            if (floatNames.Count + intNames.Count + boolNames.Count == 0)
            {
                Debug.LogWarning($"AutoParameterCompression {entity.name}: 没有找到任何有效参数，将不会执行任何操作");
                return;
            }

            var groups = AutoParameterCompressionBuilder.GroupParameters(floatNames, intNames, boolNames);
            AutoParameterCompressionBuilder.Build(context, fxController, groups, syncSignalBaseName, dataExchangeBaseName);
        }
    }
}
