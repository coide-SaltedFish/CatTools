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
using System.Linq;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.Conditions
{
    [Serializable]
    public class ParameterOrConditions
    {
        public List<ParameterConditions> conditions;
        
        public override bool Equals(object obj)
        {
            return obj is ParameterOrConditions parameterOrConditions 
                   && (from conditions in conditions from otherConditions in parameterOrConditions.conditions where conditions.Equals(otherConditions) select conditions).Any();
        }
    }
    
    [Serializable]
    public class ParameterConditions
    { 
        public List<ParameterCondition> conditions;

        public override bool Equals(object obj)
        {
            if (obj is not ParameterConditions parameterConditions) return false;
            return conditions.Count == parameterConditions.conditions.Count && conditions.TrueForAll(x => parameterConditions.conditions.Contains(x));
        }
    }
    
    [Serializable]
    public class ParameterCondition
    {
        public string name; // 参数名
        public CatAnimatorConditionRuntimeMode mode; // 参数条件
        public float value = 0f; // 参数值
        
        public override bool Equals(object obj)
        {
            if (obj is not ParameterCondition parameterCondition) return false;
            if (ReferenceEquals(this, parameterCondition)) return true;

            if (mode == CatAnimatorConditionRuntimeMode.If && parameterCondition.mode == CatAnimatorConditionRuntimeMode.IfNot)
            {
                return name == parameterCondition.name &&
                    value == 0 && parameterCondition.value != 0 || (value != 0 && parameterCondition.value == 0);
            }
            return name == parameterCondition.name &&
                   mode == parameterCondition.mode &&
                   Mathf.Approximately(value, parameterCondition.value);
        }
    }
}