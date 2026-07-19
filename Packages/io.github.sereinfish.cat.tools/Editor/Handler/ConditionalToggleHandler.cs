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
using nadena.dev.ndmf;
using nadena.dev.ndmf.localization;
using UnityEngine;
using PropertyName = io.github.sereinfish.cat.tools.editor.animator.builder.PropertyName;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    public class ConditionalToggleHandler : ComponentHandler<ConditionalToggle>
    {
        public override void Execute(ICatContext context, ConditionalToggle entity)
        {
            if (entity is SelfConditionalToggle) entity.targets = new[] { entity.gameObject.transform };
            if (entity.targets == null || entity.targets.Length == 0 || entity.targets.All(t => t == null))
            {
                ErrorReport.ReportError(new Localizer(
                    "en-US",
                    () => new List<LocalizationAsset>()),
                    ErrorSeverity.NonFatal,
                    "io.github.sereinfish.cat.tools.editor.handler.ConditionalToggleHandler",
                    entity.transform,
                    entity.targets
                    );
                return;
            }
            
            var handlerTargets = new HashSet<Transform>();
            foreach (var target in entity.targets)
            {
                if (target == null) continue;
                handlerTargets.Add(target);
            }
            
            var controller = context.GetAnimatorController(entity.layerType); // 获取动画控制器
            var layer = ICatLayer
                .Create(context, $"Toggle/{StringHelper.GetRandomString()}")
                .AddToController(controller); // 创建层
            // 设置目标默认状态
            if (entity.isSetDefaultActive)
            {
                foreach (var entityTarget in entity.targets)
                {
                    entityTarget.gameObject.SetActive(entity.defaultActive);
                }
            }
            // 构建动画
            var clipOn = AnimationBuilder.Create()
                .Run(builder =>
                {
                    foreach (var targetPath in handlerTargets.Select(target => CatToolsPath.GetRelativePath(context.AvatarRootTransform, target.transform)))
                    {
                        builder.SetCurve(targetPath, typeof(GameObject), PropertyName.ObjIsActive, curveBuilder =>
                        {
                            curveBuilder.AddKey(new Keyframe(0f, entity.toggle ? 1f : 0f));
                        });
                    }
                })
                .Build();
            var clipOff = entity.reverseToggle
                ? AnimationBuilder.Create()
                    .Run(builder =>
                    {
                        foreach (var targetPath in handlerTargets.Select(target => CatToolsPath.GetRelativePath(context.AvatarRootTransform, target.transform)))
                        {
                            builder.SetCurve(targetPath, typeof(GameObject), PropertyName.ObjIsActive, curveBuilder =>
                            {
                                curveBuilder.AddKey(new Keyframe(0f, entity.toggle ? 0f : 1f));
                            });
                        }
                    })
                    .Build()
                : null;
            // 创建状态
            var stateOn = layer.AddState("On", clipOn);
            var stateOff = layer.AddState("Off", clipOff);
            // 创建过渡
            layer.StateMachine.DefaultState = entity.toggle ? stateOff : stateOn;
            entity.conditions.CreateConditionsTransition(context, controller, stateOn, stateOff);
        }
    }
}