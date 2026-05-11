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

using io.github.sereinfish.cat.tools.Components;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(SelfConditionalToggle))]
    public class SelfConditionalToggleInspector : ConditionalEditor<SelfConditionalToggle>
    {
        private SerializedProperty _defaultActiveProp;
        private SerializedProperty _isSetDefaultActiveProp;
        private SerializedProperty _reverseToggleProp;
        private SerializedProperty _toggleProp;

        protected override void Init()
        {
            base.Init();
            _toggleProp = PropGet(nameof(SelfConditionalToggle.toggle));
            _reverseToggleProp = PropGet(nameof(SelfConditionalToggle.reverseToggle));
            _defaultActiveProp = PropGet(nameof(SelfConditionalToggle.defaultActive));
            _isSetDefaultActiveProp = PropGet(nameof(SelfConditionalToggle.isSetDefaultActive));
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            EditorGUILayout.PropertyField(_toggleProp, new GUIContent("条件满足时状态"));
            EditorGUILayout.PropertyField(_reverseToggleProp, new GUIContent("条件不满足时是否反转状态"));
            EditorGUILayout.PropertyField(_isSetDefaultActiveProp, new GUIContent("构建时设置默认状态"));
            if (_isSetDefaultActiveProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(_defaultActiveProp, new GUIContent("构建时将开关状态设置为"));
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }
    }
}