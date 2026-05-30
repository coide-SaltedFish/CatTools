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
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.inspector.ui;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(TargetsConditionalBlendShapeSetter))]
    public class TargetsConditionalBlendShapeSetterInspector : ConditionalEditor<TargetsConditionalBlendShapeSetter>
    {
        private ShapeChangeList _shapeChangeList;
        private SerializedProperty _shapeChangeInfosProp;
        private SerializedProperty _restoreToggleProp;
        
        protected override void Init()
        {
            base.Init();
            // _shapeChangeInfosProp = PropGet(nameof(ConditionalBlendShapeSetter.shapeChangeInfos));
            _restoreToggleProp = PropGet(nameof(ConditionalBlendShapeSetter.restoreToggle));
            _shapeChangeList = new ShapeChangeList(GetAvatarRoot(), serializedObject);
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            
            _shapeChangeList.DoLayout();
            EditorGUILayout.PropertyField(_restoreToggleProp, new GUIContent("条件不满足时恢复为默认值"));
        }

        private void OnDestroy()
        {
            _shapeChangeList?.OnDisableWindow();
        }

        private void OnDisable()
        {
            _shapeChangeList?.OnDisableWindow();
        }
    }
}