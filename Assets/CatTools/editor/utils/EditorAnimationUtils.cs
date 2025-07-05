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
using CatTools.Runtime.entity;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;

namespace CatTools.editor.utils
{
    public static class EditorAnimationUtils
    {
        /// <summary>
        /// 创建条件过渡
        /// 满足条件指向 onState ，否则指向 offState
        /// </summary>
        /// <param name="conditions"></param>
        /// <param name="controller"></param>
        /// <param name="onState"></param>
        /// <param name="offState"></param>
        public static void CreateConditionsTransition(this List<ParameterConditionsEntry> conditions,
            VirtualAnimatorController controller, VirtualState onState, VirtualState offState)
        {
            var tOnState = VirtualStateTransition.Create();
            tOnState.SetDestination(offState);
            tOnState.ExitTime = null;
            tOnState.Duration = 0;

            onState.Transitions = onState.Transitions.Add(tOnState);

            // 遍历或条件
            foreach (var entry in conditions)
            {
                var tOffState = VirtualStateTransition.Create();
                tOffState.SetDestination(onState);
                tOffState.ExitTime = null;
                tOffState.Duration = 0;

                offState.Transitions = offState.Transitions.Add(tOffState);

                // 遍历与条件
                foreach (var condition in entry.conditions)
                {
                    // 找不到参数时添加同名 float 参数
                    if (controller.Parameters.All(p => p.Value.name != condition.name))
                        controller.Parameters = controller.Parameters.Add(condition.name,
                            new AnimatorControllerParameter
                            {
                                name = condition.name,
                                type = AnimatorControllerParameterType.Float
                            });

                    // 添加条件到过渡
                    var mode = condition.condition.ToAnimatorConditionMode();
                    if (mode is AnimatorConditionMode.If or AnimatorConditionMode.IfNot)
                        if (condition.value == "0")
                            mode = mode.Inverse();

                    tOffState.Conditions = tOffState.Conditions.Add(new AnimatorCondition
                    {
                        mode = mode,
                        threshold = Convert.ToSingle(condition.value),
                        parameter = condition.name
                    });
                    tOnState.Conditions = tOnState.Conditions.Add(new AnimatorCondition
                    {
                        mode = mode.Inverse(), // 条件取反
                        threshold = Convert.ToSingle(condition.value),
                        parameter = condition.name
                    });
                }
            }
        }
    }
}