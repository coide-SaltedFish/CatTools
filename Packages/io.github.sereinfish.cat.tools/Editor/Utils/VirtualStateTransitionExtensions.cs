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
using io.github.sereinfish.cat.tools.Conditions;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class VirtualStateTransitionExtensions
    {
        public static VirtualStateTransition CreateVirtualStateTransition(this BuildContext context, Action<VirtualStateTransition> action = null)
        {
            var unityTransition = new AnimatorStateTransition();

            var transition = context
                .Extension<VirtualControllerContext>()
                .Clone(unityTransition);
            action?.Invoke(transition);
            return transition;
        }

        /// <summary>
        /// 合并条件到过渡
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="orConditions"></param>
        /// <returns></returns>
        public static VirtualStateTransition[] MergeParameterOrConditions(this VirtualStateTransition transition,
            ParameterOrConditions orConditions)
        {
            var ret = new List<VirtualStateTransition>();
            foreach (var conditions in orConditions)
            {
                var cloneTransition = (VirtualStateTransition) transition.Clone();
                
                var transitionConditions = cloneTransition.Conditions.ToList();
                // 编辑条件
                foreach (var condition in conditions)
                {
                    transitionConditions.Add(new AnimatorCondition
                    {
                        parameter = condition.name,
                        mode = condition.GetMode(),
                        threshold = Convert.ToSingle(condition.value)
                    });
                }
                
                cloneTransition.Conditions = transitionConditions.ToImmutableList();
                ret.Add(cloneTransition);
            }
            return ret.ToArray();
        }
    }
}