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
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CatTools.editor.ui
{
    public class MaterialChangeList
    {
        private readonly Func<Renderer> _getFirstRenderer;
        private readonly ReorderableList _list;
        private readonly SerializedProperty _listProp;
        private readonly SerializedObject _so;

        public MaterialChangeList(
            SerializedObject serializedObject,
            string propertyName,
            Func<Renderer> getFirstRenderer
        )
        {
            _so = serializedObject;
            _listProp = _so.FindProperty(propertyName);
            _getFirstRenderer = getFirstRenderer;

            _list = new ReorderableList(_so, _listProp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "材质替换列表"),
                elementHeightCallback = _ => EditorGUIUtility.singleLineHeight * 2 + 10,
                onAddCallback = _ =>
                {
                    _listProp.arraySize++;
                    _so.ApplyModifiedProperties();
                }
            };

            _list.drawElementCallback = DrawElement;
        }

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            var element = _list.serializedProperty.GetArrayElementAtIndex(index);
            var slotProp = element.FindPropertyRelative("index");
            var targetMatProp = element.FindPropertyRelative("targetMaterial");

            // 统一使用第一个 Renderer，不在每行展示选择框
            var rend = _getFirstRenderer();

            var h = EditorGUIUtility.singleLineHeight;
            var y = rect.y + 2;
            var w = rect.width;

            // —— 第1行：材质槽下拉（30%宽） + 原有材质显示（70%宽，仅灰显） —— 
            var slotRect = new Rect(rect.x, y, w * 0.3f, h);
            var origMatRect = new Rect(rect.x + w * 0.3f + 5, y, w * 0.7f - 5, h);

            // 材质槽下拉
            var matNames = rend != null ? GetMaterialSlotNames(rend) : new[] { "(无)" };
            slotProp.intValue = EditorGUI.Popup(
                slotRect,
                rend ? slotProp.intValue : 0,
                matNames
            );

            // 只读显示当前材质，灰显但可点击选中
            Material orig = null;
            if (rend != null)
            {
                var mats = rend.sharedMaterials;
                var slot = Mathf.Clamp(slotProp.intValue, 0, mats.Length - 1);
                orig = mats.Length > 0 ? mats[slot] : null;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ObjectField(origMatRect, orig, typeof(Material), false);
            EditorGUI.EndDisabledGroup();

            // —— 第2行："将材质设置为：" + 目标材质选择框（占满整行） —— 
            y += h + 4;
            var labelRect = new Rect(rect.x, y, 95, h);
            var matFieldRect = new Rect(rect.x + 100, y, w - 100, h);

            EditorGUI.LabelField(labelRect, "将材质设置为：");
            EditorGUI.PropertyField(matFieldRect, targetMatProp, GUIContent.none);
        }

        private string[] GetMaterialSlotNames(Renderer renderer)
        {
            var mats = renderer.sharedMaterials;
            var names = new string[mats.Length];
            for (var i = 0; i < mats.Length; i++)
                names[i] = $"Slot_{i}";
            return names;
        }

        public void DoLayout()
        {
            _so.Update();
            _list.DoLayoutList();
            _so.ApplyModifiedProperties();
        }
    }
}