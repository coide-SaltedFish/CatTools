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
using System.Linq;
using Editor.Animator.BlendTree;
using io.github.sereinfish.cat.tools.Conditions;
using io.github.sereinfish.cat.tools.editor.utils;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context.bake
{
    public class CatBakeSingleBlendTree : ISingleBlendTree
    {
        private readonly BlendTree _blendTree = new BlendTree()
        {
            name = "SingleBlendTree",
            blendType = BlendTreeType.Direct
        };
        private readonly HashSet<string> _parameterNames = new();
        
        public bool Check(ParameterOrConditions conditions)
        {
            if (conditions == null) return false;
            if (conditions.conditions.Count is > 1 or 0) return false;
            if (conditions.conditions[0].conditions.Count is > 1 or 0) return false;
            var condition = conditions.conditions[0].conditions[0];
            return condition.mode is CatAnimatorConditionRuntimeMode.If or CatAnimatorConditionRuntimeMode.IfNot
                or CatAnimatorConditionRuntimeMode.Equals;
        }

        public bool Add(ParameterOrConditions conditions, AnimationClip clip)
        {
            if (Check(conditions).Not()) return false;
            var condition = conditions.conditions[0].conditions[0];
            
            // 如果是单 If、IfNot，直接添加
            if (condition.mode is CatAnimatorConditionRuntimeMode.If or CatAnimatorConditionRuntimeMode.IfNot)
            {
                var value = condition.value > 0f;
                if (condition.mode is CatAnimatorConditionRuntimeMode.IfNot) value = value.Not();
                
                _blendTree.AddChildMotion(new ChildMotion { motion = clip }, condition.name, value ? 1f : 0f);
                _parameterNames.Add(condition.name);
                return true;
            }
            
            // 如果是单 Equals，则创建 1DBlendTree
            if (condition.mode is CatAnimatorConditionRuntimeMode.Equals)
            {
                var childBlendTree = Get1DBlendTree(condition.name);
                if (childBlendTree == null)
                {
                    childBlendTree = Create1DBlendTree(condition.name);
                    childBlendTree.AddChildMotion(new ChildMotion { motion = clip }, condition.name, condition.value);
                }
                else
                {
                    childBlendTree.AddChildMotion(new ChildMotion { motion = clip }, condition.name, condition.value);
                    _blendTree.AddChildMotion(new ChildMotion { motion = childBlendTree }, "CatTools/isAlwaysTrue", 1f);
                }

                _parameterNames.Add(condition.name);
                return true;
            }
            return false;
        }

        [CanBeNull]
        private BlendTree Get1DBlendTree(string parameterName)
        {
            foreach (var blendTreeChild in _blendTree.children)
            {
                if (blendTreeChild.motion is not BlendTree childBlendTree) continue;
                if (childBlendTree.blendType == BlendTreeType.Simple1D && childBlendTree.blendParameter == parameterName)
                {
                    return childBlendTree;
                }
            }

            return null;
        }
        
        private BlendTree Create1DBlendTree(string parameterName)
        {
            var blendTree = new BlendTree
            {
                name = parameterName,
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false,
                blendParameter = parameterName
            };
            return blendTree;
        }
        
        public string[] GetParameterNames()
        {
            return _parameterNames.ToArray();
        }

        public T GetBlendTree<T>() where T : class
        {
            return _blendTree as T;
        }
    }
}