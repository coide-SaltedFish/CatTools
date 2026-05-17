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

using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.animator.builder.extensions
{
    public static class SkinnedMeshRendererAnimatorExtensions
    {
        public static AnimationBuilder SetSkinnedMeshRendererMaterial(this AnimationBuilder builder, Transform avatarRoot, Transform target,
            int slot, Material material)
        {
            var inPath = CatToolsPath.GetRelativePath(avatarRoot, target);
            var binding = EditorCurveBinding.PPtrCurve(inPath, typeof(SkinnedMeshRenderer),
                PropertyName.MaterialsSlotData(slot)
            );
            var keyframe = new ObjectReferenceKeyframe[]
            {
                new()
                {
                    time = 0f,
                    value = material
                }
            };
            AnimationUtility.SetObjectReferenceCurve(builder.Clip, binding, keyframe);

            return builder;
        }
        
        public static AnimationBuilder SetSkinnedMeshRendererMaterials(this AnimationBuilder builder, Transform avatarRoot, ConditionalMaterialSetter.MaterialSetter[] setters)
        {
            foreach (var materialSetter in setters)
            {
                builder.SetSkinnedMeshRendererMaterial(avatarRoot, materialSetter.target, materialSetter.slot, materialSetter.material);
            }
            return builder;
        }
    }
}