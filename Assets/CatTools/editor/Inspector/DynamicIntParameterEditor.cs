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

using CatTools.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CatTools.editor.Inspector
{
    [CustomEditor(typeof(DynamicIntParameter))]
    public class DynamicIntParameterEditor : Editor
    {
        private SerializedProperty _listProp;
        private ReorderableList _reorderableList;
        private SerializedObject _so;

        private void OnEnable()
        {
            _so = new SerializedObject(target);
            _listProp = _so.FindProperty("parameters");
            _reorderableList = new ReorderableList(_so, _listProp, true, true, true, true)
            {
                drawHeaderCallback = DrawHeader,
                elementHeightCallback = GetElementHeight,
                drawElementCallback = DrawElement,
                onAddCallback = OnAddElement
            };
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "参数列表");
        }

        private float GetElementHeight(int index)
        {
            var singleLine = EditorGUIUtility.singleLineHeight;
            var verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            return singleLine * 1 + verticalSpacing * 1;
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _listProp.GetArrayElementAtIndex(index);
            var pName = element.FindPropertyRelative("name");
            var bitWidth = element.FindPropertyRelative("width");

            // 计算垂直居中偏移
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var yOffset = rect.y + (rect.height - lineHeight) * 0.5f;

            // 左侧 Name 字段，占 70% 宽度
            var nameRect = new Rect(rect.x, yOffset, rect.width * 0.7f, lineHeight);
            EditorGUI.PropertyField(nameRect, pName, GUIContent.none);

            // 右侧起始 X
            var rightX = rect.x + nameRect.width + 5f;
            var rightWidth = rect.width - nameRect.width - 5f;

            // "位宽：" 标签
            const float labelWidth = 40f;
            var labelRect = new Rect(rightX, yOffset, labelWidth, lineHeight);
            EditorGUI.LabelField(labelRect, "位宽：");

            // 下拉框，只显示一个数字，宽度缩短
            const float popupWidth = 30f;
            var popupX = rightX + labelWidth + 2f;
            var popupRect = new Rect(popupX, yOffset, popupWidth, lineHeight);
            string[] options = { "2", "3", "4", "5", "6", "7" };
            var currentIndex = Mathf.Clamp(bitWidth.intValue - 2, 0, options.Length - 1);
            var newIndex = EditorGUI.Popup(popupRect, currentIndex, options);
            bitWidth.intValue = newIndex + 2;

            // 计算 MAX 值并显示在最右侧，留一点间隔
            int maxValue = 1 << bitWidth.intValue; // 2 的 width 次方
            string maxLabel = $"MAX：{maxValue}";
            Vector2 size = GUI.skin.label.CalcSize(new GUIContent(maxLabel));
            var maxX = popupX + popupWidth + 5f;
            var maxRect = new Rect(maxX, yOffset, size.x, lineHeight);
            EditorGUI.LabelField(maxRect, maxLabel);
        }

        private void OnAddElement(ReorderableList list)
        {
            // 增加数组长度
            _listProp.arraySize++;
            _so.ApplyModifiedProperties();

            var newIndex = _listProp.arraySize - 1;
            var newElem = _listProp.GetArrayElementAtIndex(newIndex);

            // 如果之前已有元素，则参考上一个元素的默认值
            if (newIndex > 0)
            {
                var prevElem = _listProp.GetArrayElementAtIndex(newIndex - 1);
                var preName = prevElem.FindPropertyRelative("name").stringValue;
                var preBitWidth = prevElem.FindPropertyRelative("width").intValue;
                newElem.FindPropertyRelative("name").stringValue = preName;
                newElem.FindPropertyRelative("width").intValue = preBitWidth;
            }
            else
            {
                // 默认初始化为0
                newElem.FindPropertyRelative("name").stringValue = "New_param";
                newElem.FindPropertyRelative("width").intValue = 2;
            }

            _so.ApplyModifiedProperties();
        }

        private int GetParameterSlotCount()
        {
            var count = 0;
            // 遍历数组中的每个元素
            for (var i = 0; i < _listProp.arraySize; i++)
            {
                // 获取第 i 项
                var element = _listProp.GetArrayElementAtIndex(i);

                // 拿到相对属性
                var bitWidthProp = element.FindPropertyRelative("width");

                // 读取它们的值
                count += bitWidthProp.intValue;
            }

            return count;
        }

        public override void OnInspectorGUI()
        {
            _so.Update();

            EditorGUILayout.HelpBox("定义使用自定义参数槽数量的Int参数", MessageType.Info);
            EditorGUILayout.LabelField($"当前共 {_listProp.arraySize} 个 Int 参数，共使用参数槽数量: {GetParameterSlotCount()}");
            EditorGUILayout.PropertyField(_so.FindProperty("layerType"), new GUIContent("Layer类型"));
            _reorderableList.DoLayoutList();
            _so.ApplyModifiedProperties();
        }
    }
}