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
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.animator.builder
{
    public class AnimationBuilder
    {
        public readonly AnimationClip Clip = new()
        {
            name = "Clip"
        };
        
        public AnimationBuilder Name(string name)
        {
            Clip.name = name;
            return this;
        }

        public AnimationBuilder SetCurve(string path, Type type, string propertyName, Action<AnimationCurveBuilder> action)
        {
            var curveBuilder = new AnimationCurveBuilder();
            action(curveBuilder);
            Clip.SetCurve(path, typeof(GameObject), "m_IsActive", curveBuilder.Build());
            return this;
        }

        public AnimationBuilder Run(Action<AnimationBuilder> action)
        {
            action(this);
            return this;
        }
        
        public AnimationClip Build()
        {
            return Clip;
        }
        
        public static AnimationBuilder Create()
        {
            return new AnimationBuilder();
        }
    }
}