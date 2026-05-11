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

using io.github.sereinfish.cat.tools.editor.utils;
using nadena.dev.ndmf.util;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.animator.builder.extensions
{
    public static class BlendShapeAnimatorExtensions
    {
        public static AnimationBuilder SetBlendShape(this AnimationBuilder builder, Transform root, Transform target, string blendShapeName,
            float value)
        {
            var path = CatToolsPath.GetRelativePath(root, target);
            var curve = new AnimationCurve(new Keyframe(0f, value));
            var binding = EditorCurveBinding.FloatCurve(path, typeof(SkinnedMeshRenderer), $"blendShape.{blendShapeName}");
            AnimationUtility.SetEditorCurve(builder.Clip, binding, curve);
            return builder;
        }
    }   
}