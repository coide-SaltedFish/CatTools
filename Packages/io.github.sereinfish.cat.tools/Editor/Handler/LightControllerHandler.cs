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
using System.Linq;
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.animator.builder;
using io.github.sereinfish.cat.tools.editor.context;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    public class LightControllerHandler: ComponentHandler<LightController>
    {
        public override void Execute(ICatContext context, LightController entity)
        {
            var controller = context.GetAnimatorController(VRCAvatarDescriptor.AnimLayerType.FX);
            var layer = ICatLayer.Create(context, $"CatLightController_{StringHelper.GetRandomString()}")
                .AddToController(controller);
            controller.AddParameterIfNot(entity.controllerParameterName, AnimatorControllerParameterType.Float, 0.5f);
            var animClip = CreateLightLimitAnimationClip(context, entity);
            layer.AddState("LightLimit", animClip)
                .SetTimeParameter(entity.controllerParameterName);
        }
        
        private List<Transform> GetLightTransforms(LightController entity)
        {
            if (!entity.includeChildren)
                return new List<Transform> { entity.transform };

            return entity.GetComponentsInChildren<Transform>(true).ToList();
        }
        
        private AnimationClip CreateLightLimitAnimationClip(ICatContext context, LightController entity)
        {
            return AnimationBuilder.Create()
                .Run(builder =>
                {
                    foreach (var transform in GetLightTransforms(entity))
                    {
                        var renderer = transform.GetComponent<Renderer>();
                        if (renderer == null) continue;
                        foreach (var material in renderer.sharedMaterials)
                        {
                            if (material == null) continue;
                            // 构建动画
                            var path = CatToolsPath.GetRelativePath(context.AvatarRootTransform, transform);
                            var _0hf112 = new Action<AnimationCurveBuilder>(curveBuilder =>
                            {
                                curveBuilder.AddKey(0, 0f);
                                curveBuilder.AddKey(15, 0.5f);
                                curveBuilder.AddKey(30, 1f);
                                curveBuilder.AddKey(50, 1f);
                                curveBuilder.AddKey(60, 2f);
                            });
                            builder.SetCurve(path, renderer.GetType(), "material._GlitterBrightness", curveBuilder =>
                            {
                                curveBuilder.AddKey(0, 0f);
                                curveBuilder.AddKey(15, 0.5f);
                                curveBuilder.AddKey(30, 1f);
                                curveBuilder.AddKey(50, 1f);
                                curveBuilder.AddKey(60, 1f);
                            });
                            builder.SetCurve(path, renderer.GetType(), "material._LightingAdditiveLimit", _0hf112);
                            builder.SetCurve(path, renderer.GetType(), "material._LightingCap", _0hf112);
                            builder.SetCurve(path, renderer.GetType(), "material._LightingMinLightBrightness", curveBuilder =>
                            {
                                curveBuilder.AddKey(0, 0f);
                                curveBuilder.AddKey(15, 0f);
                                curveBuilder.AddKey(30, 0.1f);
                                curveBuilder.AddKey(50, 1f);
                                curveBuilder.AddKey(60, 2f);
                            });
                            builder.SetCurve(path, renderer.GetType(), "material._RimEnviroIntensity", _0hf112);
                            
                            // liltoon
                            builder.SetCurve(path, renderer.GetType(), "material._LightMaxLimit", _0hf112);
                            builder.SetCurve(path, renderer.GetType(), "material._LightMinLimit", curveBuilder =>
                            {
                                curveBuilder.AddKey(0, 0f);
                                curveBuilder.AddKey(15, 0f);
                                curveBuilder.AddKey(30, 0.1f);
                                curveBuilder.AddKey(50, 1f);
                                curveBuilder.AddKey(60, 2f);
                            });
                        }
                    }
                }).Build();
        }
    }
}