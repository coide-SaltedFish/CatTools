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
    
    [CustomEditor(typeof(ConditionalParameterSetter))]
    public class ConditionalParameterSetterInspector : ConditionalEditor<ConditionalParameterSetter>
    {
        private SerializedProperty _parameterSettersProperty;
        private SerializedProperty _continuousSettingProperty;
        private ReorderableList _parameterSettersReorderableList;
        
        protected override void Init()
        {
            base.Init();
            _parameterSettersProperty = PropGet(nameof(ConditionalParameterSetter.parameterSetters));
            _continuousSettingProperty = PropGet(nameof(ConditionalParameterSetter.continuousSetting));
            InitList();
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            EditorGUILayout.PropertyField(_continuousSettingProperty, new GUIContent("持续执行", tooltip:"当勾选此项时，将会持续触发对参数的设置，反之则只会在条件满足时执行一次"));
            _parameterSettersReorderableList.DoLayoutList();
        }

        private void InitList()
        {
            _parameterSettersReorderableList =
                new ReorderableList(serializedObject, _parameterSettersProperty, true, true, true, true)
                {
                    drawElementCallback = DrawElement,
                    elementHeightCallback = GetElementHeight
                };
        }
        
        private float GetElementHeight(int index)
        {
            var prop = _parameterSettersProperty.GetArrayElementAtIndex(index);
            var type = prop.FindPropertyRelative(nameof(ConditionalParameterSetter.ParameterSetter.type));
            return type.enumValueIndex switch
            {
                (int)ConditionalParameterSetter.ParameterSetterType.Set
                    or (int)ConditionalParameterSetter.ParameterSetterType.Add
                    or (int)ConditionalParameterSetter.ParameterSetterType.Copy => (EditorGUIUtility.singleLineHeight +
                        2f) * 3 + EditorGUIUtility.standardVerticalSpacing,
                (int)ConditionalParameterSetter.ParameterSetterType.Random =>
                    (EditorGUIUtility.singleLineHeight + 2f) * 4 + EditorGUIUtility.standardVerticalSpacing,
                _ => throw new ArgumentException("Invalid type")
            };
        }
        
        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var lineH = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var prop = _parameterSettersProperty.GetArrayElementAtIndex(index);

            var type = prop.FindPropertyRelative(nameof(ConditionalParameterSetter.ParameterSetter.type));
            var source = prop.FindPropertyRelative(nameof(ConditionalParameterSetter.ParameterSetter.source));
            var destination = prop.FindPropertyRelative(nameof(ConditionalParameterSetter.ParameterSetter.destination));
            var value = prop.FindPropertyRelative(nameof(ConditionalParameterSetter.ParameterSetter.value));
            var maxValue = prop.FindPropertyRelative(nameof(ConditionalParameterSetter.ParameterSetter.maxValue));
            var minValue = prop.FindPropertyRelative(nameof(ConditionalParameterSetter.ParameterSetter.minValue));

            var x = rect.x;
            var y = rect.y + 1f;
            
            switch (type.enumValueIndex)
            {
                case (int) ConditionalParameterSetter.ParameterSetterType.Set:
                case (int) ConditionalParameterSetter.ParameterSetterType.Add:
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), type);
                    y += lineH + spacing;
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), destination);
                    y += lineH + spacing;
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), value);
                    break;
                case (int) ConditionalParameterSetter.ParameterSetterType.Random:
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), type);
                    y += lineH + spacing;
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), destination);
                    y += lineH + spacing;
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), minValue);
                    y += lineH + spacing;
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), maxValue);
                    break;
                case (int) ConditionalParameterSetter.ParameterSetterType.Copy:
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), type);
                    y += lineH + spacing;
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), source);
                    y += lineH + spacing;
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), destination);
                    break;
                default:
                    throw new ArgumentException("Invalid type");
            }
        }
    }
}