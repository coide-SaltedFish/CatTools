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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.Conditions;
using io.github.sereinfish.cat.tools.editor.utils;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    public class AnimatorLayerLockHandler : ComponentHandler<AnimatorLayerLock>
    {
        public override BuildPhase Phase => BuildPhase.Optimizing;

        public override void Execute(BuildContext context, AnimatorLayerLock entity)
        {
            if (ExecuteCheck(entity).Not()) return;

            RegisterConditionsParameters(context, entity);
            
            foreach (var animatorLayerLockEntry in entity.layerLockEntries)
            {
                var controller = context.GetAnimatorController(animatorLayerLockEntry.animLayerType);
                if (controller == null)
                {
                    Debug.LogWarning($"AnimatorLayerLock {entity.name}: AnimatorController {animatorLayerLockEntry.animLayerType} 不存在，将不会执行任何操作");
                    continue;
                }

                var targetLayers = GetTargetLayers(controller, animatorLayerLockEntry);
                if (targetLayers.Count == 0)
                {
                    Debug.LogWarning($"AnimatorLayerLock {entity.name}: AnimatorController {animatorLayerLockEntry.animLayerType} 中没有找到目标层，将不会执行任何操作");
                    continue;
                }
                foreach (var layer in targetLayers)
                {
                    LockLayer(context, entity, animatorLayerLockEntry, controller, layer);
                }
            }
        }

        private void LockLayer(BuildContext context, AnimatorLayerLock entity, AnimatorLayerLockEntry entry, VirtualAnimatorController controller, VirtualLayer layer)
        {
            var lockTargetState = layer.GetStateMachine().DefaultState; // 锁定时默认跳转到的状态
            if (entry.lockOperation == AnimatorLayerLockOperation.CreateEmptyStateAndLock)
            {
                var emptyState = layer.AddState("Empty");
                lockTargetState = emptyState;
            }

            if (lockTargetState == null)
            {
                Debug.LogWarning($"AnimatorLayerLockHandler: {entry.animLayerType}->{layer.Name} 中没有找到目标状态，将不会执行任何操作");
                return;
            }
            // 如果是锁定到默认状态，为默认状态发出的过渡添加条件，不满足条件才进入下一步
            var newTransitions = new List<VirtualStateTransition>();
            foreach (var transition in lockTargetState.Transitions)
            {
                newTransitions.AddRange(transition.MergeParameterOrConditions(entity.conditions.Inverse()));
            }
            lockTargetState.Transitions = newTransitions.ToImmutableList();
            // 为所有的状态添加过渡，满足条件时过渡到锁定状态
            foreach (var state in EnumerateStates(layer.GetStateMachine()))
            {
                if (state == null) continue;
                
                if (state == lockTargetState) continue;
                entity.conditions.CreateConditionsTransitionTo(context, controller, state, lockTargetState);
            }
            // 当为锁定到空状态时，为锁定状态添加过渡，不满足条件时退出
            if (entry.lockOperation == AnimatorLayerLockOperation.CreateEmptyStateAndLock)
            {
                entity.conditions.Inverse().CreateConditionsTransitionToExit(context, controller, lockTargetState);
            }
            
            // 为 AnyStateTransitions 添加过渡
            SetAnyStateTransitions(context, controller, layer, layer.StateMachine, entity, lockTargetState);
        }
        
        private void SetAnyStateTransitions(BuildContext context, VirtualAnimatorController controller, VirtualLayer layer,
            VirtualStateMachine stateMachine, AnimatorLayerLock entity, VirtualState lockTargetState)
        {
            if (stateMachine == null) return;
            // 当前 StateMachine 直接包含的 AnyTransition
            if (stateMachine.AnyStateTransitions.IsEmpty.Not())
            {
                // 添加过渡当满足条件时进入锁定状态
                entity.conditions.CreateAnyStateConditionsTransition(context, controller, layer, lockTargetState);
                var anyStateTransitions = new List<VirtualStateTransition>();
                foreach (var transition in stateMachine.AnyStateTransitions)
                {
                    if (transition.DestinationState == lockTargetState)
                    {
                        anyStateTransitions.Add(transition);
                        continue;
                    }
                    // 为过渡添加条件
                    anyStateTransitions.AddRange(transition.MergeParameterOrConditions(entity.conditions.Inverse()));
                }   
                stateMachine.AnyStateTransitions = anyStateTransitions.ToImmutableList();
            }

            // 递归进入子 StateMachine
            foreach (var childMachine in stateMachine.StateMachines)
            {
                SetAnyStateTransitions(context, controller, layer, childMachine.StateMachine, entity, lockTargetState);
            }
        }
        private IEnumerable<VirtualState> EnumerateStates(VirtualStateMachine stateMachine)
        {
            // 当前 StateMachine 直接包含的 State
            foreach (var state in stateMachine.States)
            {
                yield return state.State;
            }

            // 递归进入子 StateMachine
            foreach (var childMachine in stateMachine.StateMachines)
            {
                foreach (var state in EnumerateStates(childMachine.StateMachine))
                {
                    yield return state;
                }
            }
        }
        
        /// <summary>
        /// 注册条件参数
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        private void RegisterConditionsParameters(BuildContext context, AnimatorLayerLock entity)
        {
            var controllerTypes = new HashSet<VRCAvatarDescriptor.AnimLayerType>();
            foreach (var animatorLayerLockEntry in entity.layerLockEntries)
            {
                controllerTypes.Add(animatorLayerLockEntry.animLayerType);
            }
            foreach (var animLayerType in controllerTypes)
            {
                var controller = context.GetAnimatorController(animLayerType);
                if (controller == null) continue;
                
                foreach (var parameterOrCondition in entity.conditions)
                {
                    foreach (var parameterCondition in parameterOrCondition)
                    {
                        controller.AddParameterIfNot(parameterCondition.name,
                            parameterCondition.mode is CatAnimatorConditionRuntimeMode.If or CatAnimatorConditionRuntimeMode.IfNot
                            ? AnimatorControllerParameterType.Bool
                            : AnimatorControllerParameterType.Float);
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取目标层
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private List<VirtualLayer> GetTargetLayers(VirtualAnimatorController controller, AnimatorLayerLockEntry entity)
        {
            var targetLayers = new List<VirtualLayer>();
            switch (entity.lockScope)
            {
                case AnimatorLayerLockScope.EntireController: // 整个控制器
                    targetLayers.AddRange(controller.Layers);
                    break;
                case AnimatorLayerLockScope.SpecificLayer:
                    if (string.IsNullOrEmpty(entity.layerName))
                    {
                        Debug.LogWarning("SpecificLayer 模式下 layerName 不能为空");
                        return targetLayers;
                    }
                    try
                    {
                        var regex = new Regex(entity.layerName);
                        targetLayers.AddRange(controller.Layers.Where(layer => regex.IsMatch(layer.Name)));
                    }
                    catch (ArgumentException)
                    {
                        // 正则无效，回退到精确匹配
                        targetLayers.AddRange(controller.Layers.Where(layer => layer.Name == entity.layerName));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return targetLayers;
        }

        /// <summary>
        /// 执行检查
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool ExecuteCheck(AnimatorLayerLock entity)
        {
            if (entity.layerLockEntries == null || entity.layerLockEntries.Length == 0)
            {
                Debug.LogWarning($"AnimatorLayerLock {entity.name}: layerLockEntries 为空，将不会执行任何操作");
                return false;
            }

            if (entity.conditions == null || !entity.conditions.Any())
            {
                Debug.LogWarning($"AnimatorLayerLock {entity.name}: conditions 为空，将不会执行任何操作");
                return false;
            }

            return true;
        }
    }
}