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
using io.github.sereinfish.cat.tools.editor.context;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    public class AnimatorLayerLockHandler : ComponentHandler<AnimatorLayerLock>
    {
        public override void Execute(ICatContext context, AnimatorLayerLock entity)
        {
            if (entity.layerLockEntries == null || entity.layerLockEntries.Length == 0)
            {
                Debug.LogWarning($"AnimatorLayerLock {entity.name}: layerLockEntries 为空，将不会执行任何操作");
                return;
            }

            if (entity.conditions == null || !entity.conditions.Any())
            {
                Debug.LogWarning($"AnimatorLayerLock {entity.name}: conditions 为空，将不会执行任何操作");
                return;
            }

            foreach (var entry in entity.layerLockEntries)
            {
                var controller = context.GetAnimatorController(entry.animLayerType);
                if (controller == null)
                {
                    Debug.LogWarning($"AnimatorLayerLock {entity.name}: 无法获取 AnimLayerType.{entry.animLayerType} 对应的控制器");
                    continue;
                }

                var targetLayers = GetTargetLayers(controller, entry);
                if (targetLayers.Count == 0)
                {
                    Debug.LogWarning($"AnimatorLayerLock {entity.name}: 未找到匹配的图层 (scope={entry.lockScope}, layerName={entry.layerName})");
                    continue;
                }

                foreach (var layer in targetLayers)
                {
                    RegisterConditionsParameters(controller, entity.conditions);

                    switch (entry.lockOperation)
                    {
                        case AnimatorLayerLockOperation.LockToCurrentDefault:
                            ApplyLockToCurrentDefault(context, controller, layer, entity.conditions);
                            break;
                        case AnimatorLayerLockOperation.CreateEmptyStateAndLock:
                            ApplyCreateEmptyStateAndLock(context, controller, layer, entity.conditions);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 根据 lockScope 获取目标图层列表
        /// </summary>
        private static List<ICatLayer> GetTargetLayers(ICatAnimatorController controller, AnimatorLayerLockEntry entry)
        {
            var result = new List<ICatLayer>();

            if (entry.lockScope == AnimatorLayerLockScope.EntireController)
            {
                result.AddRange(controller.Layers);
            }
            else if (entry.lockScope == AnimatorLayerLockScope.SpecificLayer)
            {
                if (string.IsNullOrEmpty(entry.layerName))
                {
                    Debug.LogWarning("SpecificLayer 模式下 layerName 不能为空");
                    return result;
                }

                try
                {
                    var regex = new Regex(entry.layerName);
                    foreach (var layer in controller.Layers)
                    {
                        if (regex.IsMatch(layer.Name))
                            result.Add(layer);
                    }
                }
                catch (ArgumentException)
                {
                    // 正则无效，回退到精确匹配
                    foreach (var layer in controller.Layers)
                    {
                        if (layer.Name == entry.layerName)
                            result.Add(layer);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 注册条件参数到控制器
        /// </summary>
        private static void RegisterConditionsParameters(ICatAnimatorController controller, ParameterOrConditions conditions)
        {
            foreach (var conditionsEntry in conditions)
            {
                foreach (var condition in conditionsEntry.conditions)
                {
                    var mode = condition.mode.ToEdit();
                    controller.AddParameterIfNot(new AnimatorControllerParameter
                    {
                        name = condition.name,
                        type = mode is AnimatorConditionMode.If or AnimatorConditionMode.IfNot
                            ? AnimatorControllerParameterType.Bool
                            : AnimatorControllerParameterType.Float,
                        defaultFloat = 0f,
                        defaultBool = false,
                        defaultInt = 0
                    });
                }
            }
        }

        /// <summary>
        /// 锁定到当前默认状态
        /// </summary>
        private static void ApplyLockToCurrentDefault(ICatContext context, ICatAnimatorController controller,
            ICatLayer layer, ParameterOrConditions conditions)
        {
            var defaultState = layer.StateMachine.DefaultState;

            if (defaultState == null)
            {
                Debug.LogWarning($"AnimatorLayerLock: 图层 '{layer.Name}' 没有默认状态，无法执行锁定");
                return;
            }

            // 判断是否存在目标为默认状态的 AnyState 过渡
            var hasAnyStateToDefault = AnyStateTransitionsTargetsState(layer, defaultState);

            if (hasAnyStateToDefault)
            {
                ApplyAnyStateLock(context, controller, layer, defaultState, conditions);
            }
            else
            {
                ApplyEntryLock(context, controller, layer, defaultState, conditions);
            }
        }

        /// <summary>
        /// 创建空状态并锁定到空状态
        /// </summary>
        private static void ApplyCreateEmptyStateAndLock(ICatContext context, ICatAnimatorController controller,
            ICatLayer layer, ParameterOrConditions conditions)
        {
            var emptyState = layer.AddState($"CatLockEmpty_{StringHelper.GetRandomString()}", null);
            layer.StateMachine.DefaultState = emptyState;

            // 新创建的状态不会有 AnyState 过渡指向它，直接走 Entry 路径
            ApplyEntryLock(context, controller, layer, emptyState, conditions);
        }

        /// <summary>
        /// AnyState 路径锁定：修改所有 AnyState 过渡条件
        /// </summary>
        private static void ApplyAnyStateLock(ICatContext context, ICatAnimatorController controller,
            ICatLayer layer, ICatState lockTarget, ParameterOrConditions conditions)
        {
            var existingTransitions = layer.StateMachine.AnyStateTransitions;
            if (!existingTransitions.Any()) return;

            // 先确保参数已注册
            RegisterConditionsParameters(controller, conditions);

            var newTransitions = ImmutableList<ICatStateTransition>.Empty;

            foreach (var existingTransition in existingTransitions)
            {
                if (TransitionTargetsState(existingTransition, lockTarget))
                {
                    // 目标为锁定状态的过渡：追加 conditions（AND）
                    var expanded = ExpandConditionsAnd(existingTransition, conditions, context, lockTarget);
                    newTransitions = newTransitions.AddRange(expanded);
                }
                else
                {
                    // 目标为其他状态的过渡：追加 conditions 的非（AND NOT conditions）
                    var expanded = ExpandConditionsAndNot(existingTransition, conditions, context, lockTarget);
                    newTransitions = newTransitions.AddRange(expanded);
                }
            }

            layer.StateMachine.AnyStateTransitions = newTransitions;
        }

        /// <summary>
        /// Entry 路径锁定：修改普通过渡条件
        /// </summary>
        private static void ApplyEntryLock(ICatContext context, ICatAnimatorController controller,
            ICatLayer layer, ICatState lockTarget, ParameterOrConditions conditions)
        {
            // 先确保参数已注册
            RegisterConditionsParameters(controller, conditions);

            var nonDefaultStates = layer.StateMachine.States
                .Where(s => !IsSameState(s, lockTarget))
                .ToList();

            foreach (var state in nonDefaultStates)
            {
                var stateTransitions = state.Transitions;
                if (!stateTransitions.Any()) continue;

                var newTransitions = ImmutableList<ICatStateTransition>.Empty;

                foreach (var existingTransition in stateTransitions)
                {
                    // 跳过已经是到锁定目标的过渡（不修改）
                    if (TransitionTargetsState(existingTransition, lockTarget))
                    {
                        newTransitions = newTransitions.Add(existingTransition);
                        continue;
                    }

                    if (IsExitTransition(existingTransition))
                    {
                        // Exit 过渡：保留退出目标，只追加 NOT conditions
                        var expanded = ExpandConditionsAndNotToExit(existingTransition, conditions, context);
                        newTransitions = newTransitions.AddRange(expanded);
                    }
                    else
                    {
                        // 普通过渡到其他状态：追加 NOT conditions
                        var expanded = ExpandConditionsAndNot(existingTransition, conditions, context, lockTarget);
                        newTransitions = newTransitions.AddRange(expanded);
                    }
                }

                // 新增: 从此状态到锁定目标的过渡，条件为 conditions
                foreach (var orGroup in conditions)
                {
                    var toLockTransition = ICatStateTransition.Create(context);
                    toLockTransition.SetDestination(lockTarget);
                    toLockTransition.ExitTime = null;
                    toLockTransition.Duration = 0;

                    var existingConds = ImmutableList<AnimatorCondition>.Empty;
                    foreach (var condition in orGroup.conditions)
                    {
                        existingConds = existingConds.Add(new AnimatorCondition
                        {
                            parameter = condition.name,
                            mode = condition.GetMode(),
                            threshold = Convert.ToSingle(condition.value)
                        });
                    }
                    toLockTransition.Conditions = existingConds;

                    newTransitions = newTransitions.Add(toLockTransition);
                }

                state.Transitions = newTransitions;
            }
        }

        /// <summary>
        /// 扩展过渡条件：在原条件基础上追加 conditions（AND 关系）
        /// 对于 conditions 中每个 OR 组，生成一个新的过渡
        /// </summary>
        private static ImmutableList<ICatStateTransition> ExpandConditionsAnd(
            ICatStateTransition existingTransition, ParameterOrConditions conditions,
            ICatContext context, ICatState destination)
        {
            var result = ImmutableList<ICatStateTransition>.Empty;

            foreach (var orGroup in conditions)
            {
                var newTransition = ICatStateTransition.Create(context);
                newTransition.SetDestination(destination);
                newTransition.ExitTime = existingTransition.ExitTime;
                newTransition.Duration = existingTransition.Duration;

                var combined = existingTransition.Conditions;
                foreach (var condition in orGroup.conditions)
                {
                    combined = combined.Add(new AnimatorCondition
                    {
                        parameter = condition.name,
                        mode = condition.GetMode(),
                        threshold = Convert.ToSingle(condition.value)
                    });
                }
                newTransition.Conditions = combined;

                result = result.Add(newTransition);
            }

            return result;
        }

        /// <summary>
        /// 扩展过渡条件：在原条件基础上追加 NOT conditions（AND NOT 关系）
        /// 生成笛卡尔积：每个条件组中选一个条件的逆，跨组组合
        /// </summary>
        private static ImmutableList<ICatStateTransition> ExpandConditionsAndNot(
            ICatStateTransition existingTransition, ParameterOrConditions conditions,
            ICatContext context, ICatState destination)
        {
            var result = ImmutableList<ICatStateTransition>.Empty;

            // 生成笛卡尔积：从每个 OR 组中选一个条件
            var inverseGroups = new List<List<ParameterCondition>>();
            foreach (var orGroup in conditions)
            {
                inverseGroups.Add(orGroup.conditions.ToList());
            }

            // 计算所有组合
            var total = inverseGroups.Aggregate(1, (current, group) => current * group.Count);
            var condsArray = inverseGroups.Select(g => g.ToArray()).ToArray();

            for (var i = 0; i < total; i++)
            {
                var index = i;

                var newTransition = ICatStateTransition.Create(context);
                newTransition.SetDestination(destination);
                newTransition.ExitTime = existingTransition.ExitTime;
                newTransition.Duration = existingTransition.Duration;

                var combined = existingTransition.Conditions;
                foreach (var group in condsArray)
                {
                    var choiceIndex = index % group.Length;
                    index /= group.Length;

                    var condition = group[choiceIndex];
                    combined = combined.Add(new AnimatorCondition
                    {
                        parameter = condition.name,
                        mode = condition.GetMode().Inverse(),
                        threshold = Convert.ToSingle(condition.value)
                    });
                }
                newTransition.Conditions = combined;

                result = result.Add(newTransition);
            }

            return result;
        }

        /// <summary>
        /// 检查 AnyState 过渡中是否有目标为指定状态的
        /// </summary>
        private static bool AnyStateTransitionsTargetsState(ICatLayer layer, ICatState targetState)
        {
            foreach (var t in layer.StateMachine.AnyStateTransitions)
            {
                if (TransitionTargetsState(t, targetState))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 判断过渡的目标是否是指定状态（同时支持 Bake 和 Build 模式）
        /// </summary>
        private static bool TransitionTargetsState(ICatStateTransition transition, ICatState state)
        {
            // Bake 模式
            var bakeTransition = transition.GetTransition<AnimatorStateTransition>();
            var bakeState = state.GetState<AnimatorState>();
            if (bakeTransition != null && bakeState != null)
                return bakeTransition.destinationState == bakeState;

            // Build 模式
            var buildTransition = transition.GetTransition<nadena.dev.ndmf.animator.VirtualStateTransition>();
            var buildState = state.GetState<nadena.dev.ndmf.animator.VirtualState>();
            if (buildTransition != null && buildState != null)
                return buildTransition.DestinationState == buildState;

            return false;
        }

        /// <summary>
        /// 判断过渡是否指向退出（Exit）
        /// </summary>
        private static bool IsExitTransition(ICatStateTransition transition)
        {
            // Bake 模式
            var bakeTransition = transition.GetTransition<AnimatorStateTransition>();
            if (bakeTransition != null)
                return bakeTransition.isExit;

            // Build 模式
            var buildTransition = transition.GetTransition<nadena.dev.ndmf.animator.VirtualStateTransition>();
            if (buildTransition != null)
                return buildTransition.IsExit;

            return false;
        }

        /// <summary>
        /// 扩展过渡条件（保留 Exit 目标）：在原条件基础上追加 NOT conditions（AND NOT 关系）
        /// </summary>
        private static ImmutableList<ICatStateTransition> ExpandConditionsAndNotToExit(
            ICatStateTransition existingTransition, ParameterOrConditions conditions,
            ICatContext context)
        {
            var result = ImmutableList<ICatStateTransition>.Empty;

            // 生成笛卡尔积：从每个 OR 组中选一个条件
            var inverseGroups = new List<List<ParameterCondition>>();
            foreach (var orGroup in conditions)
            {
                inverseGroups.Add(orGroup.conditions.ToList());
            }

            // 计算所有组合
            var total = inverseGroups.Aggregate(1, (current, group) => current * group.Count);
            var condsArray = inverseGroups.Select(g => g.ToArray()).ToArray();

            for (var i = 0; i < total; i++)
            {
                var index = i;

                var newTransition = ICatStateTransition.Create(context);
                newTransition.SetExitDestination();
                newTransition.ExitTime = existingTransition.ExitTime;
                newTransition.Duration = existingTransition.Duration;

                var combined = existingTransition.Conditions;
                foreach (var group in condsArray)
                {
                    var choiceIndex = index % group.Length;
                    index /= group.Length;

                    var condition = group[choiceIndex];
                    combined = combined.Add(new AnimatorCondition
                    {
                        parameter = condition.name,
                        mode = condition.GetMode().Inverse(),
                        threshold = Convert.ToSingle(condition.value)
                    });
                }
                newTransition.Conditions = combined;

                result = result.Add(newTransition);
            }

            return result;
        }

        /// <summary>
        /// 判断两个 ICatState 是否引用同一个底层状态
        /// </summary>
        private static bool IsSameState(ICatState a, ICatState b)
        {
            // Bake 模式
            var bakeA = a.GetState<AnimatorState>();
            var bakeB = b.GetState<AnimatorState>();
            if (bakeA != null && bakeB != null)
                return bakeA == bakeB;

            // Build 模式
            var buildA = a.GetState<nadena.dev.ndmf.animator.VirtualState>();
            var buildB = b.GetState<nadena.dev.ndmf.animator.VirtualState>();
            if (buildA != null && buildB != null)
                return buildA == buildB;

            return false;
        }
    }
}
