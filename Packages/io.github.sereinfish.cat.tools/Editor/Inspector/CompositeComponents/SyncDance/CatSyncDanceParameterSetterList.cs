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
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    public class CatSyncDanceParameterSetterList
    {
        private string _name;
        private readonly SerializedProperty _data;
        private readonly ReorderableList _list;
        
        public CatSyncDanceParameterSetterList(string name, SerializedProperty data)
        {
            _name = name;
            _data = data;
            _list = new ReorderableList(_data.serializedObject, _data, true, true, true, true)
            {
                drawElementCallback = DrawElementCallback,
                elementHeightCallback = GetElementHeight,
                drawHeaderCallback = rect => GUI.Label(rect, _name)
            }; 
            
        }
        
        private float GetElementHeight(int index)
        {
            var prop = _data.GetArrayElementAtIndex(index);
            var type = prop.FindPropertyRelative(nameof(CatSyncDanceParameterSetterEntry.parameterType));
            var lineH = EditorGUIUtility.singleLineHeight + 2f;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            
            return type.enumValueIndex switch
            {
                (int)CatSyncDanceParameterSetterEntry.ParameterSetterType.Set
                    or (int)CatSyncDanceParameterSetterEntry.ParameterSetterType.Add
                    or (int)CatSyncDanceParameterSetterEntry.ParameterSetterType.Copy => lineH * 4 + spacing * 3,
                (int)CatSyncDanceParameterSetterEntry.ParameterSetterType.Random =>
                    lineH * 5 + spacing * 4,
                _ => throw new ArgumentException("Invalid type")
            };
        }
        
        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var lineH = EditorGUIUtility.singleLineHeight + 2f;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var x = rect.x;
            var y = rect.y;
            
            var prop = _data.GetArrayElementAtIndex(index);
            var nameProp = prop.FindPropertyRelative("parameterName");
            var typeProp = prop.FindPropertyRelative("parameterType");
            var setterType = prop.FindPropertyRelative("parameterSetterType");
            var sourceParameterNameProp = prop.FindPropertyRelative("sourceParameterName");
            var valueProp = prop.FindPropertyRelative("value");
            
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), nameProp, new GUIContent("目标参数"));
            y += lineH + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), typeProp, new GUIContent("参数类型"));
            y += lineH + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), setterType, new GUIContent("参数设置类型"));
            y += lineH + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), sourceParameterNameProp, new GUIContent("源参数"));
            if (setterType.enumValueIndex == (int)CatSyncDanceParameterSetterEntry.ParameterSetterType.Copy)
            {
                y += lineH + spacing;
                EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), valueProp, new GUIContent("值")); 
            }
        }
         
        public void DoLayoutList()
        {
            _list.DoLayoutList();
        }
    }
}