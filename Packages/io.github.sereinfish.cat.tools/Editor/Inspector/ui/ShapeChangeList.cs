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
using System.Linq;
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.inspector.window;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector.ui
{
    public class ShapeChangeList
    {
        private readonly GameObject _avatarRoot;
        private readonly ReorderableList _list;
        private readonly SerializedProperty _rootListProp;
        private readonly SerializedObject _so;
        private ShapeChangeInfoPickerWindow _window;

        public ShapeChangeList(GameObject avatarRoot, SerializedObject so)
        {
            _so = so;
            _avatarRoot = avatarRoot;
            _rootListProp = _so.FindProperty("shapeChangeInfos");
            if (_rootListProp == null)
                throw new NullReferenceException("尝试在非ShapeChange组件中使用ShapeChangeList，没有找到 shapeChangeInfos 字段");

            _list = new ReorderableList(so, _rootListProp, true, false, true, true)
            {
                drawElementCallback = DrawElement,
                elementHeightCallback = GetElementHeight,
                onAddCallback = AddOuterGroup
            };
        }

        public void DoLayout()
        {
            _list.DoLayoutList();
        }

        private float GetElementHeight(int index)
        {
            return (EditorGUIUtility.singleLineHeight + 2f) * 2 + EditorGUIUtility.standardVerticalSpacing;
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var lineH = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var prop = _rootListProp.GetArrayElementAtIndex(index);

            var targetProp = prop.FindPropertyRelative(nameof(ConditionalBlendShapeSetter.ShapeChangeInfo.target));
            var nameProp = prop.FindPropertyRelative(nameof(ConditionalBlendShapeSetter.ShapeChangeInfo.shapeName));
            var typeProp = prop.FindPropertyRelative(nameof(ConditionalBlendShapeSetter.ShapeChangeInfo.changeType));
            var valueProp = prop.FindPropertyRelative(nameof(ConditionalBlendShapeSetter.ShapeChangeInfo.value));

            var x = rect.x;
            var y = rect.y + 1f;
            // 第一行，目标对象及 ChangeType
            var lastTarget = targetProp.objectReferenceValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(x, y, rect.width * 0.7f, lineH), targetProp, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                if (targetProp.objectReferenceValue != lastTarget)
                {
                    if (GetShapeNames(targetProp.objectReferenceValue as Transform).All(n => n != nameProp.stringValue))
                    {
                        nameProp.stringValue = null;
                    }
                }
            }
            
            x += rect.width * 0.7f + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width * 0.3f, lineH), typeProp, GUIContent.none);
            x = rect.x;
            y += lineH + 2f + spacing;
            // 第二行 ShapeName 及 Value
            var options = GetShapeNames(targetProp.objectReferenceValue as Transform);
            // var currentIndex = Mathf.Max(0, Array.IndexOf(options, nameProp.stringValue));
            // EditorGUI.BeginChangeCheck();
            // var newIndex = EditorGUI.Popup(new Rect(x, y, rect.width * 0.7f, lineH), currentIndex, options);
            if (GUI.Button(new Rect(x, y, rect.width * 0.7f, lineH), nameProp.stringValue, EditorStyles.popup))
            {
                var dropdown = new ShapeDropdown(options, selected =>
                {
                    nameProp.stringValue = selected;
                    nameProp.serializedObject.ApplyModifiedProperties();
                });

                dropdown.Show(new Rect(x, y, rect.width * 0.7f, lineH));
            }
            // if (EditorGUI.EndChangeCheck())
            // {
            //     if (newIndex >= 0 && newIndex < options.Length)
            //     {
            //         nameProp.stringValue = options[newIndex];
            //     }
            // }
            // EditorGUI.PropertyField(new Rect(x, y, rect.width * 0.7f, lineH), nameProp, GUIContent.none);
            x += rect.width * 0.7f + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width * 0.3f, lineH), valueProp, GUIContent.none);
        }
        
        private string[] GetShapeNames(Transform target)
        {
            var mesh = target.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            var shapeNames = new string[mesh.blendShapeCount];
            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                shapeNames[i] = mesh.GetBlendShapeName(i);
            }

            return shapeNames;
        }

        private void AddOuterGroup(ReorderableList list)
        {
            _window ??= ShapeChangeInfoPickerWindow.Open(_avatarRoot, info =>
            {
                var serializedObject = list.serializedProperty.serializedObject;
                serializedObject.Update();
                var prop = list.serializedProperty;
                var index = prop.arraySize;
                prop.InsertArrayElementAtIndex(index);
                var newGroup = prop.GetArrayElementAtIndex(index);
                newGroup.FindPropertyRelative(nameof(ConditionalBlendShapeSetter.ShapeChangeInfo.target))
                    .objectReferenceValue = info.target;
                newGroup.FindPropertyRelative(
                        nameof(ConditionalBlendShapeSetter.ShapeChangeInfo.shapeName))
                    .stringValue = info.shapeName;
                var typeProp = newGroup.FindPropertyRelative(
                    nameof(ConditionalBlendShapeSetter.ShapeChangeInfo.changeType));
                typeProp.enumValueIndex = (int)info.changeType;
                newGroup.FindPropertyRelative(
                        nameof(ConditionalBlendShapeSetter.ShapeChangeInfo.value))
                    .floatValue = info.value;
                serializedObject.ApplyModifiedProperties();
                list.index = index;
                GUI.changed = true;
            });
        }

        public void OnDisableWindow()
        {
            if (_window != null) _window.ClosePicker();
            _window = null;
        }
    }
}