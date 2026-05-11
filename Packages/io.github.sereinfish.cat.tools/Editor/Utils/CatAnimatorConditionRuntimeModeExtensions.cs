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
using io.github.sereinfish.cat.tools.Conditions;
using UnityEditor.Animations;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class CatAnimatorConditionRuntimeModeExtensions
    {
        public static AnimatorConditionMode ToEdit(this CatAnimatorConditionRuntimeMode mode)
        {
            return mode switch
            {
                CatAnimatorConditionRuntimeMode.If => AnimatorConditionMode.If,
                CatAnimatorConditionRuntimeMode.IfNot => AnimatorConditionMode.IfNot,
                CatAnimatorConditionRuntimeMode.Greater => AnimatorConditionMode.Greater,
                CatAnimatorConditionRuntimeMode.Less => AnimatorConditionMode.Less,
                CatAnimatorConditionRuntimeMode.Equals => AnimatorConditionMode.Equals,
                CatAnimatorConditionRuntimeMode.NotEqual => AnimatorConditionMode.NotEqual,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
        
        public static AnimatorConditionMode GetMode(this ParameterCondition entry)
        {
            var mode = entry.mode.ToEdit();
            if (mode is not (AnimatorConditionMode.If or AnimatorConditionMode.IfNot)) return mode;
            if (entry.value == 0f)
            {
                mode = mode.Inverse();
            }

            return mode;
        }
        
        public static AnimatorConditionMode Inverse(this AnimatorConditionMode mode)
        {
            return mode switch
            {
                AnimatorConditionMode.If => AnimatorConditionMode.IfNot,
                AnimatorConditionMode.IfNot => AnimatorConditionMode.If,
                AnimatorConditionMode.Equals => AnimatorConditionMode.NotEqual,
                AnimatorConditionMode.NotEqual => AnimatorConditionMode.Equals,
                AnimatorConditionMode.Greater => AnimatorConditionMode.Less,
                AnimatorConditionMode.Less => AnimatorConditionMode.Greater,
                _ => throw new Exception("Unknown AnimatorConditionMode: " + mode)
            };
        }
    }
}