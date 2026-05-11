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
using io.github.sereinfish.cat.tools.editor.utils;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context.bake
{
    public class CatBakeBlendTree : ICatBlendTree
    {
        private readonly BlendTree _blendTree;
        private readonly HashSet<string> _parameterNames = new();
        
        public CatBakeBlendTree(BlendTree blendTree)
        {
            _blendTree = blendTree;
        }

        public bool Check(ParameterOrConditions conditions)
        {
            return conditions.SelectMany(parameterConditions => parameterConditions)
                .All(parameterCondition => parameterCondition.mode is not (
                    CatAnimatorConditionRuntimeMode.Greater
                    or CatAnimatorConditionRuntimeMode.Less
                    or CatAnimatorConditionRuntimeMode.NotEqual
                    ));
        }

        public bool Add(ParameterOrConditions conditions, AnimationClip clip)
        {
            if (!Check(conditions)) return false;
            foreach (var parameterConditions in conditions)
            {
                var blendTree = _blendTree;
                for (var i = 0; i < parameterConditions.conditions.Count; i++)
                {
                    var condition = parameterConditions.conditions[i];
                    float weight;
                    switch (condition.mode)
                    {
                        case CatAnimatorConditionRuntimeMode.If:
                            weight = condition.GetValueBool().ToFloat();
                            break;
                        case CatAnimatorConditionRuntimeMode.IfNot:
                            weight = condition.GetValueBool().Not().ToFloat();
                            break;
                        case CatAnimatorConditionRuntimeMode.Equals:
                            weight = condition.value;
                            break;
                        case CatAnimatorConditionRuntimeMode.Greater:
                        case CatAnimatorConditionRuntimeMode.Less:
                        case CatAnimatorConditionRuntimeMode.NotEqual:
                        default:
                            return false;
                    }
                    _parameterNames.Add(condition.name);
                    if (i == parameterConditions.conditions.Count - 1)
                    {
                        // 到末尾
                        var motion = new ChildMotion
                        {
                            motion = clip
                        };
                        blendTree.AddChildMotion(motion, condition.name, weight);
                    }
                    else
                    {
                        // 添加子树，并且设置条件
                        var childBlendTree = new ChildMotion
                        {
                            motion = new BlendTree
                            {
                                blendType = BlendTreeType.Simple1D,
                                useAutomaticThresholds = false
                            }
                        };
                        blendTree.AddChildMotion(childBlendTree, condition.name, weight);
                    }
                }
            }
            return true;
        }

        public string[] GetDirectBlendParameters()
        {
            return _parameterNames.ToArray();
        }

        public T GetBlendTree<T>() where T : class
        {
            return _blendTree as T;
        }
    }
}