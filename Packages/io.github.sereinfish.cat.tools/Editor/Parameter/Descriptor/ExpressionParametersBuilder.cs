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
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace io.github.sereinfish.cat.tools.editor.Parameter.Descriptor
{
    /// <summary>
    /// 用来对 ExpressionParameters 进行批量构建操作
    /// </summary>
    public class ExpressionParametersBuilder
    {
        private VRCAvatarDescriptor _descriptor;
        private VRCExpressionParameters _parameters;
        private List<VRCExpressionParameters.Parameter> _parametersList;
        
        public ExpressionParametersBuilder(VRCAvatarDescriptor descriptor)
        {
            _descriptor = descriptor;
            _parameters = descriptor.expressionParameters;
            _parametersList = new List<VRCExpressionParameters.Parameter>(_parameters.parameters);
        }
        
        public ExpressionParametersBuilder Add(VRCExpressionParameters.Parameter parameter)
        {
            _parametersList.Add(parameter);
            return this;
        }
        
        public ExpressionParametersBuilder Add(string name, VRCExpressionParameters.ValueType valueType, float defaultValue, bool saved, bool networkSynced = true)
        {
            _parametersList.Add(new VRCExpressionParameters.Parameter
            {
                name = name,
                defaultValue = defaultValue,
                valueType = valueType,
                saved = saved,
                networkSynced = networkSynced
            });
            return this;
        }

        public ExpressionParametersBuilder Remove(string name)
        {
            _parametersList.RemoveAll(x => x.name == name);
            return this;
        }
        
        public ExpressionParametersBuilder Remove(VRCExpressionParameters.Parameter parameter)
        {
            _parametersList.Remove(parameter);
            return this;
        }
        
        public ExpressionParametersBuilder Clear()
        {
            _parametersList.Clear();
            return this;
        }
        
        public ExpressionParametersBuilder Contains(string name, ref bool contains)
        {
            contains = _parametersList.Exists(x => x.name == name);
            return this;
        }
        
        public ExpressionParametersBuilder Find(string name, Action<VRCExpressionParameters.Parameter> action)
        {
            action(_parametersList.Find(x => x.name == name));
            return this;
        }
        
        public ExpressionParametersBuilder IfExists(string name, Action<ExpressionParametersBuilder> run, Action<ExpressionParametersBuilder> not)
        {
            if (_parametersList.Exists(x => x.name == name))
            {
                run(this);
            }
            else
            {
                not(this);
            }
            return this;
        }

        public ExpressionParametersBuilder Run(Action<ExpressionParametersBuilder> action)
        {
            action(this);
            return this;
        }

        public ExpressionParametersBuilder ForEach<T>(IEnumerable<T> items, Action<ExpressionParametersBuilder, T> action)
        {
            if (items == null || action == null) return this;

            foreach (var item in items)
            {
                action(this, item);
            }
            return this;
        }

        public void Build()
        {
            _parameters.parameters = _parametersList.ToArray();
        }
    }
}