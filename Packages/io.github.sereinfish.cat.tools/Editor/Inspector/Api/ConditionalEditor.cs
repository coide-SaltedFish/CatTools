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

using io.github.sereinfish.cat.tools.editor.inspector.ui;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    public abstract class ConditionalEditor<T> : AnimLayerEditor<T> where T : ConditionalBehaviour
    {
        // protected SerializedProperty ConditionsProp;

        private ParameterConditionList<ConditionalBehaviour> _parameterConditionList;
        protected T Target => (T)target;

        protected override void Init()
        {
            // ConditionsProp = PropGet(nameof(ConditionalBehaviour.conditions));
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            DrawConditions();
        }

        protected void DrawConditions()
        {
            _parameterConditionList ??= new ParameterConditionList<ConditionalBehaviour>(serializedObject);
            _parameterConditionList.DoLayout();
        }
    }
}