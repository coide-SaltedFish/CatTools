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
using CatTools.editor.utils;
using CatTools.Runtime.utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CatTools.editor.ui
{
    /// <summary>
    ///     一个参数条件列表
    ///     支持与条件和或条件
    ///     接受的 SerializedProperty 必须是 List&lt;ParameterConditionEntry&gt;
    /// </summary>
    public class ParameterConditionList
    {
        private readonly ReorderableList _outerList;
        private readonly SerializedProperty _rootListProp;

        public ParameterConditionList(SerializedObject so, string propertyName)
        {
            _rootListProp = so.FindProperty(propertyName)
                            ?? throw new ArgumentException($"Property '{propertyName}' not found.");
            _outerList = new ReorderableList(so, _rootListProp, true, true, true, true)
            {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawOuterElement,
                elementHeightCallback = GetOuterElementHeight,
                onAddCallback = AddOuterGroup
            };
        }

        public void DoLayout()
        {
            _rootListProp.serializedObject.Update();
            _outerList.DoLayoutList();
            _rootListProp.serializedObject.ApplyModifiedProperties();
        }

        private static void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "参数条件列表（ OR ）");
        }

        private float GetOuterElementHeight(int index)
        {
            var lineH = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var totalH = lineH + spacing * 2;
            var groupProp = _rootListProp.GetArrayElementAtIndex(index);
            var condsProp = groupProp.FindPropertyRelative("conditions");
            if (condsProp == null || !condsProp.isArray)
                return totalH + lineH;

            var count = condsProp.arraySize;
            totalH += count * (lineH + spacing);
            totalH += lineH + spacing;
            totalH += spacing * 2;

            var minH = (lineH + spacing) * 2 + spacing * 4;
            return Mathf.Max(totalH, minH);
        }

        private void DrawOuterElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var lineH = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var groupProp = _rootListProp.GetArrayElementAtIndex(index);
            var condsProp = groupProp.FindPropertyRelative("conditions");

            var titleR = new Rect(rect.x, rect.y + spacing, rect.width, lineH);
            EditorGUI.LabelField(titleR, $"与条件组 {index + 1}");

            if (condsProp != null && condsProp.isArray)
            {
                for (var i = 0; i < condsProp.arraySize; i++)
                {
                    var entryProp = condsProp.GetArrayElementAtIndex(i);
                    var entryR = new Rect(rect.x,
                        titleR.y + (lineH + spacing) * (i + 1),
                        rect.width,
                        lineH);
                    DrawConditionEntry(entryR, entryProp, condsProp, i);
                }

                var btnY = titleR.y + (lineH + spacing) * (condsProp.arraySize + 1);
                var btnR = new Rect(rect.x, btnY, rect.width, lineH);
                if (GUI.Button(btnR, "+ 添加 AND 条件"))
                    AddCondition(condsProp);
            }
        }

        private void DrawConditionEntry(Rect rect, SerializedProperty entryProp, SerializedProperty condsProp,
            int index)
        {
            var nameProp = entryProp.FindPropertyRelative("name");
            var condProp = entryProp.FindPropertyRelative("condition");
            var valueProp = entryProp.FindPropertyRelative("value");

            float w = rect.width, h = EditorGUIUtility.singleLineHeight;
            var r1 = new Rect(rect.x, rect.y, w * 0.40f, h);
            var r2 = new Rect(rect.x + w * 0.41f, rect.y, w * 0.17f, h);
            var r3 = new Rect(rect.x + w * 0.59f, rect.y, w * 0.39f - 20, h);
            var delR = new Rect(rect.x + w - 20, rect.y, 20, h);

            EditorGUI.PropertyField(r1, nameProp, GUIContent.none);
            EditorGUI.PropertyField(r2, condProp, GUIContent.none);

            var mode = (CatToolsAnimatorConditionMode)condProp.enumValueFlag;
            if (mode is CatToolsAnimatorConditionMode.If or CatToolsAnimatorConditionMode.IfNot)
            {
                string[] labels = { "False", "True" };
                var sel = valueProp.stringValue == "1" ? 1 : 0;
                sel = EditorGUI.Popup(r3, sel, labels);
                valueProp.stringValue = sel == 1 ? "1" : "0";
            }
            else
            {
                EditorGUI.PropertyField(r3, valueProp, GUIContent.none);
            }

            if (GUI.Button(delR, new GUIContent("-", "删除此 AND 条件")))
            {
                var so = condsProp.serializedObject;
                so.Update();
                condsProp.DeleteArrayElementAtIndex(index);
                so.ApplyModifiedProperties();
            }
        }

        private static void AddCondition(SerializedProperty condsProp)
        {
            var so = condsProp.serializedObject;
            so.Update();

            var insertIndex = condsProp.arraySize;
            condsProp.InsertArrayElementAtIndex(insertIndex);
            so.ApplyModifiedProperties();
            so.Update();

            var newEntry = condsProp.GetArrayElementAtIndex(insertIndex);
            if (insertIndex > 0)
            {
                var prev = condsProp.GetArrayElementAtIndex(insertIndex - 1);
                newEntry.FindPropertyRelative("name").stringValue = prev.FindPropertyRelative("name").stringValue;
                newEntry.FindPropertyRelative("condition").enumValueFlag =
                    prev.FindPropertyRelative("condition").enumValueFlag;
                newEntry.FindPropertyRelative("value").stringValue = prev.FindPropertyRelative("value").stringValue;
            }
            else
            {
                newEntry.FindPropertyRelative("name").stringValue = "New_Param";
                newEntry.FindPropertyRelative("condition").enumValueFlag = 0;
                newEntry.FindPropertyRelative("value").stringValue = "1";
            }

            so.ApplyModifiedProperties();
        }

        private void AddOuterGroup(ReorderableList list)
        {
            var arrayProp = list.serializedProperty;
            var so = arrayProp.serializedObject;

            so.Update();
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
            so.ApplyModifiedProperties();
            so.Update();

            var newIndex = arrayProp.arraySize - 1;
            var newGroup = arrayProp.GetArrayElementAtIndex(newIndex);
            var condsProp = newGroup.FindPropertyRelative("conditions");
            if (condsProp != null && condsProp.isArray)
            {
                condsProp.arraySize = 1;
                var first = condsProp.GetArrayElementAtIndex(0);
                first.FindPropertyRelative("name").stringValue = "New_Param";
                first.FindPropertyRelative("condition").enumValueFlag = 1;
                first.FindPropertyRelative("value").stringValue = "1";
            }

            list.index = newIndex;
            so.ApplyModifiedProperties();
        }
    }
}