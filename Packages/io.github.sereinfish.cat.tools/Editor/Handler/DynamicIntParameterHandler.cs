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
using System.Linq;
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.Conditions;
using io.github.sereinfish.cat.tools.editor.Conditions.Build;
using io.github.sereinfish.cat.tools.editor.context;
using io.github.sereinfish.cat.tools.editor.context.Extensions;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    public class DynamicIntParameterHandler : ComponentHandler<DynamicIntParameter>
    {
        public override void Execute(ICatContext context, DynamicIntParameter entity)
        {
            var controller = context.GetAnimatorController(entity.layerType);

            foreach (var catDynamicInt in entity.parameters)
            {
                CreateDynamicInt(context, controller, catDynamicInt.name, null, catDynamicInt.width, catDynamicInt.save,
                    true, catDynamicInt.defaultValue, true, true, false);
            }
        }

        private static void CreateRWState(ICatContext context, ICatAnimatorController controller, ICatLayer layer,
            string name, int bitWidth, BitParameter[] bitParameters, bool read = true, bool write = true, bool isLocal = false)
        {
            var valueMax = 1 << bitWidth;
            const int stateHeight = 60;
            var stateY = -(stateHeight * valueMax / 2);
            for (var i = 0; i < valueMax; i++)
            {
                var currentValue = i;
                var bits = i.SplitToBools(bitWidth);
                if (read)
                {
                    var stateR = layer.AddState($"R_{i}", position: new Vector3(300, stateY + i * stateHeight));
                    if (i == 0) layer.StateMachine.DefaultState = stateR;
                    stateR.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
                    {
                        driver.AddParameterDriverSet(name, currentValue);
                        driver.AddParameterDriverSet($"IsInit/{name}", true);
                    });
                    var conditionRBuilder = ConditionsBuilder.Create()
                        .If(VRCSdkAnimatorParameters.IsLocal.Name, isLocal)
                        .Run(builder =>
                        {
                            for (var j = 0; j < bits.Length; j++)
                            {
                                builder.If(bitParameters[j].Name, bits[j]);
                            }
                        });
                    if (i > 0)
                    {
                        conditionRBuilder.Or()
                            .If($"IsInit/{name}", false)
                            .Run(builder =>
                            {
                                for (var j = 0; j < bits.Length; j++)
                                {
                                    builder.If(bitParameters[j].Name, bits[j]);
                                }
                            });
                    }
                    conditionRBuilder.Build().CreateAnyStateConditionsTransition(context, controller, layer, stateR);
                }

                if (write)
                {
                    var stateW = layer.AddState($"W_{i}", position: new Vector3(-300, stateY + i * stateHeight));
                    stateW.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
                    {
                        for (var j = 0; j < bits.Length; j++)
                        {
                            driver.AddParameterDriverSet(bitParameters[j].Name, bits[j]);
                        }
                    });
                    var conditionW = ConditionsBuilder.Create()
                        .If(VRCSdkAnimatorParameters.IsLocal.Name, true)
                        .If($"IsInit/{name}", true)
                        .Equal(name, currentValue)
                        .Build();
                    conditionW.CreateAnyStateConditionsTransition(context, controller, layer, stateW);
                }
            }
        }
        
        private static void CreateInitState(ICatContext context, ICatAnimatorController controller, ICatLayer layer,
            string name, BitParameter[] bitParameters)
        {
            var initState = layer.AddState("Init", position:new Vector3(0, 200));
            initState.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
            {
                driver.AddParameterDriverSet(name, 0f); // 初始值
                driver.AddParameterDriverSet($"IsInit/{name}", true);
            });

            var condition = ConditionsBuilder.Create()
                .If($"IsInit/{name}", false)
                .ForEach(bitParameters.Select(p => p.Name), CatAnimatorConditionRuntimeMode.If, false)
                .Build();
            
            condition.CreateAnyStateConditionsTransition(context, controller, layer, initState);
        }

        private static BitParameter[] GetBitParameter(string name, int bitWidth, int defaultValue, string[] bitNames = null)
        {
            var bits = defaultValue.SplitToBools(bitWidth);
            if (bitNames == null || bitNames.Length == 0)
            {
                bitNames = bits.Select((b, i) => $"CT_BIT/{name}/{i}").ToArray();
            }
            if (bits.Length != bitNames.Length)
            {
                throw new System.ArgumentException("Bit names length must match bit width");
            }
            return bits.Select((b, i) => new BitParameter { Name = bitNames[i], Value = b }).ToArray();
        }

        /// <summary>
        /// 初始化控制器参数
        /// </summary>
        private static void InitControllerParameter(ICatAnimatorController controller, string name, int defaultValue, BitParameter[] bitParameters)
        {
            // 注册 IsLocal
            controller.AddParameterIfNot(VRCSdkAnimatorParameters.IsLocal.Name, false);
            // 注册 IsInit
            controller.AddParameterIfNot($"IsInit/{name}", false);
            
            // 注册 Int
            controller.AddParameterIfNot(new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Int,
                defaultInt = defaultValue
            });
            // 注册 Bit
            foreach (var bitParameter in bitParameters)
            {
                controller.AddParameterIfNot(new AnimatorControllerParameter
                {
                    name = bitParameter.Name,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = bitParameter.Value
                });
            }
        }
        
        /// <summary>
        /// 注册参数
        /// </summary>
        private static void RegisterParameter(ICatContext context, string name, int defaultValue, bool save, bool networkSynced, BitParameter[] bitParameters)
        {
            
            context.GetAvatarDescriptor().ExpressionParameters()
                .Add(name, VRCExpressionParameters.ValueType.Int, defaultValue, false, false) // 注册 Int
                .Add($"IsInit/{name}", VRCExpressionParameters.ValueType.Bool, 0, false, false)
                .ForEach(bitParameters, (builder, bitParameter) =>
                {
                    // 注册 Bit
                    builder.Add(bitParameter.Name, VRCExpressionParameters.ValueType.Bool, bitParameter.Value ? 1f : 0f, save, networkSynced);
                }).Build();
        }
        
        private struct BitParameter
        {
            public string Name;
            public bool Value;
        }

        /// <summary>
        /// 创建动态 Int 参数
        /// </summary>
        public static KeyValuePair<string, List<string>> CreateDynamicInt(ICatContext context, ICatAnimatorController controller,
            string parameterName, string[] bitNames, int bitWidth, bool save, bool networkSynced, int defaultValue,
            bool read, bool write, bool isLocal)
        {
            var layer = ICatLayer.Create(context, $"DynamicInt/{parameterName}_{StringHelper.GetRandomString()}")
                .AddToController(controller);
            // 参数注册
            var bitParameters = GetBitParameter(parameterName, bitWidth, defaultValue, bitNames);
            InitControllerParameter(controller, parameterName, defaultValue, bitParameters);
            RegisterParameter(context, parameterName, defaultValue, save, networkSynced, bitParameters);
            // 创建状态机
            CreateInitState(context, controller, layer, parameterName, bitParameters);
            CreateRWState(context, controller, layer, parameterName, bitWidth, bitParameters, read, write, isLocal);
            
            return new KeyValuePair<string, List<string>>(parameterName, bitParameters.Select(p => p.Name).ToList());
        }
    }
}