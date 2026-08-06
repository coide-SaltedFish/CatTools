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
using io.github.sereinfish.cat.tools.editor.utils;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using PropertyName = io.github.sereinfish.cat.tools.editor.animator.builder.PropertyName;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    public class ConditionalMatchMaterialsSetterHandler : ComponentHandler<ConditionalMatchMaterialsSetter>
    {
        public override void Execute(BuildContext context, ConditionalMatchMaterialsSetter entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var controller = context.GetAnimatorController(entity.layerType);
            var layer = controller.AddLayer($"ConditionalMatchMaterialsSetter/{StringHelper.GetRandomString()}");
            var sm = layer.GetStateMachine();

            var onClip = AnimationBuilder.Create()
                .Run(builder =>
                {
                    var smrs = entity.GetComponentsInChildren<Renderer>(true);
                    // 遍历所有 SkinnerMeshRenderer 的材质槽，读取材质路径，进行匹配
                    foreach (var renderer in smrs)
                    {
                        var relativePath = CatToolsPath.GetRelativePath(context.AvatarRootTransform, renderer.transform);
                        for (var i = 0; i < renderer.sharedMaterials.Length; i++)
                        {
                            var itemSharedMaterial = renderer.sharedMaterials[i];
                            if (itemSharedMaterial == null) continue;
                            
                            var targetMaterials = entity.FindTargetsMaterial(itemSharedMaterial);
                            if (targetMaterials.Length > 1)
                            {
                                Debug.LogWarning($"找到过多的目标材质，在 {relativePath} 的 {itemSharedMaterial}");
                                continue;
                            }
                            if (targetMaterials.Length == 0)
                            {
                                Debug.LogWarning($"未找到目标材质，在 {relativePath} 的 {itemSharedMaterial}");
                                // 如果启用材质处理脚本，则尝试处理材质
                                var autoHandler = GetAutoHandler(entity, itemSharedMaterial);
                                // 当启用材质处理脚本，且未找到目标材质时，尝试使用自动处理材质
                                if ((autoHandler == null && entity.materialHandler != null) ||
                                    autoHandler is { materialHandler: not null })
                                {
                                    var handler = autoHandler?.materialHandler ?? entity.materialHandler;
                                    Material output = null;
                                    try
                                    {
                                        output = handler.HandleMaterial(itemSharedMaterial);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"材质处理失败：{e.Message}");
                                    }
                                    if (output == null) continue;
                                    targetMaterials = new[] { output };
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            var targetMaterial = targetMaterials[0];
                            
                            builder.SetObjectReferenceCurve(relativePath, renderer.GetType(), PropertyName.MaterialsSlotData(i), new ObjectReferenceKeyframe[]
                            {
                                new()
                                {
                                    time = 0f,
                                    value = targetMaterial
                                }
                            });
                        }
                    }
                }).Build().ToVirtualMotion(context);

            var defaultClip = AnimationBuilder.Create()
                .Run(builder =>
                {
                    var smrs = entity.GetComponentsInChildren<Renderer>(true);
                    // 遍历所有 SkinnerMeshRenderer 的材质槽
                    foreach (var renderer in smrs)
                    {
                        var relativePath = CatToolsPath.GetRelativePath(context.AvatarRootTransform, entity.transform);
                        for (var i = 0; i < renderer.sharedMaterials.Length; i++)
                        {
                            var itemSharedMaterial = renderer.sharedMaterials[i];
                            if (itemSharedMaterial == null) continue;

                            builder.SetObjectReferenceCurve(relativePath, renderer.GetType(), PropertyName.MaterialsSlotData(i), new ObjectReferenceKeyframe[]
                            {
                                new()
                                {
                                    time = 0f,
                                    value = itemSharedMaterial
                                }
                            });
                        }
                    }
                }).Build().ToVirtualMotion(context);

            var onState = sm.AddState("on", onClip);
            var defaultState = sm.AddState("default", defaultClip);
            var emptyState = sm.AddState("empty");

            sm.DefaultState = emptyState;
            
            entity.conditions.CreateConditionsTransitionTo(context, controller, emptyState, onState);
            entity.conditions.CreateConditionsTransitionInverseTo(context, controller, onState, defaultState);
            entity.conditions.CreateConditionsTransitionInverseTo(context, controller, defaultState, emptyState);
        }
        
        private ConditionalMatchMaterialsSetter.AutoHandleMaterial GetAutoHandler(ConditionalMatchMaterialsSetter entity, Material material)
        {
            foreach (var targetAutoHandleMaterial in entity.autoHandleMaterials)
            {
                var sourceMaterialPath = GlobalObjectId.GetGlobalObjectIdSlow(material).ToString();
                if (targetAutoHandleMaterial.materialPath == sourceMaterialPath)
                {
                    return targetAutoHandleMaterial;
                }
            }
            return null;
        }
    }
}