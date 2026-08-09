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
using System.Linq;
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    public class CatSyncDanceDanceEditWindows : EditorWindow
    {
        private SerializedObject _target;
        private SerializedProperty _dances;
        private Vector2 _scrollPos;
        private ReorderableList _list;
        private readonly Dictionary<string, ItemList> _danceLists = new();
        private SerializedProperty _syncParameterProp;
        private SerializedProperty _controllerParameterNameProp;
        
        private CatSyncDanceDanceEditWindows()
        {
            
        }

        private void Init(SerializedObject target)
        {
            _target = target;
            _dances = _target.FindProperty("dances");
            _syncParameterProp = _target.FindProperty("syncDanceConfig").FindPropertyRelative("syncParameterNames");
            _controllerParameterNameProp = _target.FindProperty("controllerParameterName");
            
            _list = new ReorderableList(_target, _dances,
                true, false, true, true)
            {
                drawElementCallback = DrawElement,
                elementHeightCallback = ElementHeightCallback,
                drawElementBackgroundCallback = DrawElementBackground,
                onAddCallback = OnAddElement,
                onRemoveCallback = list =>
                {
                    var index = list.index;
                    _danceLists.Remove(GetSyncDanceUuid(_dances.GetArrayElementAtIndex(index)));
                    // 调用默认删除逻辑
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                }
            };
        }

        private void OnGUI()
        {
            _target.Update();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _list.DoLayoutList();
            EditorGUILayout.EndScrollView();
            _target.ApplyModifiedProperties();
        }
        
        private float ElementHeightCallback(int index)
        {
            var lineH = EditorGUIUtility.singleLineHeight + 2f;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var prop = _dances.GetArrayElementAtIndex(index);
            var itemList = GetOrCreateItemList(index, prop);

            // 与 DrawElement 保持一致：标题、舞蹈名称、动画路径类型、音乐/循环/速度 共 4 行，加 2 个内嵌列表
            return lineH * 4 + spacing * 5 + itemList.AnimClipList.GetHeight() + itemList.MusicClipList.GetHeight();
        }
        
        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var lineH = EditorGUIUtility.singleLineHeight + 2f;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var x = rect.x;
            var y = rect.y;
            
            var prop = _dances.GetArrayElementAtIndex(index);
            var danceName = prop.FindPropertyRelative("danceName");
            var pathType = prop.FindPropertyRelative("pathType");
            var musicClip = prop.FindPropertyRelative("musicClip");
            var loop = prop.FindPropertyRelative("loop");
            var speed = prop.FindPropertyRelative("speed");

            var itemList = GetOrCreateItemList(index, prop);
            
            // 行标题：控制参数 = 当前行数 + 1，用于区分每一行舞蹈
            var controllerName = _controllerParameterNameProp?.stringValue ?? string.Empty;
            var title = string.IsNullOrEmpty(controllerName)
                ? $"{index + 1}"
                : $"{controllerName} = {index + 1}";
            var titleRect = new Rect(x, y, rect.width, lineH);
            EditorGUI.DrawRect(titleRect, new Color(0.36f, 0.58f, 0.95f, 0.25f));
            EditorGUI.LabelField(titleRect, title, EditorStyles.boldLabel);
            y += lineH + spacing;
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), danceName, new GUIContent("舞蹈名称"));
            if (EditorGUI.EndChangeCheck())
            {
                if (DanceNameCheck(index, danceName.stringValue).Not())
                {
                    danceName.stringValue = GetUniqueDanceName(index, danceName.stringValue);
                }
            }
            y += lineH + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), pathType, new GUIContent("动画路径类型"));
            y += lineH + spacing;
            itemList.AnimClipList.DoList(new Rect(x, y, rect.width, lineH));
            y += itemList.AnimClipList.GetHeight() + spacing;
            // 音乐剪辑、循环和速度同一行：音乐剪辑在前占剩余宽度，循环和速度保持紧凑布局
            var loopLabelWidth = EditorStyles.label.CalcSize(new GUIContent("循环")).x;
            var speedLabelWidth = EditorStyles.label.CalcSize(new GUIContent("速度:")).x;
            var toggleWidth = EditorGUIUtility.singleLineHeight;
            const float speedInputWidth = 40f;
            var musicClipLabelWidth = EditorStyles.label.CalcSize(new GUIContent("音乐剪辑")).x;
            // 循环和速度区域的总宽度
            var loopSpeedWidth = loopLabelWidth + 2f + toggleWidth + 5f + speedLabelWidth + 2f + speedInputWidth;
            var musicClipFieldWidth = Mathf.Max(0f, rect.width - musicClipLabelWidth - 2f - loopSpeedWidth);
            var fieldX = x;
            EditorGUI.LabelField(new Rect(fieldX, y, musicClipLabelWidth, lineH), "音乐剪辑");
            fieldX += musicClipLabelWidth + 2f;
            EditorGUI.ObjectField(new Rect(fieldX, y, musicClipFieldWidth, lineH), musicClip, typeof(AudioClip));
            fieldX += musicClipFieldWidth;
            EditorGUI.LabelField(new Rect(fieldX, y, loopLabelWidth, lineH), "循环");
            fieldX += loopLabelWidth + 2f;
            EditorGUI.BeginChangeCheck();
            var loopValue = EditorGUI.Toggle(new Rect(fieldX, y, toggleWidth, lineH), loop.boolValue);
            if (EditorGUI.EndChangeCheck()) loop.boolValue = loopValue;
            fieldX += toggleWidth + 5f;
            EditorGUI.LabelField(new Rect(fieldX, y, speedLabelWidth, lineH), "速度:");
            fieldX += speedLabelWidth + 2f;
            EditorGUI.PropertyField(new Rect(fieldX, y, speedInputWidth, lineH), speed, GUIContent.none);
            y += lineH + spacing;
            itemList.MusicClipList.DoList(new Rect(x, y, rect.width, lineH));
        }

        private string[] GetSyncParameterNames()
        {
            var names = new List<string>();
            for (var i = 0; i < _syncParameterProp.arraySize; i++)
            {
                names.Add(_syncParameterProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
            }

            return names.ToArray();
        }

        private string GetSyncDanceUuid(SerializedProperty dance)
        {
            return dance.FindPropertyRelative("danceName").stringValue.GetMD5();
        }
        
        private bool DanceNameCheck(int index, string dName)
        {
            for (var i = 0; i < _dances.arraySize; i++)
            {
                if (i == index) continue;
                var prop = _dances.GetArrayElementAtIndex(i);
                var danceName = prop.FindPropertyRelative("danceName");
                if (danceName.stringValue == dName)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 生成不与其它舞蹈重复的舞蹈名称
        /// </summary>
        private string GetUniqueDanceName(int index, string name)
        {
            var ci = 0;
            var cName = name;
            // 遍历
            while (DanceNameCheck(index, cName).Not())
            {
                cName = $"{name} {ci}";
                ci++;
            }

            return cName;
        }

        /// <summary>
        /// 绘制元素行背景，使用斑马纹让每一行舞蹈更清晰可辨
        /// </summary>
        private void DrawElementBackground(Rect rect, int index, bool selected, bool focused)
        {
            // 保留默认的选择高亮绘制
            ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, selected, focused, true);
            if (selected) return;
            EditorGUI.DrawRect(rect, index % 2 == 0
                ? new Color(0.5f, 0.65f, 0.9f, 0.05f)
                : new Color(0.5f, 0.65f, 0.9f, 0.12f));
        }

        /// <summary>
        /// 添加舞蹈时对名称应用查重，避免新增条目重名
        /// </summary>
        private void OnAddElement(ReorderableList list)
        {
            // 执行默认添加逻辑
            ReorderableList.defaultBehaviours.DoAddButton(list);
            var index = list.index;
            if (index < 0 || index >= _dances.arraySize) return;
            var danceName = _dances.GetArrayElementAtIndex(index).FindPropertyRelative("danceName");
            var baseName = string.IsNullOrEmpty(danceName.stringValue) ? "Dance" : danceName.stringValue;
            var uniqueName = GetUniqueDanceName(index, baseName);
            if (uniqueName != danceName.stringValue) danceName.stringValue = uniqueName;
        }
        
        /// <summary>
        /// 获取舞蹈对应的列表项缓存，元素增删/撤销/拖拽导致下标失效时自动重建
        /// </summary>
        private ItemList GetOrCreateItemList(int index, SerializedProperty prop)
        {
            var uuid = GetSyncDanceUuid(prop);
            if (_danceLists.TryGetValue(uuid, out var itemList))
            {
                // 缓存绑定的下标已与当前下标不一致，说明数组结构发生了变化，需要重建
                if (itemList.BoundIndex == index) return itemList;
                _danceLists.Remove(uuid);
            }
            itemList = new ItemList(_dances, index, GetSyncParameterNames);
            _danceLists[uuid] = itemList;
            return itemList;
        }
        
        private class ItemList
        {
            public readonly ReorderableList AnimClipList;
            public readonly ReorderableList MusicClipList;
            /// <summary>
            /// 该列表项绑定的舞蹈元素下标，用于在数组变化时检测缓存是否失效
            /// </summary>
            public readonly int BoundIndex;
            
            public ItemList(SerializedProperty dances,int index, Func<string[]> getSyncParameterNames)
            {
                BoundIndex = index;
                var lineH = EditorGUIUtility.singleLineHeight + 2f;
                var spacing = EditorGUIUtility.standardVerticalSpacing;
                
                var prop = dances.GetArrayElementAtIndex(index);
                var clips = prop.FindPropertyRelative("clip");
                var danceParameters = prop.FindPropertyRelative("danceParameters");
                
                MusicClipList = new ReorderableList(danceParameters.serializedObject, danceParameters, true, true, true, true)
                {
                    drawHeaderCallback = r => EditorGUI.LabelField(r, "同步参数"),
                    elementHeightCallback = _ => lineH,
                    drawElementCallback = (r, i, a, f) =>
                    {
                        // 防御：撤销等操作可能导致属性暂时失效
                        if (i < 0 || i >= danceParameters.arraySize) return;
                        var pProp = danceParameters.GetArrayElementAtIndex(i);
                        var parameterName = pProp.FindPropertyRelative("parameterName");
                        var pValue = pProp.FindPropertyRelative("value");
                        // 下拉框
                        var options = getSyncParameterNames();
                        var nowIndex = Array.IndexOf(options, parameterName.stringValue);
                        if (nowIndex == -1)
                        {
                            parameterName.stringValue = options.TryGet(0);
                            nowIndex = 0;
                        }
                        // 一行布局：固定宽度标签 + 5 + 30%宽度下拉框 + 5 + 固定宽度值标签 + 2 + 剩余宽度输入框
                        var labelWidth = EditorStyles.label.CalcSize(new GUIContent("同步参数")).x;
                        var valueLabelWidth = EditorStyles.label.CalcSize(new GUIContent("值:")).x;
                        var popupWidth = r.width * 0.3f;
                        var valueWidth = r.width - labelWidth - 5f - popupWidth - 5f - valueLabelWidth - 2f;
                        var x = r.x;
                        EditorGUI.LabelField(new Rect(x, r.y, labelWidth, lineH), "同步参数");
                        x += labelWidth + 5f;
                        var newIndex = EditorGUI.Popup(new Rect(x, r.y, popupWidth, lineH), nowIndex, options);
                        if (newIndex != nowIndex)
                        {
                            parameterName.stringValue = options[newIndex];
                        }
                        x += popupWidth + 5f;
                        EditorGUI.LabelField(new Rect(x, r.y, valueLabelWidth, lineH), "值:");
                        x += valueLabelWidth + 2f;
                        EditorGUI.PropertyField(new Rect(x, r.y, valueWidth, lineH), pValue, GUIContent.none);
                    }
                };
                AnimClipList = new ReorderableList(clips.serializedObject, clips, true, true, true, true)
                {
                    drawHeaderCallback = r => EditorGUI.LabelField(r, "动画剪辑"),
                    elementHeightCallback = _ => lineH,
                    drawElementCallback = (r, i, a, f) =>
                    {
                        // 防御：撤销等操作可能导致属性暂时失效
                        if (i < 0 || i >= clips.arraySize) return;
                        var clip = clips.GetArrayElementAtIndex(i);
                        EditorGUI.PropertyField(new Rect(r.x, r.y, r.width, lineH), clip, GUIContent.none);
                    }
                };
            }
        }
        
        public static void ShowWindow(SerializedObject target)
        {
            var wnd = GetWindow<CatSyncDanceDanceEditWindows>(true, "同步舞蹈");
            wnd.Init(target);
            wnd.minSize = new Vector2(500, 400);
            wnd.Show();
        }
    }
}