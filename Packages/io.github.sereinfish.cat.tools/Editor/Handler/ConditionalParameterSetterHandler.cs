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
using io.github.sereinfish.cat.tools.editor.Conditions.Build;
using io.github.sereinfish.cat.tools.editor.context;
using io.github.sereinfish.cat.tools.editor.context.Extensions;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    public class ConditionalParameterSetterHandler : ComponentHandler<ConditionalParameterSetter>
    {
        public override void Execute(ICatContext context, ConditionalParameterSetter entity)
        {
            var controller = context.GetAnimatorController(entity.layerType);
            var layer = ICatLayer.Create(context, $"CatConditionalParameterSetter_{StringHelper.GetRandomString()}")
                .AddToController(controller);
            // 构建状态
            var emptyState = layer.AddState("Empty");
            layer.DefaultState = emptyState;
            var setState = layer.AddState("Set");
            setState.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
            {
                foreach (var setter in entity.parameterSetters)
                {
                    switch(setter.type)
                    {
                        case ConditionalParameterSetter.ParameterSetterType.Set:
                            driver.AddParameterDriverSet(setter.destination, setter.value);
                            break;
                        case ConditionalParameterSetter.ParameterSetterType.Add:
                            driver.AddParameterDriverAdd(setter.destination, setter.value);
                            break;
                        case ConditionalParameterSetter.ParameterSetterType.Copy:
                            driver.AddParameterDriverCopy(setter.destination, setter.source);
                            break;
                        case ConditionalParameterSetter.ParameterSetterType.Random:
                            driver.AddParameterDriverRandom(setter.destination, setter.minValue, setter.maxValue);
                            break;  
                        default:
                            throw new ArgumentOutOfRangeException(nameof(setter.type));
                    };
                }
            });
            
            if (entity.continuousSetting)
            {
                var exitPleaseName = $"exitPlease_{StringHelper.GetRandomString()}";
                controller.AddParameterIfNot(exitPleaseName, false);
                // empty => set
                entity.conditions.CreateConditionsTransitionTo(context, controller, emptyState, setState);
                // set => empty
                var exitPleaseCondition = ConditionsBuilder.Create()
                    .If(exitPleaseName, false)
                    .Build();
                exitPleaseCondition.CreateConditionsTransitionTo(context, controller, setState, emptyState);
            }
            else
            {
                entity.conditions.CreateConditionsTransition(context, controller, setState, emptyState);
            }
        }
    }
}