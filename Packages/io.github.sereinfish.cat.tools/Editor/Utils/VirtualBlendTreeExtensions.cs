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

using nadena.dev.ndmf.animator;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class VirtualBlendTreeExtensions
    {
        public static void AddChildMotion(this VirtualBlendTree blendTree, VirtualMotion clip, string name, float weight)
        {
            var child = new VirtualBlendTree.VirtualChildMotion
            {
                Motion = clip,
                DirectBlendParameter = name,
                Threshold = weight,
            };
            blendTree.Children = blendTree.Children.Add(child);
        }
    }
}