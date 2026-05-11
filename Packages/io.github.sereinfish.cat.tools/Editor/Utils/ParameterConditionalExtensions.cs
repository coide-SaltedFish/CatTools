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

using System.Linq;
using io.github.sereinfish.cat.tools.Conditions;
using io.github.sereinfish.cat.tools.utils;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class ParameterConditionalExtensions
    {
        /// <summary>
        /// 条件反转
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static ParameterOrConditions Inverse(this ParameterOrConditions original)
        {
            if (original == null || original.conditions.Count == 0)
                return new ParameterOrConditions();

            // 1) 将所有组内的条件“铺平”
            var allConditions = original
                .Where(group => group.conditions != null)
                .SelectMany(group => group.conditions)
                .ToList();

            // 2) 对每一个条件取反
            var invertedConditions = allConditions
                .Select(c =>
                {
                    var mode = c.mode;
                    if (c.mode is not (CatAnimatorConditionRuntimeMode.If or CatAnimatorConditionRuntimeMode.IfNot))
                        return new ParameterCondition
                        {
                            name = c.name,
                            value = c.value,
                            mode = mode.Inverse()
                        };
                
                    return new ParameterCondition
                    {
                        name = c.name,
                        value = c.value,
                        mode = c.value == 0f ? mode : mode.Inverse()
                    };
                })
                .ToList();

            // 3) 返回一个只包含上述所有取反条件的一组 ParameterConditionsEntry
            return new ParameterOrConditions
            {
                new ParameterConditions
                {
                    conditions = invertedConditions
                }
            };
        }
    }
}