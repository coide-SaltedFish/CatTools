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
using CatTools.Runtime.entity;
using JetBrains.Annotations;
using nadena.dev.ndmf.animator;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CatTools.editor.utils
{
    public static class AnimationUtils
    {
        /// <summary>
        ///     创建一个开关动画片段，指定对象、时长和开关状态
        /// </summary>
        /// <param name="root"></param>
        /// <param name="target">目标对象</param>
        /// <param name="duration">动画时长</param>
        /// <param name="toggle">开关状态</param>
        /// <returns></returns>
        public static AnimationClip CreateToggleAnimationClip(GameObject root, Transform target, bool toggle,
            float duration = 0f)
        {
            var clip = new AnimationClip
            {
                name = target.name + "_Toggle_" + toggle
                // hideFlags = HideFlags.HideAndDontSave // 不序列化到场景和预制体
            };
            var relativePath = CatToolsPath.GetRelativePath(root.transform, target);

            var startValue = toggle ? 0f : 1f; // 如果要开，则从 0（关）开始；要关则从 1（开）开始
            var endValue = toggle ? 1f : 0f; // 如果要开，则到 1（开）；要关则到 0（关）

            var curve = new AnimationCurve(
                new Keyframe(0f, startValue),
                new Keyframe(duration, endValue)
            );

            if (duration == 0f)
                curve = new AnimationCurve(
                    new Keyframe(0f, toggle ? 1f : 0f)
                );

            clip.SetCurve(
                relativePath,
                typeof(GameObject),
                "m_IsActive",
                curve
            );

            return clip;
        }

        /// <summary>
        /// 录制一个材质切换动画片段
        /// </summary>
        /// <param name="root"></param>
        /// <param name="target"></param>
        /// <param name="entrys"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static AnimationClip CreateMaterialChangeAnimationClip(GameObject root, Transform target,
            SkinnedMeshRendererMaterialSlotEntry[] entrys, float duration = 0f)
        {
            var clip = new AnimationClip
            {
                name = target.name + "_MaterialChange_" + CryptoRandomString.GetRandomString()
            };
            var relativePath = CatToolsPath.GetRelativePath(root.transform, target.transform);
            
            foreach (var entry in entrys)
            {
                var binding = EditorCurveBinding.PPtrCurve(relativePath, typeof(SkinnedMeshRenderer),
                    $"m_Materials.Array.data[{entry.index}]"
                );
                var keyframe = new ObjectReferenceKeyframe[1]
                {
                    new()
                    {
                        time  = duration,
                        value = entry.targetMaterial
                    }
                };
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframe);
            }
            return clip;
        }

        /// <summary>
        ///     返回给定 AnimatorConditionMode 的逻辑取反模式。
        /// </summary>
        public static AnimatorConditionMode Inverse(this AnimatorConditionMode mode)
        {
            switch (mode)
            {
                case AnimatorConditionMode.If:
                    return AnimatorConditionMode.IfNot;
                case AnimatorConditionMode.IfNot:
                    return AnimatorConditionMode.If;

                case AnimatorConditionMode.Equals:
                    return AnimatorConditionMode.NotEqual;
                case AnimatorConditionMode.NotEqual:
                    return AnimatorConditionMode.Equals;

                case AnimatorConditionMode.Greater:
                    return AnimatorConditionMode.Less;
                case AnimatorConditionMode.Less:
                    return AnimatorConditionMode.Greater;

                default:
                    throw new Exception("Unknown AnimatorConditionMode: " + mode);
            }
        }

        public static string GetLayerName(string name)
        {
            return $"CT_{name}_{CryptoRandomString.GetRandomString()}";
        }

        [CanBeNull]
        public static AnimatorControllerParameter GetParameterByName(this VirtualAnimatorController controller,
            string name)
        {
            return controller.Parameters.GetValueOrDefault(name);
        }
        
        /// <summary>
        /// 如果参数不存在，则添加
        /// 如果类型不一致，改为 float
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="parameter"></param>
        public static void AddParameterIfNot(this VirtualAnimatorController controller, AnimatorControllerParameter parameter)
        {
            var param = controller.GetParameterByName(parameter.name);
            if (param != null && param.type != parameter.type)
            {
                parameter.type = AnimatorControllerParameterType.Float;
            }

            controller.Parameters = controller.Parameters.Remove(parameter.name);
            controller.Parameters = controller.Parameters.Add(parameter.name, parameter);
        }
    }
}