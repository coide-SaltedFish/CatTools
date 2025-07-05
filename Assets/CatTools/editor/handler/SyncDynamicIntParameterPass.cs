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
using CatTools.editor.utils;
using CatTools.Runtime;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

namespace CatTools.editor.handler
{
    public class SyncDynamicIntParameterPass : Pass<SyncDynamicIntParameterPass>
    {
        private AnimatorServicesContext _asc;
        private CloneContext _cc;
        
        protected override void Execute(BuildContext context)
        {
            // 遍历 Object
            var targets = context.AvatarRootTransform.GetComponentsInChildren<SyncDynamicIntParameter>(true);
            _asc = context.Extension<AnimatorServicesContext>();

            var services = context.Extension<AnimatorServicesContext>();
            _cc = services.ControllerContext.CloneContext;

            foreach (var target in targets) ProcessComponent(context, target);
        }
        
        /// <summary>
        /// 对单个组件进行处理
        /// </summary>
        /// <param name="context"></param>
        /// <param name="target"></param>
        private void ProcessComponent(BuildContext context, SyncDynamicIntParameter target)
        {
            var csBitNames = new List<string>();
            var csBitCount = GetParameterCsCount(target);
            
            // 没有参数则跳过
            if (target.parameters.Count == 0) return;
            // 创建片选层
            if (target.parameters.Count > 1)
            {
                for (var i = 0; i < csBitCount; i++)
                {
                    csBitNames.Add($"{target.transform.name}_CS_Bit_{i}_{CryptoRandomString.GetRandomString()}");
                }
                
                CreateCsLayer(context, csBitNames, csBitCount, target);
            }
            // 创建 I/O 层
            CreateIOLayer(context, csBitNames, csBitCount, target);
        }

        private void CreateIOLayer(BuildContext context, List<string> csBitNames, int csBitCount, SyncDynamicIntParameter target)
        {
            // 获取动画控制器
            var controller = _asc.ControllerContext.Controllers[target.layerType];
            // 参数读写层
            var layerName = AnimationUtils.GetLayerName($"SyncDynamicIntParameter_IO_{target.transform.name}");
            var layer = VirtualLayer.Create(_cc, layerName);
            controller.AddLayer(LayerPriority.Default, layer);
            // 写入参数 bits 到动画控制器及菜单参数
            var pBitNames = new List<string>();
            for (var i = 0; i < 1 << target.bitWidth; i++)
            { 
                var bitName = $"CT_{target.transform.name}_BIT_{i}_{CryptoRandomString.GetRandomString()}";
                
                controller.AddParameterIfNot(new AnimatorControllerParameter
                {
                    name = bitName,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = false
                });
                context.AvatarDescriptor.AddParameterOrElse(new VRCExpressionParameters.Parameter
                {
                    name = bitName,
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    defaultValue = 0,
                    saved = false,
                    networkSynced = true
                });
                
                pBitNames.Add(bitName);
            }
            
            // 写入参数到动画控制器
            controller.AddParameterIfNot(new AnimatorControllerParameter
            {
                name = "IsLocal",
                type = AnimatorControllerParameterType.Bool,
                defaultInt = 0
            });
            
            foreach (var pName in target.parameters)
            {
                controller.AddParameterIfNot(new AnimatorControllerParameter
                {
                    name = pName,
                    type = AnimatorControllerParameterType.Int,
                    defaultInt = 0
                });
            }
            
            // 添加状态
            var sm = layer.StateMachine!;
            const int startY = 0;
            const int stateHeight = 60;
            var stateIndex = 0;
            
            sm.AnyStatePosition = Vector3.zero;
            
            // any state 
            // 遍历片选条件
            // 每个片选条件对应一个 Int 参数的读与写，在本地为写，非本地为读，根据位宽生成读写状态
            // 参数数量为 1 时，不需要片选
            
            // 创建状态，遍历参数依次生成，过渡条件为 非本地 片选（参数数量大于 1 ），参数 Bit 值
            for (var pCsIndex = 0; pCsIndex < target.parameters.Count; pCsIndex++)
            {
                var pName = target.parameters[pCsIndex];
                // 循环参数值
                for (var j = 0; j < 1 << target.bitWidth; j++)
                {
                    // 读状态
                    var state = sm.AddState($"{pName}_{j}_R", position:new Vector3(300, startY + stateIndex * stateHeight));
                    
                    // 设置状态动作，满足条件时设置参数值
                    var parameterDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                    parameterDriver.isLocalPlayer = false;
                    parameterDriver.isEnabled = true;
                    
                    parameterDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        name = pName,
                        value = j,
                        type = VRC_AvatarParameterDriver.ChangeType.Set
                    });
                    
                    state.Behaviours = state.Behaviours.Add(parameterDriver);
                    
                    // 添加条件过度
                    var transition = VirtualStateTransition.Create();
                    transition.SetDestination(state);
                    transition.Duration = 0f;
                    transition.ExitTime = null;
                    
                    // 添加条件，非本地，参数值
                    transition.Conditions = transition.Conditions.Add(new AnimatorCondition
                    {
                        parameter = "IsLocal",
                        mode = AnimatorConditionMode.IfNot
                    });
                    var bits = j.SplitToBools(target.bitWidth);
                    for (var i = 0; i < bits.Length; i++)
                    {
                        transition.Conditions = transition.Conditions.Add(new AnimatorCondition
                        {
                            parameter = pBitNames[i],
                            mode = bits[i] ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot
                        });
                    }
                    
                    // 如果参数数量大于 1 ，加入片选条件
                    if (target.parameters.Count > 1)
                    {
                        var csBits = pCsIndex.SplitToBools(csBitCount);
                        for (var i = 0; i < csBits.Length; i++)
                        {
                            transition.Conditions = transition.Conditions.Add(new AnimatorCondition
                            {
                                parameter = csBitNames[i],
                                mode = csBits[i] ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot
                            });
                        }
                    }
                    
                    sm.AnyStateTransitions = sm.AnyStateTransitions.Add(transition);
                    
                    // 写状态
                    state = sm.AddState($"{pName}_{j}_W", position:new Vector3(- 300, startY + stateIndex * stateHeight));
                    
                    // 写动作，在触发时写入对应的参数 bits
                    parameterDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                    parameterDriver.isLocalPlayer = false;
                    parameterDriver.isEnabled = true;
                    
                    bits = j.SplitToBools(target.bitWidth);
                    for (var i = 0; i < bits.Length; i++)
                    {
                        parameterDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                        {
                            name = pBitNames[i],
                            value = bits[i] ? 1f : 0f,
                            type = VRC_AvatarParameterDriver.ChangeType.Set
                        });
                    }
                    state.Behaviours = state.Behaviours.Add(parameterDriver);
                    
                    // 添加条件过度
                    transition = VirtualStateTransition.Create();
                    transition.SetDestination(state);
                    transition.Duration = 0f;
                    transition.ExitTime = null;
                    
                    // 本地、int 为指定值 时
                    transition.Conditions = transition.Conditions.Add(new AnimatorCondition
                    {
                        parameter = "IsLocal",
                        mode = AnimatorConditionMode.If
                    });
                    
                    transition.Conditions = transition.Conditions.Add(new AnimatorCondition
                    {
                        parameter = pName,
                        mode = AnimatorConditionMode.Equals,
                        threshold = j
                    });
                    sm.AnyStateTransitions = sm.AnyStateTransitions.Add(transition);
                    // 如果参数数量大于 1 ，加入片选条件
                    if (target.parameters.Count > 1)
                    {
                        var csBits = pCsIndex.SplitToBools(csBitCount);
                        for (var i = 0; i < csBits.Length; i++)
                        {
                            transition.Conditions = transition.Conditions.Add(new AnimatorCondition
                            {
                                parameter = csBitNames[i],
                                mode = csBits[i] ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot
                            });
                        }
                    }
                    
                    stateIndex++;
                }
            }
        }
        
        private void CreateCsLayer(BuildContext context, List<string> csBitNames, int csBitCount, SyncDynamicIntParameter target)
        {
            // 获取动画控制器
            var controller = _asc.ControllerContext.Controllers[target.layerType];
            
            // 创建片选层
            var csLayerName = AnimationUtils.GetLayerName($"SyncDynamicIntParameter_CS_{target.gameObject.name}");
            var csLayer = VirtualLayer.Create(_cc, csLayerName);
            controller.AddLayer(LayerPriority.Default, csLayer);
            
            // 创建状态
            var csStateMachine = csLayer.StateMachine!;
            csStateMachine.EntryPosition = new Vector3(0, -300);
            csStateMachine.AnyStatePosition = new Vector3(0, 0);
            
            // 初始化Bits列表和状态列表
            
            var csStates = new List<VirtualState>();
            const int csStateStartX = 300;
            const int csStateHeight = 120;
            var csStateStartY =  - (csBitCount * csStateHeight / 2);

            
            
            for (var i = 0; i < target.parameters.Count; i++)
            {
                var state = csStateMachine.AddState($"CS_Bit_{i}", position: new Vector3(csStateStartX, csStateStartY +
                    (i + 1) * csStateHeight));
                
                // 设置行为
                var parameterDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                parameterDriver.isLocalPlayer = false;
                parameterDriver.isEnabled = true;
                // 读取参数设置值，设置 bits 参数为 i
                var bits = i.SplitToBools(csBitCount);
                for (var j = 0; j < bits.Length; j++)
                {
                    var splitToBool = bits[j];
                    var csBitName = csBitNames[j];
                    parameterDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        name = csBitName,
                        value = splitToBool ? 1 : 0,
                        type = VRC_AvatarParameterDriver.ChangeType.Set
                    });
                }

                state.Behaviours = state.Behaviours.Add(parameterDriver);
                csStates.Add(state);
            }
            
            // 向动画控制器添加参数以及向菜单参数注册
            foreach (var csBitName in csBitNames)
            {
                controller.AddParameterIfNot(new AnimatorControllerParameter
                {
                    name = csBitName,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = false
                });
                context.AvatarDescriptor.AddParameterOrElse(new VRCExpressionParameters.Parameter
                {
                    name = csBitName,
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    defaultValue = 0,
                    saved = false,
                    networkSynced = true
                });
            }
            
            // 片选状态连线
            var csExitTime = (float)target.cycleTime / csStates.Count / 1000; // 退出时间为 周期 / 状态数量，单位为秒
            for (var i = 0; i < csStates.Count; i++)
            {
                // 第 i 个参数的过渡条件就是当 bits 为 i - 1 时，第 0 为 最大
                var conditionValue = i - 1;
                if (i == 0) conditionValue = csStates.Count - 1;

                var transition = VirtualStateTransition.Create();
                transition.SetDestination(csStates[i]);
                transition.Duration = 0f;
                transition.ExitTime = csExitTime;
                
                // 设置过渡条件
                var bits = conditionValue.SplitToBools(csBitCount);
                for (var j = 0; j < bits.Length; j++)
                {
                    var splitToBool = bits[j];
                    var csBitName = csBitNames[j];
                    transition.Conditions = transition.Conditions.Add(new AnimatorCondition
                    {
                        mode = splitToBool ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                        threshold = 0,
                        parameter = csBitName
                    });
                }
                
                csStateMachine.AnyStateTransitions = csStateMachine.AnyStateTransitions.Add(transition);
            }
        }
        
        /// <summary>
        /// 获取片选参数数量
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private int GetParameterCsCount(SyncDynamicIntParameter target)
        {
            var n = target.parameters.Count;
            return n > 1 ? Mathf.CeilToInt(Mathf.Log(n, 2f)) : 0;
        }
    }
}