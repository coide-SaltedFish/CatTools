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

using System.Linq;
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.animator.builder;
using io.github.sereinfish.cat.tools.editor.animator.builder.extensions;
using io.github.sereinfish.cat.tools.editor.context;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    public class ConditionalMaterialSetterHandler : ComponentHandler<ConditionalMaterialSetter>
    {
        public override void Execute(ICatContext context, ConditionalMaterialSetter entity)
        {
            if (entity.setters == null || entity.setters.Length == 0)
            {
                Debug.LogWarning($"ConditionalMaterialSetter {entity.name} 没有设置任何形态键操作，将不会执行任何操作");
                return;
            }

            var controller = context.GetAnimatorController(entity.layerType);
            var layer = ICatLayer.Create(context, $"ConditionalMaterialSetter_{StringHelper.GetRandomString()}")
                .AddToController(controller);

            var defaultClip = entity.restoreToggle
                ? AnimationBuilder.Create()
                    .SetSkinnedMeshRendererMaterials(context.AvatarRootTransform,
                        (from materialSetter in entity.setters
                            let smr = materialSetter.target.GetComponent<SkinnedMeshRenderer>()
                            where smr != null
                            select new ConditionalMaterialSetter.MaterialSetter
                            {
                                target = materialSetter.target, slot = materialSetter.slot,
                                material = smr.sharedMaterials[materialSetter.slot]
                            }).ToArray())
                    .Build()
                : null;
            var setterClip = AnimationBuilder.Create()
                .SetSkinnedMeshRendererMaterials(context.AvatarRootTransform, entity.setters)
                .Build();

            var defaultState = layer.AddState("Default", defaultClip);
            var setterState = layer.AddState("Setter", setterClip);

            entity.conditions.CreateConditionsTransition(context, controller, setterState, defaultState);
        }
    }
}