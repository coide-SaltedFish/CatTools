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
using io.github.sereinfish.cat.tools.Conditions;
using io.github.sereinfish.cat.tools.editor.Animator.BlendTree;
using io.github.sereinfish.cat.tools.editor.utils;
using JetBrains.Annotations;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context.build
{
    public class CatPluginBuildSingleBlendTree : ISingleBlendTree
    {
        private readonly VirtualBlendTree _blendTree = VirtualBlendTree.Create("SingleBlendTree");
        private readonly HashSet<string> _parameterNames = new();
        private readonly CloneContext _cloneContext;
        
        public CatPluginBuildSingleBlendTree(CloneContext cloneContext)
        {
            _cloneContext = cloneContext;
            _blendTree.BlendType = BlendTreeType.Direct;
        }
        
        /// <summary>
        /// 检查条件是否符合单条件
        /// 且为 If、IfNot、Equal
        /// </summary>
        /// <param name="conditions"></param>
        /// <returns></returns>
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
            var ifCondition = IsIf(condition)?.Value;
            if (ifCondition != null)
            {
                _blendTree.AddChildMotion(_cloneContext.Clone(clip), condition.name, ifCondition.Value ? 1f : 0f);
                _parameterNames.Add(condition.name);
                return true;
            }
            
            // 如果是单 Equals，检查是否已有 1DBlendTree，否则创建
            if (condition.mode is CatAnimatorConditionRuntimeMode.Equals)
            {
                var childBlendTree = Get1DBlendTree(condition.name);
                if (childBlendTree == null)
                {
                    childBlendTree = Create1DBlendTree(condition.name);
                    childBlendTree.AddChildMotion(_cloneContext.Clone(clip), condition.name, condition.value);
                    _blendTree.AddChildMotion(childBlendTree, "CatTools/isAlwaysTrue", 1f);
                    _parameterNames.Add(condition.name);
                }
                else
                {
                    childBlendTree.AddChildMotion(_cloneContext.Clone(clip), condition.name, condition.value);
                    _parameterNames.Add(condition.name);
                }
                return true;
            }
            return false;
        }

        public string[] GetParameterNames()
        {
            return _parameterNames.ToArray();
        }

        public T GetBlendTree<T>() where T : class
        {
            return _blendTree as T;
        }

        
        
        [CanBeNull]
        private VirtualBlendTree Get1DBlendTree(string parameterName)
        {
            foreach (var childMotion in _blendTree.Children)
            {
                if (childMotion.Motion is not VirtualBlendTree childBlendTree) continue;
                if (childBlendTree.BlendType == BlendTreeType.Simple1D && childBlendTree.BlendParameter == parameterName)
                {
                    return childBlendTree;
                }
            }

            return null;
        }
        
        [CanBeNull]
        private VirtualBlendTree Create1DBlendTree(string parameterName)
        {
            var blendTree = VirtualBlendTree.Create(parameterName);
            blendTree.BlendType = BlendTreeType.Simple1D;
            blendTree.BlendParameter = parameterName;
            blendTree.UseAutomaticThresholds = false;
            return blendTree;
        }

        private KeyValuePair<string, bool>? IsIf(ParameterCondition condition)
        {
            if (condition.mode is not (CatAnimatorConditionRuntimeMode.If or CatAnimatorConditionRuntimeMode.IfNot))
                return null;
            
            var value = condition.value > 0f;
            if (condition.mode is CatAnimatorConditionRuntimeMode.IfNot) value = value.Not();
            return new KeyValuePair<string, bool>(condition.name, value);
        }
    }
}