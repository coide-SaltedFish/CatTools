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
using CatTools.Runtime.utils;
using UnityEditor.Animations;

namespace CatTools.editor.utils
{
    public static class CatToolsAnimatorConditionModeUtils
    {
        public static AnimatorConditionMode ToAnimatorConditionMode(this CatToolsAnimatorConditionMode mode)
        {
            return mode switch
            {
                CatToolsAnimatorConditionMode.If => AnimatorConditionMode.If,
                CatToolsAnimatorConditionMode.IfNot => AnimatorConditionMode.IfNot,
                CatToolsAnimatorConditionMode.Greater => AnimatorConditionMode.Greater,
                CatToolsAnimatorConditionMode.Less => AnimatorConditionMode.Less,
                CatToolsAnimatorConditionMode.Equals => AnimatorConditionMode.Equals,
                CatToolsAnimatorConditionMode.NotEqual => AnimatorConditionMode.NotEqual,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
    }
}