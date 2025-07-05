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
using System.Globalization;
using CatTools.Runtime.utils;

namespace CatTools.Runtime.entity
{
    /// <summary>
    ///     参数条件
    /// </summary>
    [Serializable]
    public class ParameterConditionsEntry
    {
        public List<ParameterConditionEntry> conditions;
    }

    [Serializable]
    public class ParameterConditionEntry
    {
        public string name; // 参数名
        public CatToolsAnimatorConditionMode condition; // 参数条件
        public string value = "0"; // 参数值

        /// <summary>
        ///     当 condition 为 If、IfNot 时，value 为 0 或 1
        ///     其余时候，value 为 int 或 float
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool ValueTypeChack(ParameterConditionEntry input)
        {
            if (string.IsNullOrWhiteSpace(input.value))
                return false;

            if (input.condition is CatToolsAnimatorConditionMode.If or CatToolsAnimatorConditionMode.IfNot)
                return input.value is "0" or "1";

            var isInt = int.TryParse(input.value, out _);
            var isFloat = float.TryParse(input.value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

            return isInt || isFloat;
        }
    }
}