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
using io.github.sereinfish.cat.tools.Conditions;

namespace io.github.sereinfish.cat.tools.editor.Conditions.Build
{
    public class ConditionsBuilder
    {
        private ParameterOrConditions _conditions = new();
        private ParameterConditions _currentConditions = null;
        
        private ConditionsBuilder()
        {
            
        }

        public static ConditionsBuilder Create()
        {
            return new ConditionsBuilder();
        }

        public ConditionsBuilder Or()
        {
            _currentConditions = new ParameterConditions();
            _conditions.Add(_currentConditions);
            return this;
        }

        public ConditionsBuilder ForEach<T>(IEnumerable<T> items, Action<ConditionsBuilder, T> action)
        {
            if (items == null || action == null) return this;
            InitCurrentConditions();
            foreach (var item in items)
            {
                action(this, item);
            }
            return this;
        }

        public ConditionsBuilder ForEach(IEnumerable<string> items, CatAnimatorConditionRuntimeMode mode, float value)
        {
            if (items == null) return this;
            InitCurrentConditions();
            foreach (var name in items)
            {
                _currentConditions.Add(new ParameterCondition
                {
                    mode = mode,
                    name = name,
                    value = value
                });
            }
            return this;
        }

        public ConditionsBuilder ForEach(IEnumerable<string> items, CatAnimatorConditionRuntimeMode mode, bool value)
        {
            return ForEach(items, mode, value ? 1f : 0f);
        }

        public ConditionsBuilder If(string name, bool value)
        {
            InitCurrentConditions();
            _currentConditions.Add(new ParameterCondition
            {
                mode = CatAnimatorConditionRuntimeMode.If,
                name = name,
                value = value ? 1f : 0f
            });
            return this;
        }
        
        public ConditionsBuilder IfNot(string name, bool value)
        {
            InitCurrentConditions();
            _currentConditions.Add(new ParameterCondition
            {
                mode = CatAnimatorConditionRuntimeMode.IfNot,
                name = name,
                value = value ? 1f : 0f
            });
            return this;
        }

        public ConditionsBuilder Greater(string name, float value)
        {
            InitCurrentConditions();
            _currentConditions.Add(new ParameterCondition
            {
                mode = CatAnimatorConditionRuntimeMode.Greater,
                name = name,
                value = value
            });
            return this;
        }
        
        public ConditionsBuilder Less(string name, float value)
        {
            InitCurrentConditions();
            _currentConditions.Add(new ParameterCondition
            {
                mode = CatAnimatorConditionRuntimeMode.Less,
                name = name,
                value = value
            });
            return this;
        }
        
        public ConditionsBuilder Equal(string name, float value)
        {
            InitCurrentConditions();
            _currentConditions.Add(new ParameterCondition
            {
                mode = CatAnimatorConditionRuntimeMode.Equals,
                name = name,
                value = value
            });
            return this;
        }
        
        public ConditionsBuilder NotEqual(string name, float value)
        {
            InitCurrentConditions();
            _currentConditions.Add(new ParameterCondition
            {
                mode = CatAnimatorConditionRuntimeMode.NotEqual,
                name = name,
                value = value
            });
            return this;
        }

        public ConditionsBuilder Run(Action<ConditionsBuilder> action)
        {
            action(this);
            return this;
        }
        
        public ParameterOrConditions Build()
        {
            return _conditions;
        }
        
        private void InitCurrentConditions()
        {
            if (_currentConditions != null) return;
            _currentConditions = new ParameterConditions();
            _conditions.Add(_currentConditions);
        }
    }
}