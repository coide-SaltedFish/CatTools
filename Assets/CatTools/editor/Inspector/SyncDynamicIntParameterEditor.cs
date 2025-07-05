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
    [CustomEditor(typeof(SyncDynamicIntParameter))]
    public class SyncDynamicIntParameterEditor : Editor
    {
        private SerializedProperty _bitWidthProp;
        private SerializedProperty _cycleTimeProp;
        private ReorderableList _list;
        private SerializedProperty _parametersProp;
        private SerializedObject _so;
        
        private const float IndexWidth = 30f; // 预留给“###.”的空间

        private void OnEnable()
        {
            _so = new SerializedObject(target);
            _parametersProp = _so.FindProperty("parameters");
            _cycleTimeProp = _so.FindProperty("cycleTime");
            _bitWidthProp = _so.FindProperty("bitWidth");

            _list = new ReorderableList(_so, _parametersProp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "参数列表"),
                drawElementCallback = DrawElement,
                onAddCallback = OnAddElement
            };
        }

        public override void OnInspectorGUI()
        {
            _so.Update();

            EditorGUILayout.HelpBox("定义使用自定义参数槽数量的异步 Int 参数", MessageType.Info);
            
            // 计算参数槽占用
            EditorGUILayout.LabelField($"当前共 {_parametersProp.arraySize} 个 Int 参数，每个参数最大值为：{1 << _bitWidthProp.intValue}，共使用参数槽数量: {GetParameterSlotCount()}");
            EditorGUILayout.PropertyField(_so.FindProperty("layerType"), new GUIContent("Layer类型"));
            // 显示周期时间
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("周期时间（ms）：");
            _cycleTimeProp.intValue = EditorGUILayout.IntField(_cycleTimeProp.intValue, GUILayout.MinWidth(50));
            EditorGUILayout.EndHorizontal();

            // 显示位宽
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("位宽：");
            _bitWidthProp.intValue = EditorGUILayout.Popup(_bitWidthProp.intValue - 2,
                new[] { "2", "3", "4", "5", "6", "7", "8" }, GUILayout.MinWidth(50)) + 2;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            _list.DoLayoutList();

            _so.ApplyModifiedProperties();
        }
        
        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _parametersProp.GetArrayElementAtIndex(index);
            rect.y += (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;

            //序号标签区
            var idxRect = new Rect(rect.x, rect.y, IndexWidth, EditorGUIUtility.singleLineHeight);
            var idxText = $"{index + 1,3}."; 
            var idxStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight
            };
            EditorGUI.LabelField(idxRect, idxText, idxStyle);

            //参数名输入框，占剩余全部宽度
            var nameRect = new Rect(rect.x + IndexWidth + 5f, rect.y,
                rect.width - IndexWidth - 5f, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(nameRect, element, GUIContent.none);
        }

        private void OnAddElement(ReorderableList list)
        {
            var oldSize = _parametersProp.arraySize;
            _parametersProp.arraySize++;
            _so.ApplyModifiedProperties();

            var newIndex = _parametersProp.arraySize - 1;
            var newElem = _parametersProp.GetArrayElementAtIndex(newIndex);

            if (oldSize == 0)
            {
                newElem.stringValue = "New_Param";
            }
            else
            {
                var prevElem = _parametersProp.GetArrayElementAtIndex(oldSize - 1);
                newElem.stringValue = prevElem.stringValue;
            }

            _so.ApplyModifiedProperties();
        }
        
        private int GetParameterSlotCount()
        {
            var n = _parametersProp.arraySize;
            var bitWidth = _bitWidthProp.intValue;

            var overhead = 0;
            if (n > 1)
                overhead = Mathf.CeilToInt(Mathf.Log(n, 2f));

            return overhead + bitWidth;
        }
    }
}