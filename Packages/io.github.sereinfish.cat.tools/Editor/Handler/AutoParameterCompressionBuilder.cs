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
    /// 将参数按 Float/Int/Bool 分组打包为若干压缩层（每层最多 10 个参数），
    /// 每层拥有独立的数据交换参数与同步信号，避免类型不兼容与参数覆盖。
    /// </summary>
    public static class AutoParameterCompressionBuilder
    {
        /// <summary>固定同步间隔（秒）</summary>
        public const float SyncIntervalSeconds = 0.1f;

        /// <summary>每个压缩层最多处理的参数数量（10 个 × 0.1s = 最大 1s 延迟）</summary>
        public const int MaxParametersPerLayer = 10;

        /// <summary>
        /// 一个压缩层：包含最多 <see cref="MaxParametersPerLayer"/> 个参数。
        /// Float 与 Int 不混合；Bool 可填充 Float/Int 层的空位。
        /// </summary>
        public sealed class ParameterGroup
        {
            private readonly List<string> _parameterNames = new();
            private readonly List<VRCExpressionParameters.ValueType> _parameterTypes = new();

            public IReadOnlyList<string> ParameterNames => _parameterNames;
            public IReadOnlyList<VRCExpressionParameters.ValueType> ParameterTypes => _parameterTypes;

            /// <summary>该层数据交换参数类型（Float 层为 Float，Int 层为 Int，纯 Bool 层为 Bool）</summary>
            public VRCExpressionParameters.ValueType ExchangeType;

            public int Count => _parameterNames.Count;

            /// <summary>非 Bool 参数数量（Float/Int 层的核心参数）</summary>
            public int NonBoolCount { get; private set; }

            /// <summary>
            /// 是否过小、不值得单独成层：仅 1 个 int/float（≤3 个参数）或纯 Bool 层 ≤3 个参数时放弃。
            /// </summary>
            public bool ShouldAbandon => Count <= 3 && NonBoolCount <= 1;

            internal void Add(string name, VRCExpressionParameters.ValueType type)
            {
                _parameterNames.Add(name);
                _parameterTypes.Add(type);
                if (type != VRCExpressionParameters.ValueType.Bool) NonBoolCount++;
            }
        }

        /// <summary>
        /// 将已按类型分类的参数打包为若干压缩层。
        /// Float 与 Int 各自独立成层（互不混合），Bool 填充前两者的剩余空位，最后剩余 Bool 独立成层。
        /// </summary>
        public static List<ParameterGroup> GroupParameters(
            IReadOnlyList<string> floatParameterNames,
            IReadOnlyList<string> intParameterNames,
            IReadOnlyList<string> boolParameterNames)
        {
            var floatNames = floatParameterNames ?? new List<string>();
            var intNames = intParameterNames ?? new List<string>();
            var boolNames = boolParameterNames ?? new List<string>();

            var groups = new List<ParameterGroup>();

            // Float 与 Int 各自独立成层（互不混合）
            PackPrimary(groups, floatNames, VRCExpressionParameters.ValueType.Float);
            PackPrimary(groups, intNames, VRCExpressionParameters.ValueType.Int);

            // Bool 填充 Float/Int 层的剩余空位，达到最大利用率
            var boolIndex = 0;
            foreach (var group in groups)
            {
                while (boolIndex < boolNames.Count && group.Count < MaxParametersPerLayer)
                {
                    group.Add(boolNames[boolIndex++], VRCExpressionParameters.ValueType.Bool);
                }
            }

            // 剩余 Bool 独立成层（数据交换类型为 Bool）
            while (boolIndex < boolNames.Count)
            {
                var group = new ParameterGroup { ExchangeType = VRCExpressionParameters.ValueType.Bool };
                while (boolIndex < boolNames.Count && group.Count < MaxParametersPerLayer)
                {
                    group.Add(boolNames[boolIndex++], VRCExpressionParameters.ValueType.Bool);
                }
                groups.Add(group);
            }

            return groups;
        }

        private static void PackPrimary(List<ParameterGroup> groups, IReadOnlyList<string> names,
            VRCExpressionParameters.ValueType type)
        {
            if (names == null) return;
            for (var i = 0; i < names.Count; i += MaxParametersPerLayer)
            {
                var group = new ParameterGroup { ExchangeType = type };
                for (var j = i; j < names.Count && group.Count < MaxParametersPerLayer; j++)
                {
                    group.Add(names[j], type);
                }
                groups.Add(group);
            }
        }

        /// <summary>
        /// 构建所有压缩层：注册 IsLocal、逐层注册同步信号/数据交换/目标参数并构建 R/W 状态机，
        /// 最后将目标参数 networkSynced 置为 false（改由数据交换通道统一同步）。
        /// </summary>
        public static void Build(BuildContext context, VirtualAnimatorController fxController,
            IReadOnlyList<ParameterGroup> groups, string syncSignalBaseName, string dataExchangeBaseName)
        {
            if (groups == null || groups.Count == 0) return;

            // 注册 IsLocal 参数（幂等）
            fxController.AddParameterIfNot(VRCSdkAnimatorParameters.IsLocal.Name, false);

            var builtGroups = new List<ParameterGroup>();
            for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                var group = groups[groupIndex];
                // 过小的层放弃处理：参数保持 ExpressionParameters 中的默认同步设置（networkSynced 不动）
                if (group.ShouldAbandon)
                {
                    Debug.LogWarning($"AutoParameterCompression: 压缩层仅含 {group.Count} 个参数（int/float {group.NonBoolCount} 个），过小，已放弃该层处理，参数将保持默认同步设置：{string.Join(", ", group.ParameterNames)}");
                    continue;
                }

                // 每个压缩层使用独立的同步信号与数据交换参数
                var syncSignalName = $"{syncSignalBaseName}/{groupIndex}";
                var dataExchangeName = $"{dataExchangeBaseName}/{groupIndex}";
                BuildGroup(context, fxController, group, syncSignalName, dataExchangeName);
                builtGroups.Add(group);
            }

            if (builtGroups.Count == 0) return;

            // 构建完成后，将已构建层的目标参数 networkSynced 置为 false（改由数据交换通道统一同步）
            var expressionBuilder = context.VRChatAvatarDescriptor().ExpressionParameters();
            var changed = false;
            foreach (var group in builtGroups)
            {
                foreach (var parameterName in group.ParameterNames)
                {
                    VRCExpressionParameters.Parameter found = null;
                    expressionBuilder.Find(parameterName, p => found = p);
                    if (found == null || !found.networkSynced) continue;
                    expressionBuilder.Remove(parameterName);
                    var p = found;
                    expressionBuilder.Add(parameterName, p.valueType, p.defaultValue, p.saved, false);
                    changed = true;
                }
            }
            if (changed) expressionBuilder.Build();
        }

        private static void BuildGroup(BuildContext context, VirtualAnimatorController fxController,
            ParameterGroup group, string syncSignalName, string dataExchangeName)
        {
            var parameterNames = group.ParameterNames;
            var parameterTypes = group.ParameterTypes;
            var dataExchangeType = group.ExchangeType;
            var syncSignalBitWidth = GetBitWidth(parameterNames.Count);

            // 注册同步信号位参数（Bool），位宽保证能表示全部索引
            var syncSignalBitNames = new string[syncSignalBitWidth];
            for (var i = 0; i < syncSignalBitWidth; i++)
            {
                syncSignalBitNames[i] = $"{syncSignalName}/bit{i}";
                fxController.AddParameterIfNot(new AnimatorControllerParameter
                {
                    name = syncSignalBitNames[i],
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = false,
                    defaultFloat = 0f,
                    defaultInt = 0
                });
            }

            // 注册同步信号位参数 + 数据交换参数到 ExpressionParameters
            var syncExpressionBuilder = context.VRChatAvatarDescriptor().ExpressionParameters();
            foreach (var syncSignalBitName in syncSignalBitNames)
            {
                syncExpressionBuilder.Add(syncSignalBitName, VRCExpressionParameters.ValueType.Bool, 0, false, true);
            }
            syncExpressionBuilder
                .Add(dataExchangeName, dataExchangeType, 0, false, true)
                .Build();

            // 注册数据交换参数到 FX 控制器（按层类型，而非硬编码 Float）
            fxController.AddParameterIfNot(new AnimatorControllerParameter
            {
                name = dataExchangeName,
                type = ToAnimatorType(dataExchangeType),
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

            // 在 FX 控制器中创建图层并构建 R/W 状态机
            var layer = fxController.AddLayer($"AutoParameterCompression/{StringHelper.GetRandomString()}");
            BuildSyncLayer(context, fxController, layer, parameterNames, syncSignalBitNames, dataExchangeName);
        }

        private static void BuildSyncLayer(BuildContext context, VirtualAnimatorController controller, VirtualLayer layer,
            IReadOnlyList<string> parameterNames, IReadOnlyList<string> syncSignalBitNames, string dataExchangeParameterName)
        {
            const int stateHeight = 60;
            var stateY = -(stateHeight * parameterNames.Count / 2);
            VirtualState defaultState = null;

            // 本地循环切换计数器，用于让写入状态 W 按索引循环切换
            var loopParameterName = $"AutoParameterCompression/loop/{StringHelper.GetRandomString()}";
            controller.AddParameterIfNot(loopParameterName, AnimatorControllerParameterType.Int);
            var clip = AnimationBuilder.Create()
                .FrameRate(100)
                .SetCurve($"CatToolsEmptyObject_{StringHelper.GetRandomString()}", typeof(GameObject), PropertyName.ObjIsActive, curveBuilder =>
                {
                    curveBuilder.AddKey(new Keyframe(0f, 0));
                    curveBuilder.AddKey(new Keyframe(SyncIntervalSeconds, 0));
                })
                .Build().ToVirtualMotion(context);
            var clipW = AnimationBuilder.Create()
                .FrameRate(100)
                .SetCurve($"CatToolsEmptyObject_{StringHelper.GetRandomString()}", typeof(GameObject), PropertyName.ObjIsActive, curveBuilder =>
                {
                    curveBuilder.AddKey(new Keyframe(0f, 0));
                    curveBuilder.AddKey(new Keyframe(0.01f, 0));
                })
                .Build().ToVirtualMotion(context);

            for (var i = 0; i < parameterNames.Count; i++)
            {
                var targetName = parameterNames[i];
                var index = i;
                var bits = index.SplitToBools(syncSignalBitNames.Count);

                // 读取状态 R_i：将 dataExchangeParameterName 的值 Copy 给目标参数
                // 过渡条件：IsLocal == false 且各同步信号位参数 == index 对应位
                var stateR = layer.AddState($"R_{i}", clipW, position: new Vector3(300, stateY + i * stateHeight));
                stateR.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
                {
                    driver.AddParameterDriverCopy(targetName, dataExchangeParameterName);
                });
                ConditionsBuilder.Create()
                    .If(VRCSdkAnimatorParameters.IsLocal.Name, false)
                    .Run(builder =>
                    {
                        for (var j = 0; j < syncSignalBitNames.Count; j++)
                        {
                            builder.If(syncSignalBitNames[j], bits[j]);
                        }
                    })
                    .Build()
                    .CreateAnyStateConditionsTransition(context, controller, layer, stateR, exitTime: 1f);

                // 写入状态 W_i：将目标参数的值 Copy 给 dataExchangeParameterName
                // 过渡条件：IsLocal == true 且 loopParameterName == index
                var stateW = layer.AddState($"W_{i}", clip, position: new Vector3(-300, stateY + i * stateHeight));
                stateW.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
                {
                    driver.AddParameterDriverCopy(dataExchangeParameterName, targetName);
                    // 直接设置同步信号位参数为当前索引对应的位值
                    for (var j = 0; j < syncSignalBitNames.Count; j++)
                    {
                        driver.AddParameterDriverSet(syncSignalBitNames[j], bits[j]);
                    }
                    // 推进本地循环计数器，用于循环切换到下一个写入状态
                    driver.AddParameterDriverSet(loopParameterName, (index + 1) % parameterNames.Count);
                });
                ConditionsBuilder.Create()
                    .If(VRCSdkAnimatorParameters.IsLocal.Name, true)
                    .Greater(loopParameterName, index - 0.1f)
                    .Less(loopParameterName, index + 0.1f)
                    .Build()
                    .CreateAnyStateConditionsTransition(context, controller, layer, stateW, exitTime: 1);

                if (i == 0) defaultState = stateW;
            }

            // 默认状态为 W_0
            layer.GetStateMachine().DefaultState = defaultState;
        }

        /// <summary>
        /// 计算同步信号位宽：2^bitWidth >= count，最小 1 位
        /// </summary>
        private static int GetBitWidth(int count)
        {
            var bitWidth = 1;
            while ((1 << bitWidth) < count) bitWidth++;
            return bitWidth;
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
