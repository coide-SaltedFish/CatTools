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
using io.github.sereinfish.cat.tools.Conditions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(DynamicIntParameter))]
    public class DynamicIntParameterInspector : AnimLayerEditor<DynamicIntParameter>
    {
        private SerializedProperty _parametersProp;
        private ReorderableList _list;

        private static Vector2? _nameLabelSize;
        private static Vector2? _bitWidthLabelSize;
        private static Vector2? _isSaveLabelSize;
        private static Vector2? _defaultValueLabelSize;
        
        protected override void Init()
        {
            base.Init();
            
            _parametersProp = PropGet(nameof(DynamicIntParameter.parameters));
            
            _list = new ReorderableList(serializedObject, _parametersProp, true, true, true, true)
            {
                drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "参数列表"); },
                drawElementCallback = DrawElement,
                elementHeightCallback = _ => (EditorGUIUtility.singleLineHeight + 2f) * 2 + EditorGUIUtility.standardVerticalSpacing,
                onAddCallback = AddGroup
            };
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            _list.DoLayoutList();
        }
        
        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            _nameLabelSize ??= GUI.skin.label.CalcSize(new GUIContent("名称："));
            _bitWidthLabelSize ??= GUI.skin.label.CalcSize(new GUIContent("位宽："));
            _isSaveLabelSize ??= GUI.skin.label.CalcSize(new GUIContent("是否保存："));
            _defaultValueLabelSize ??= GUI.skin.label.CalcSize(new GUIContent("默认值："));
            
            var lineH = EditorGUIUtility.singleLineHeight + 2f;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            
            var element = _parametersProp.GetArrayElementAtIndex(index);
            var nameProp = element.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.name));
            var widthProp = element.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.width));
            var saveProp = element.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.save));
            var defaultValueProp = element.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.defaultValue));

            var x = rect.x;
            var y = rect.y;
            
            // 名称，宽度（MAX：255）
            // 是否保存，默认值
            EditorGUI.LabelField(new Rect(x, y, _nameLabelSize?.x ?? 0, lineH), "名称：");
            x += _nameLabelSize?.x ?? 0;
            EditorGUI.PropertyField(new Rect(x, y, rect.width * 0.6f, lineH), nameProp, GUIContent.none);
            x += rect.width * 0.6f + 6f;
            // 宽度下拉框 1,2,3,4,5,6,7,8
            EditorGUI.LabelField(new Rect(x, y, _bitWidthLabelSize?.x ?? 0, lineH),"位宽：");
            x += _bitWidthLabelSize?.x ?? 0;
            string[] options = {"1", "2", "3", "4", "5", "6", "7", "8" };
            var currentIndex = Mathf.Clamp(widthProp.intValue - 1, 0, options.Length - 1);
            var newIndex = EditorGUI.Popup(new Rect(x, y, 30f, lineH), currentIndex, options);
            x += 30f + spacing;
            widthProp.intValue = newIndex + 1;
            var maxValue = (1 << widthProp.intValue) - 1;
            var maxLabel = $"MAX：{maxValue}";
            var msw = GUI.skin.label.CalcSize(new GUIContent(maxLabel));
            EditorGUI.LabelField(new Rect(x, y, msw.x, lineH), maxLabel);
            x = rect.x;
            y += lineH + spacing;
            EditorGUI.LabelField(new Rect(x, y, _isSaveLabelSize?.x ?? 0, lineH), "是否保存：");
            x += _isSaveLabelSize?.x ?? 0;
            EditorGUI.PropertyField(new Rect(x, y, 25f, lineH), saveProp, GUIContent.none);
            x += 25f + spacing;
            EditorGUI.LabelField(new Rect(x, y, _defaultValueLabelSize?.x ?? 0, lineH), "默认值：");
            x += _defaultValueLabelSize?.x ?? 0;
            EditorGUI.PropertyField(new Rect(x, y, 50f, lineH), defaultValueProp, GUIContent.none);
        }
        
        private void AddGroup(ReorderableList list)
        {
            var prop = list.serializedProperty;
            // 获取旧组
            SerializedProperty oldGroup = null;
            if (prop.arraySize > 0)
            {
                oldGroup = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            }
            
            prop.InsertArrayElementAtIndex(prop.arraySize);
            // 初始化新组
            var newGroup = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            newGroup.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.name))
                .stringValue = oldGroup?.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.name)).stringValue ?? "New Parameter";
            newGroup.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.width))
                .intValue = oldGroup?.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.width)).intValue ?? 2;
            newGroup.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.save))
                .boolValue = oldGroup?.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.save)).boolValue ?? true;
            newGroup.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.defaultValue))
                .intValue = oldGroup?.FindPropertyRelative(nameof(DynamicIntParameter.CatDynamicInt.defaultValue)).intValue ?? 0;
            list.index = prop.arraySize - 1;
        }
    }
}