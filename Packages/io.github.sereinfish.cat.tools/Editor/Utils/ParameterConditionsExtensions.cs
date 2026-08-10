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
using System.Linq;
using io.github.sereinfish.cat.tools.Conditions;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class ParameterConditionsExtensions
    {
        public static void CreateConditionsTransition(this ParameterOrConditions conditions, BuildContext context,
            VirtualAnimatorController controller, VirtualState onState, VirtualState offState, float? exitTime = null, float duration = 0f)
        {
            // 检查参数
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
            
            // 生成 off -> on 过渡
            // 遍历或条件中的与条件生成过渡
            foreach (var entry in conditions)
            {
                var tOnState = context.CreateVirtualStateTransition();
                tOnState.SetDestination(onState);
                tOnState.ExitTime = exitTime;
                tOnState.Duration = duration;
                
                foreach (var condition in entry.conditions)
                {
                    tOnState.Conditions = tOnState.Conditions.Add(new AnimatorCondition
                    {
                        parameter = condition.name,
                        mode = condition.GetMode(),
                        threshold = Convert.ToSingle(condition.value)
                    });
                }
                offState.Transitions = offState.Transitions.Add(tOnState);
            }
            
            // 创建 on -> off 过渡
            var conditionsOff = new ParameterOrConditions();
            
            var total = conditions.Aggregate(1, (current, entry) => current * entry.conditions.Count);
            for (var i = 0; i < total; i++)
            {
                var index = i;
                var combination = new ParameterConditions();

                foreach (var entry in conditions)
                {
                    var choiceIndex = index % entry.conditions.Count;
                    index /= entry.conditions.Count;
                    combination.Add(entry.conditions[choiceIndex]);
                }

                conditionsOff.Add(combination);
            }
            
            foreach (var entries in conditionsOff)
            {
                var tOffState = context.CreateVirtualStateTransition();
                tOffState.SetDestination(offState);
                tOffState.ExitTime = null;
                tOffState.Duration = 0;
                
                foreach (var condition in entries)
                {
                    tOffState.Conditions = tOffState.Conditions.Add(new AnimatorCondition
                    {
                        parameter = condition.name,
                        mode = condition.GetMode().Inverse(),
                        threshold = Convert.ToSingle(condition.value)
                    });
                }
                
                onState.Transitions = onState.Transitions.Add(tOffState);
            }
        }

        /// <summary>
        /// 创建一个满足条件时的过渡
        /// </summary>
        /// <param name="conditions"></param>
        /// <param name="context"></param>
        /// <param name="controller"></param>
        /// <param name="state"></param>
        /// <param name="target"></param>
        /// <param name="exitTime"></param>
        /// <param name="duration"></param>
        public static void CreateConditionsTransitionTo(this ParameterOrConditions conditions,
            BuildContext context, VirtualAnimatorController controller, VirtualState state, VirtualState target,
            float? exitTime = null, float duration = 0f)
        {
            // 检查参数
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
            
            foreach (var entry in conditions)
            {
                var transition = context.CreateVirtualStateTransition();
                transition.SetDestination(target);
                transition.ExitTime = exitTime;
                transition.Duration = duration;
                
                foreach (var condition in entry.conditions)
                {
                    transition.Conditions = transition.Conditions.Add(new AnimatorCondition
                    {
                        parameter = condition.name,
                        mode = condition.GetMode(),
                        threshold = Convert.ToSingle(condition.value)
                    });
                }
                state.Transitions = state.Transitions.Add(transition);
            }
        }

        /// <summary>
        /// 创建一个不满足条件时的过渡 
        /// </summary>
        /// <param name="conditions"></param>
        /// <param name="context"></param>
        /// <param name="controller"></param>
        /// <param name="onState"></param>
        /// <param name="offState"></param>
        /// <param name="exitTime"></param>
        /// <param name="duration"></param>
        public static void CreateConditionsTransitionInverseTo(this ParameterOrConditions conditions,
            BuildContext context, VirtualAnimatorController controller, VirtualState onState, VirtualState offState, float? exitTime = null, float duration = 0f)
        {
            conditions = conditions.Inverse();
            
            // 检查参数
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
            
            foreach (var entries in conditions)
            {
                var tOffState = context.CreateVirtualStateTransition();
                tOffState.SetDestination(offState);
                tOffState.ExitTime = exitTime;
                tOffState.Duration = duration;
                
                foreach (var condition in entries.conditions)
                {
                    tOffState.Conditions = tOffState.Conditions.Add(new AnimatorCondition
                    {
                        parameter = condition.name,
                        mode = condition.GetMode(),
                        threshold = Convert.ToSingle(condition.value)
                    });
                }
                
                onState.Transitions = onState.Transitions.Add(tOffState);
            }
        }

        public static void CreateAnyStateConditionsTransition(this ParameterOrConditions conditions,
            BuildContext context, VirtualAnimatorController controller, VirtualLayer layer, VirtualState state, float? exitTime = null, float duration = 0f)
        {
            // 检查参数
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
            
            foreach (var entry in conditions)
            {
                var transition = context.CreateVirtualStateTransition();
                transition.SetDestination(state);
                transition.ExitTime = exitTime;
                transition.Duration = duration;
                
                foreach (var condition in entry.conditions)
                {
                    transition.Conditions = transition.Conditions.Add(new AnimatorCondition
                    {
                        parameter = condition.name,
                        mode = condition.GetMode(),
                        threshold = Convert.ToSingle(condition.value)
                    });
                }

                layer.GetStateMachine().AnyStateTransitions = layer.GetStateMachine().AnyStateTransitions.Add(transition);
            }
        }
        
        public static void CreateConditionsTransitionToExit(this ParameterOrConditions conditions,
            BuildContext context, VirtualAnimatorController controller, VirtualState state,
            float? exitTime = null, float duration = 0f)
        {
            // 检查参数
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
            
            foreach (var entry in conditions)
            {
                var transition = context.CreateVirtualStateTransition();
                transition.SetExitDestination();
                transition.ExitTime = exitTime;
                transition.Duration = duration;
                
                foreach (var condition in entry.conditions)
                {
                    transition.Conditions = transition.Conditions.Add(new AnimatorCondition
                    {
                        parameter = condition.name,
                        mode = condition.GetMode(),
                        threshold = Convert.ToSingle(condition.value)
                    });
                }
                state.Transitions = state.Transitions.Add(transition);
            }
        }
    }
}