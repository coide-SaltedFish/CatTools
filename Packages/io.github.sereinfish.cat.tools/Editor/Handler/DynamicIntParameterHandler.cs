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
using Editor.Conditions.Build;
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.Conditions;
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
                var layer = ICatLayer.Create(context, $"DynamicInt/{catDynamicInt.name}")
                    .AddToController(controller);
                var bitParameters = GetBitParameter(catDynamicInt);
                
                InitControllerParameter(controller, catDynamicInt, bitParameters);
                RegisterParameter(context, catDynamicInt, bitParameters);
                
                // 创建状态机
                CreateInitState(context, controller, layer, catDynamicInt, bitParameters);
                CreateRWState(context, controller, layer, catDynamicInt, bitParameters);
            }
        }

        private void CreateRWState(ICatContext context, ICatAnimatorController controller, ICatLayer layer,
            DynamicIntParameter.CatDynamicInt catDynamicInt, BitParameter[] bitParameters)
        {
            var valueMax = 1 << catDynamicInt.width;
            const int stateHeight = 60;
            var stateY = -(stateHeight * valueMax / 2);
            for (var i = 0; i < valueMax; i++)
            {
                var currentValue = i;
                var bits = i.SplitToBools(catDynamicInt.width);
                var stateR = layer.AddState($"R_{i}", position: new Vector3(300, stateY + i * stateHeight));
                if (i == 0) layer.StateMachine.DefaultState = stateR;
                stateR.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
                {
                    driver.AddParameterDriverSet(catDynamicInt.name, currentValue);
                    driver.AddParameterDriverSet($"IsInit/{catDynamicInt.name}", true);
                });
                var conditionRBuilder = ConditionsBuilder.Create()
                    .If(VRCSdkAnimatorParameters.IsLocal.Name, false)
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
                        .If($"IsInit/{catDynamicInt.name}", false)
                        .Run(builder =>
                        {
                            for (var j = 0; j < bits.Length; j++)
                            {
                                builder.If(bitParameters[j].Name, bits[j]);
                            }
                        });
                }
                conditionRBuilder.Build().CreateAnyStateConditionsTransition(context, controller, layer, stateR);
                
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
                    .If($"IsInit/{catDynamicInt.name}", true)
                    .Equal(catDynamicInt.name, currentValue)
                    .Build();
                conditionW.CreateAnyStateConditionsTransition(context, controller, layer, stateW);
            }
        }
        
        private void CreateInitState(ICatContext context, ICatAnimatorController controller, ICatLayer layer, DynamicIntParameter.CatDynamicInt catDynamicInt, BitParameter[] bitParameters)
        {
            var initState = layer.AddState("Init", position:new Vector3(0, 200));
            initState.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
            {
                driver.AddParameterDriverSet(catDynamicInt.name, catDynamicInt.defaultValue);
                driver.AddParameterDriverSet($"IsInit/{catDynamicInt.name}", true);
            });

            var condition = ConditionsBuilder.Create()
                .If($"IsInit/{catDynamicInt.name}", false)
                .ForEach(bitParameters.Select(p => p.Name), CatAnimatorConditionRuntimeMode.If, false)
                .Build();
            
            condition.CreateAnyStateConditionsTransition(context, controller, layer, initState);
        }

        private BitParameter[] GetBitParameter(DynamicIntParameter.CatDynamicInt catDynamicInt)
        {
            var bits = catDynamicInt.defaultValue.SplitToBools(catDynamicInt.width);
            return bits.Select((b, i) => new BitParameter { Name = $"CT_BIT/{catDynamicInt.name}/{i}", Value = b }).ToArray();
        }

        /// <summary>
        /// 初始化控制器参数
        /// </summary>
        private void InitControllerParameter(ICatAnimatorController controller, DynamicIntParameter.CatDynamicInt catDynamicInt, BitParameter[] bitParameters)
        {
            // 注册 IsLocal
            controller.AddParameterIfNot(VRCSdkAnimatorParameters.IsLocal.Name, AnimatorControllerParameterType.Bool, false);
            // 注册 IsInit
            controller.AddParameterIfNot($"IsInit/{catDynamicInt.name}", AnimatorControllerParameterType.Bool, false);
            
            // 注册 Int
            controller.AddParameterIfNot(new AnimatorControllerParameter
            {
                name = catDynamicInt.name,
                type = AnimatorControllerParameterType.Int,
                defaultInt = catDynamicInt.defaultValue
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
        private void RegisterParameter(ICatContext context, DynamicIntParameter.CatDynamicInt catDynamicInt, BitParameter[] bitParameters)
        {
            
            context.GetAvatarDescriptor().ExpressionParameters()
                .Add(catDynamicInt.name, VRCExpressionParameters.ValueType.Int, catDynamicInt.defaultValue, false) // 注册 Int
                .ForEach(bitParameters, (builder, bitParameter) =>
                {
                    // 注册 Bit
                    builder.Add(bitParameter.Name, VRCExpressionParameters.ValueType.Bool, bitParameter.Value ? 1f : 0f, catDynamicInt.save);
                }).Build();
        }
        
        private struct BitParameter
        {
            public string Name;
            public bool Value;
        }
    }
}