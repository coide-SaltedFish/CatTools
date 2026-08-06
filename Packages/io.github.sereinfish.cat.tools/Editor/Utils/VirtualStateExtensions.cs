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
using nadena.dev.ndmf.animator;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class VirtualStateExtensions
    {
        public static T CreateScriptableObject<T>(this VirtualState state, Action<T> setup = null) where T : StateMachineBehaviour 
        {
            var obj = ScriptableObject.CreateInstance<T>();
            setup?.Invoke(obj);
            state.Behaviours = state.Behaviours.Add(obj);
            return obj;
        }
    }
}