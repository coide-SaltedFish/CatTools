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
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector.ui
{
    /// <summary>
    ///     一个参数条件列表
    ///     支持与条件和或条件
    ///     接受的 SerializedProperty 必须是 List&lt;ParameterConditionEntry&gt;
    /// </summary>
    public class ParameterConditionList<T> where T : ConditionalBehaviour
    {
        private readonly ReorderableList _outerList;
        private readonly SerializedProperty _rootListProp;
        private readonly SerializedObject _so;
        
        private Dictionary<string, SerializedProperty> Props { get; } = new();
        
        private SerializedProperty PropGet(string pName)
        {
            return PropGet(_so, pName);
        }
        
        private SerializedProperty PropGet(SerializedObject so, string pName)
        {
            if (Props.TryGetValue(pName, out var get)) return get;
            
            return Props[pName] = so.FindProperty(pName);
        }

        public ParameterConditionList(SerializedObject so)
        {
            _so = so;
            
            _rootListProp = PropGet(nameof(ConditionalBehaviour.conditions))
                                .FindPropertyRelative(nameof(ParameterOrConditions.conditions))
                            ?? throw new ArgumentException($"Property '{nameof(ConditionalBehaviour.conditions)}' not found.");

            _outerList = new ReorderableList(so, _rootListProp, true, true, true, true)
            {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawOuterElement,
                elementHeightCallback = GetOuterElementHeight,
                // 增加新的 OR 组
                onAddCallback = AddOuterGroup,
                // 删除整个 OR 组
                onRemoveCallback = list =>
                {
                    var prop = list.serializedProperty;
                    prop.DeleteArrayElementAtIndex(list.index);
                }
            };
        }

        public void DoLayout()
        {
            _outerList.DoLayoutList();
        }

        private static void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "参数条件列表（ OR ）");
        }

        private float GetOuterElementHeight(int index)
        {
            var lineH = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            // 标题高度 + 上下间距
            var totalH = lineH + spacing * 2;
            var groupProp = _rootListProp.GetArrayElementAtIndex(index);
            var condsProp = groupProp.FindPropertyRelative(nameof(ParameterConditions.conditions));
            if (condsProp != null && condsProp.isArray)
            {
                var count = condsProp.arraySize;
                // 每个 AND 条件行高
                totalH += count * (lineH + spacing);
                // 添加按钮行
                totalH += lineH + spacing;
                // 底部间距
                totalH += spacing * 2;

                var minH = (lineH + spacing) * 2 + spacing * 4;
                return Mathf.Max(totalH, minH);
            }

            return totalH + lineH;
        }

        private void DrawOuterElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var lineH = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var groupProp = _rootListProp.GetArrayElementAtIndex(index);
            var condsProp = groupProp.FindPropertyRelative(nameof(ParameterConditions.conditions));

            // OR 组标题
            var titleR = new Rect(rect.x, rect.y + spacing, rect.width, lineH);
            EditorGUI.LabelField(titleR, $"与条件组 {index + 1}");

            if (condsProp != null && condsProp.isArray)
            {
                // 画每个 AND 条件
                for (var i = 0; i < condsProp.arraySize; i++)
                {
                    var entryProp = condsProp.GetArrayElementAtIndex(i);
                    var entryR = new Rect(
                        rect.x,
                        titleR.y + (lineH + spacing) * (i + 1),
                        rect.width,
                        lineH
                    );
                    DrawConditionEntry(entryR, entryProp, condsProp, i);
                }

                // 添加 AND 条件 按钮
                var btnY = titleR.y + (lineH + spacing) * (condsProp.arraySize + 1);
                var btnR = new Rect(rect.x, btnY, rect.width, lineH);
                if (GUI.Button(btnR, "+ 添加 AND 条件"))
                {
                    condsProp.InsertArrayElementAtIndex(condsProp.arraySize);
                    // 初始化新条件
                    var newEntry = condsProp.GetArrayElementAtIndex(condsProp.arraySize - 1);
                    newEntry.FindPropertyRelative(nameof(ParameterCondition.name)).stringValue = "New_Param";
                    newEntry.FindPropertyRelative(nameof(ParameterCondition.mode)).enumValueFlag = 0;
                    newEntry.FindPropertyRelative(nameof(ParameterCondition.value)).floatValue = 1f;
                }
            }
        }

        private void DrawConditionEntry(Rect rect, SerializedProperty entryProp, SerializedProperty condsProp,
            int index)
        {
            var nameProp = entryProp.FindPropertyRelative(nameof(ParameterCondition.name));
            var condProp = entryProp.FindPropertyRelative(nameof(ParameterCondition.mode));
            var valueProp = entryProp.FindPropertyRelative(nameof(ParameterCondition.value));

            var w = rect.width;
            var lineH = EditorGUIUtility.singleLineHeight;

            var r1 = new Rect(rect.x, rect.y, w * 0.40f, lineH);
            var r2 = new Rect(rect.x + w * 0.41f, rect.y, w * 0.17f, lineH);
            var r3 = new Rect(rect.x + w * 0.59f, rect.y, w * 0.39f - 20, lineH);
            var delR = new Rect(rect.x + w - 20, rect.y, 20, lineH);

            EditorGUI.PropertyField(r1, nameProp, GUIContent.none);
            EditorGUI.PropertyField(r2, condProp, GUIContent.none);

            var mode = (CatAnimatorConditionRuntimeMode)condProp.enumValueFlag;
            if (mode is CatAnimatorConditionRuntimeMode.If or CatAnimatorConditionRuntimeMode.IfNot)
            {
                string[] labels = { "False", "True" };
                var sel = Mathf.Approximately(valueProp.floatValue, 1f) ? 1 : 0;
                sel = EditorGUI.Popup(r3, sel, labels);
                valueProp.floatValue = sel == 1 ? 1f : 0f;
            }
            else
            {
                EditorGUI.PropertyField(r3, valueProp, GUIContent.none);
            }

            // 删除当前 AND 条件
            if (GUI.Button(delR, new GUIContent("-", "删除此 AND 条件"))) condsProp.DeleteArrayElementAtIndex(index);
        }

        private void AddOuterGroup(ReorderableList list)
        {
            var prop = list.serializedProperty;
            prop.InsertArrayElementAtIndex(prop.arraySize);
            // 初始化新组
            var newGroup = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            var conds = newGroup.FindPropertyRelative(nameof(ParameterConditions.conditions));
            conds.arraySize = 1;
            var e0 = conds.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative(nameof(ParameterCondition.name)).stringValue = "New_Param";
            e0.FindPropertyRelative(nameof(ParameterCondition.mode)).enumValueFlag = 0;
            e0.FindPropertyRelative(nameof(ParameterCondition.value)).floatValue = 1f;
            list.index = prop.arraySize - 1;
        }
    }
}