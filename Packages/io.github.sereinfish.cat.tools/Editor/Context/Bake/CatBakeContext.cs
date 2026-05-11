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
using System.IO;
using System.Linq;
using Editor.Animator.BlendTree;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace io.github.sereinfish.cat.tools.editor.context.bake
{
    public class CatBakeContext : ICatContext
    {
        private readonly VRCAvatarDescriptor _avatarDescriptor;
        private readonly string _outpath;
        private readonly Dictionary<VRCAvatarDescriptor.AnimLayerType, ISingleBlendTree> _blendTrees = new();

        public CatBakeContext(GameObject avatarRootObject, string outpath)
        {
            AvatarRootObject = avatarRootObject;
            AvatarRootTransform = avatarRootObject.transform;
            _avatarDescriptor = avatarRootObject.GetComponent<VRCAvatarDescriptor>();
            _outpath = outpath;
        }

        public Transform AvatarRootTransform { get; }
        public GameObject AvatarRootObject { get; }

        public ICatAnimatorController GetAnimatorController(VRCAvatarDescriptor.AnimLayerType type)
        {
            var layer = _avatarDescriptor.baseAnimationLayers.FirstOrDefault(l => l.type == type);
            var controller = layer.animatorController as AnimatorController;
            return new CatBakeAnimatorController(controller);
        }

        public VRCAvatarDescriptor GetAvatarDescriptor()
        {
            return _avatarDescriptor;
        }

        public ICatLayer CreateLayer(string name)
        {
            var layer = new CatBakeLayer(name);
            return layer;
        }

        public ICatStateTransition CreateTransition()
        {
            return new CatBakeStateTransition();
        }

        public void AssetSave(Object asset, string name = null)
        {
            if (asset == null) return;

            // 文件名优先用传入值，其次用 asset.name，最后随机名
            name = string.IsNullOrWhiteSpace(name) ? asset.name : name;
            if (string.IsNullOrWhiteSpace(name))
                name = StringHelper.GetRandomString();

            name = SanitizeFileName(Path.GetFileNameWithoutExtension(name));

            // 按类型分类存放
            var category = GetAssetCategory(asset);
            var savePath = CombineUnityPath(_outpath, category);

            if (string.IsNullOrEmpty(savePath)) return;

            if (!savePath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("folderPath 必须是相对于工程的路径，例如 Assets/Clips");

            if (!AssetDatabase.IsValidFolder(savePath))
                EnsureAssetFolder(savePath);

            // 自动后缀
            var ext = GetAssetExtension(asset);
            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{savePath}/{name}{ext}");

            try
            {
                if (asset is GameObject go)
                    // GameObject 必须保存成 prefab
                    PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                else
                    // 其他大多数 UnityEngine.Object 直接创建资产
                    AssetDatabase.CreateAsset(asset, assetPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"保存资产失败：{asset.name}\n{e}");
            }
        }

        public ISingleBlendTree GetSingleBlendTree(VRCAvatarDescriptor.AnimLayerType type)
        {
            if (_blendTrees.TryGetValue(type, out var tree)) return tree;

            _blendTrees[type] = new CatBakeSingleBlendTree();
            
            return _blendTrees[type];
        }

        public void AfterBuild()
        {
            // 构建 blendtree
            foreach (var (type, tree) in _blendTrees)
            {
                var controllerLayer = _avatarDescriptor.baseAnimationLayers.FirstOrDefault(l => l.type == type);
                var controller = (AnimatorController)controllerLayer.animatorController;
                
                // 初始化参数
                controller.TryAddParameterOrNot(new AnimatorControllerParameter
                {
                    name = "CatTools/isAlwaysTrue",
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = 1f,
                    defaultBool = true,
                    defaultInt = 1
                });
                foreach (var directBlendParameter in tree.GetParameterNames())
                {
                    controller.TryAddParameterOrNot(new AnimatorControllerParameter
                    {
                        name = directBlendParameter,
                        type = AnimatorControllerParameterType.Float,
                        defaultFloat = 0
                    });
                }
                
                var layer = new AnimatorControllerLayer
                {
                    name = "Cat_BlendTree",
                    defaultWeight = 1f,
                    blendingMode = AnimatorLayerBlendingMode.Override,
                    stateMachine = new AnimatorStateMachine()
                };
                controller.AddLayer(layer);
                
                var sm = layer.stateMachine;
                var state = sm.AddState("BlendTree");
                state.motion = tree.GetBlendTree<BlendTree>();
            }
        }

        private static string GetAssetCategory(Object asset)
        {
            return asset switch
            {
                GameObject => "Prefabs",
                AnimationClip => "Animations",
                Material => "Materials",
                Texture2D => "Textures",
                Sprite => "Sprites",
                AudioClip => "Audio",
                Mesh => "Meshes",
                Font => "Fonts",
                Shader => "Shaders",
                RenderTexture => "RenderTextures",
                ScriptableObject => "ScriptableObjects",
                _ => asset.GetType().Name
            };
        }

        private static string GetAssetExtension(Object asset)
        {
            return asset switch
            {
                GameObject => ".prefab",
                AnimationClip => ".anim",
                Material => ".mat",
                Shader => ".shader",
                PhysicsMaterial2D => ".physicsMaterial2D",
                // 这些类型用 .asset 最稳，不会因为导出格式不兼容而失败
                Texture2D => ".asset",
                Sprite => ".asset",
                AudioClip => ".asset",
                Mesh => ".asset",
                Font => ".asset",
                RenderTexture => ".asset",
                ScriptableObject => ".asset",
                _ => ".asset"
            };
        }

        private static string CombineUnityPath(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a)) return b;
            if (string.IsNullOrWhiteSpace(b)) return a;
            return $"{a.TrimEnd('/', '\\')}/{b.TrimStart('/', '\\')}".Replace("\\", "/");
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return StringHelper.GetRandomString();

            foreach (var c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c.ToString(), "_");

            return fileName.Trim();
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            var name = Path.GetFileName(folderPath);

            if (!AssetDatabase.IsValidFolder(parent)) EnsureAssetFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}