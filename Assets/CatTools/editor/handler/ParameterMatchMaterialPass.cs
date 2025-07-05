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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CatTools.editor.utils;
using CatTools.Runtime;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor;
using UnityEngine;

namespace CatTools.editor.handler
{
    public class ParameterMatchMaterialPass : Pass<ParameterMatchMaterialPass>
    {
        private AnimatorServicesContext _asc;
        private CloneContext _cc;
        
        protected override void Execute(BuildContext context)
        {
            var targets = context.AvatarRootTransform.GetComponentsInChildren<ParameterMatchMaterial
            >(true);
            _asc = context.Extension<AnimatorServicesContext>();

            var services = context.Extension<AnimatorServicesContext>();
            _cc = services.ControllerContext.CloneContext;
            
            // 移除 targets 中为null的值
            targets = targets.Where(x => x != null).ToArray();
            if (targets.Length == 0)
            {
                throw new ArgumentException("No ParameterMatchMaterial component found.");
            }

            foreach (var target in targets) ProcessComponent(context, target);
        }
        
        
        private void ProcessComponent(BuildContext context, ParameterMatchMaterial target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            
            var controller = _asc.ControllerContext.Controllers[target.layerType];
            // 创建动画剪辑
            var clip = CreateAnimationClip(context, target);

            var layerName = AnimationUtils.GetLayerName($"{target.transform.name}_ParameterMatchMaterial");
            var layer = VirtualLayer.Create(_cc, layerName);
            controller.AddLayer(LayerPriority.Default, layer);
            
            var sm = layer.StateMachine!;
            
            // 两个状态，一个初始 Empty 状态，一个动画状态，都写入默认值
            var emptyState = sm.AddState("empty", position:new Vector3(300, 0));
            emptyState.WriteDefaultValues = true;
            var clipState = sm.AddState("clip", clip, new Vector3(300, 120));
            clipState.WriteDefaultValues = true;
            
            sm.DefaultState = emptyState;
            
            target.conditions.CreateConditionsTransition(controller, clipState, emptyState);
        }

        private VirtualClip CreateAnimationClip(BuildContext context, ParameterMatchMaterial target)
        {
            var smr = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var clip = new AnimationClip
            {
                name = target.transform.name + "_ParameterMatchMaterial_" + CryptoRandomString.GetRandomString()
            };
            
            // 遍历所有 SkinnerMeshRenderer 的材质槽，读取材质路径，进行匹配
            foreach (var renderer in smr)
            {
                var relativePath = CatToolsPath.GetRelativePath(context.AvatarRootTransform, renderer.gameObject.transform);
                for (var i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var itemSharedMaterial = renderer.sharedMaterials[i];
                    if (itemSharedMaterial == null) continue;
                    
                    // 判断同目录下是否存在匹配材质球
                    var path = AssetDatabase.GetAssetPath(itemSharedMaterial);
                    Debug.Log($"[NDFM] mat {itemSharedMaterial.name} => '{path}'");
                    
                    var dir = Path.GetDirectoryName(path);
                    var baseName = Path.GetFileNameWithoutExtension(path);

                    // 1) 按照 {name} 分割
                    var parts = target.nameRegex.Split(new[] { "{name}" }, StringSplitOptions.None);
                    // 2) 对文字部分做转义
                    var before = Regex.Escape(parts[0]);
                    var after = Regex.Escape(parts.Length > 1 ? parts[1] : "");
                    // 3) 插入被转义的 baseName
                    var pattern = before + Regex.Escape(baseName) + after;

                    if (string.IsNullOrEmpty(dir)) continue;
                    var guids = AssetDatabase.FindAssets("t:Material", new[] { dir });
                    foreach (var guid in guids)
                    {
                        var matPath = AssetDatabase.GUIDToAssetPath(guid);
                        var matName = Path.GetFileNameWithoutExtension(matPath);
                        // 忽略大小写匹配
                        if (!Regex.IsMatch(matName, pattern)) continue;
                        var binding = EditorCurveBinding.PPtrCurve(relativePath, typeof(SkinnedMeshRenderer),
                            $"m_Materials.Array.data[{i}]"
                        );
                        var keyframe = new ObjectReferenceKeyframe[]
                        {
                            new()
                            {
                                time  = 0f,
                                value = AssetDatabase.LoadAssetAtPath<Material>(matPath)
                            }
                        };
                        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframe);
                    }
                }
            }

            return VirtualClip.Clone(_cc, clip);
        }
    }
}