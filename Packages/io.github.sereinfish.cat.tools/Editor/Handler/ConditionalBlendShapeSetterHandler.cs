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
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.animator.builder;
using io.github.sereinfish.cat.tools.editor.animator.builder.extensions;
using io.github.sereinfish.cat.tools.editor.context;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    public class ConditionalBlendShapeSetterHandler : ComponentHandler<ConditionalBlendShapeSetter>
    {
        public override void Execute(ICatContext context, ConditionalBlendShapeSetter entity)
        {
            // if (entity.targets == null || entity.targets.Length == 0)
            // {
            //     Debug.LogWarning($"ConditionalBlendShapeSetter {entity.name} 没有设置任何目标，将不会执行任何操作");
            //     return;
            // }
            if (entity.shapeChangeInfos == null || entity.shapeChangeInfos.Length == 0)
            {
                Debug.LogWarning($"ConditionalBlendShapeSetter {entity.name} 没有设置任何形态键操作，将不会执行任何操作");
                return;
            }
            
            var controller = context.GetAnimatorController(entity.layerType);
            var layer = ICatLayer.Create(context, $"ConditionalBlendShapeSetter_{StringHelper.GetRandomString()}")
                .AddToController(controller);
            // 当不需要恢复默认值时，empty -> change -> default -> empty
            // 反之default -> chang
            
            var defaultClip = AnimationBuilder.Create()
                .Run(builder =>
                {
                    foreach (var shapeChangeInfo in entity.shapeChangeInfos)
                    {
                        switch (shapeChangeInfo.changeType)
                        {
                            case ConditionalBlendShapeSetter.ShapeChangeType.Set:
                                var smr = shapeChangeInfo.target.GetComponent<SkinnedMeshRenderer>();
                                var defaultValueOrNull = smr.GetBlendShapeValueByName(shapeChangeInfo.shapeName);
                                if (defaultValueOrNull.HasValue)
                                {
                                    builder.SetBlendShape(context.AvatarRootTransform, shapeChangeInfo.target, shapeChangeInfo.shapeName,
                                        defaultValueOrNull.Value);
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"无法处理的 ShapeChangeType {shapeChangeInfo.changeType}");
                        }
                    }
                })
                .Build();
            var changeClip = AnimationBuilder.Create()
                .Run(builder =>
                {
                    foreach (var shapeChangeInfo in entity.shapeChangeInfos)
                    {
                        switch (shapeChangeInfo.changeType)
                        {
                            case ConditionalBlendShapeSetter.ShapeChangeType.Set:
                                builder.SetBlendShape(context.AvatarRootTransform, shapeChangeInfo.target, shapeChangeInfo.shapeName,
                                    shapeChangeInfo.value);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"无法处理的 ShapeChangeType {shapeChangeInfo.changeType}");
                        }
                    }
                })
                .Build();

            var defaultState = layer.StateMachine.AddState("Default", defaultClip);
            var changeState = layer.StateMachine.AddState("Change", changeClip);
            if (entity.restoreToggle)
            {
                layer.StateMachine.DefaultState = defaultState;
                entity.conditions.CreateConditionsTransition(context, controller, changeState, defaultState);
            }
            else
            {
                var emptyState = layer.StateMachine.AddState("Empty");
                layer.StateMachine.DefaultState = emptyState;
                // empty -> change
                entity.conditions.CreateConditionsTransitionTo(context, controller, emptyState, changeState);
                // change -> default
                entity.conditions.CreateConditionsTransitionInverseTo(context, controller, changeState, defaultState);
                // default -> empty
                entity.conditions.CreateConditionsTransitionInverseTo(context, controller, defaultState, emptyState);
            }
        }
    }
}