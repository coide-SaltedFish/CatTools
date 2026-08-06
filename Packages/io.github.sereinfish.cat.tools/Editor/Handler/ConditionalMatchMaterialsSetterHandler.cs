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
using System.Linq;
using HarmonyLib;
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.animator.builder;
using io.github.sereinfish.cat.tools.editor.inspector;
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
                    
                    // var smrs = entity.GetComponentsInChildren<MeshRenderer>(true).Select(s => s as Renderer).ToArray();
                    // smrs = smrs.AddRangeToArray(entity.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s => s as Renderer).ToArray());
                    var smrs = entity.GetComponentsInChildren<Renderer>(entity.includeChildren);
                    // 遍历所有 SkinnerMeshRenderer 的材质槽，读取材质路径，进行匹配
                    foreach (var renderer in smrs)
                    {
                        var relativePath = CatToolsPath.GetRelativePath(context.AvatarRootTransform, renderer.transform);
                        for (var i = 0; i < renderer.sharedMaterials.Length; i++)
                        {
                            var itemSharedMaterial = renderer.sharedMaterials[i];
                            if (itemSharedMaterial == null) continue;
                            
                            var autoHandler = GetAutoHandler(entity, itemSharedMaterial);
                            Material handledMaterial = null;
                            var targetMaterials = Array.Empty<Material>();

                            // 用户对该材质显式配置了材质处理器：始终用处理器处理源材质，忽略匹配到的目标材质
                            if (autoHandler is { materialHandler: not null })
                            {
                                if (!TryHandleMaterial(autoHandler.materialHandler, itemSharedMaterial, out handledMaterial)) continue;
                                targetMaterials = new[] { handledMaterial };
                            }
                            else
                            {
                                targetMaterials = entity.FindTargetsMaterial(itemSharedMaterial);
                                if (targetMaterials.Length > 1)
                                {
                                    Debug.LogWarning($"找到过多的目标材质，在 {relativePath} 的 {itemSharedMaterial}");
                                    continue;
                                }
                                if (targetMaterials.Length == 0)
                                {
                                    Debug.LogWarning($"未找到目标材质，在 {relativePath} 的 {itemSharedMaterial}");
                                    // 用户未配置（或设置为 None）：无替换目标时，若设置了全局材质处理器则用其处理源材质
                                    if (entity.materialHandler == null) continue;
                                    if (!TryHandleMaterial(entity.materialHandler, itemSharedMaterial, out handledMaterial)) continue;
                                    targetMaterials = new[] { handledMaterial };
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
            // 组件尚未初始化 autoHandleMaterials 时直接返回 null
            if (entity.autoHandleMaterials == null) return null;
            foreach (var targetAutoHandleMaterial in entity.autoHandleMaterials)
            {
                var sourceMaterialPath = ConditionalMatchMaterialsSetterInspector.GetMaterialPath(material);
                if (targetAutoHandleMaterial.materialPath == sourceMaterialPath)
                {
                    return targetAutoHandleMaterial;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 调用材质处理器处理源材质；处理失败或返回空材质时返回 false
        /// </summary>
        private static bool TryHandleMaterial(ConditionalMatchMaterialsSetter.IMaterialHandler handler, Material source, out Material output)
        {
            output = null;
            try
            {
                output = handler.HandleMaterial(source);
            }
            catch (Exception e)
            {
                Debug.LogError($"材质处理失败：{e.Message}");
            }
            return output != null;
        }
    }
}