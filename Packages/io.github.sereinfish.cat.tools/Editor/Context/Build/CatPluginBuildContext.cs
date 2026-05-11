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

using System.Collections.Generic;
using Editor.Animator.BlendTree;
using io.github.sereinfish.cat.tools.editor.utils;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using nadena.dev.ndmf.vrchat;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace io.github.sereinfish.cat.tools.editor.context.build
{
    public class CatPluginBuildContext: ICatContext
    {
        private readonly BuildContext _context;
        private readonly AnimatorServicesContext _asc;
        private readonly CloneContext _cloneContext;
        private readonly Dictionary<VRCAvatarDescriptor.AnimLayerType, ISingleBlendTree> _blendTrees = new();
        
        public Transform AvatarRootTransform => _context.AvatarRootTransform;
        public GameObject AvatarRootObject => _context.AvatarRootObject;

        public CatPluginBuildContext(BuildContext context)
        {
            _context = context;
            _asc = _context.Extension<AnimatorServicesContext>();
            _cloneContext = _asc.ControllerContext.CloneContext;
        }
        
        public ICatAnimatorController GetAnimatorController(VRCAvatarDescriptor.AnimLayerType type)
        {
            return new CatPluginBuildAnimatorController(_cloneContext, _asc.ControllerContext.Controllers[type]);
        }

        public VRCAvatarDescriptor GetAvatarDescriptor()
        {
            return _context.VRChatAvatarDescriptor();
        }

        public ICatLayer CreateLayer(string name)
        {
            return CatPluginBuildLayer.Create(_cloneContext, name);
        }

        public ICatStateTransition CreateTransition()
        {
            return CatPluginBuildStateTransition.Create();
        }

        public void AssetSave(Object asset, string name = null)
        {
            // pass
        }

        public ISingleBlendTree GetSingleBlendTree(VRCAvatarDescriptor.AnimLayerType type)
        {
            if (_blendTrees.TryGetValue(type, out var tree)) return tree;
            
            var blendTree = VirtualBlendTree.Create("Cat_BlendTree");
            blendTree.BlendType = BlendTreeType.Simple1D;
            blendTree.UseAutomaticThresholds = false;
            
            _blendTrees.Add(type, new CatPluginBuildSingleBlendTree(_cloneContext));
            
            return _blendTrees[type];
        }

        public void AfterBuild()
        {
            // 构建 blendtree
            foreach (var (type, tree) in _blendTrees)
            {
                var animatorController = _asc.ControllerContext.Controllers[type];
                var layer = VirtualLayer.Create(_cloneContext, "Cat_BlendTree");
                layer.DefaultWeight = 1f;
                animatorController.AddLayer(LayerPriority.Default, layer);
                var sm = layer.StateMachine;
                // 初始化参数
                animatorController.TryAddParameterOrNot(new AnimatorControllerParameter
                {
                    name = "CatTools/isAlwaysTrue",
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = 1f,
                    defaultBool = true,
                    defaultInt = 1
                });
                foreach (var directBlendParameter in tree.GetParameterNames())
                {
                    animatorController.TryAddParameterOrNot(new AnimatorControllerParameter()
                    {
                        name = directBlendParameter,
                        type = AnimatorControllerParameterType.Float,
                        defaultFloat = 0
                    });
                }
                
                sm?.AddState("BlendTree", tree.GetBlendTree<VirtualBlendTree>());
            }
        }
    }
}