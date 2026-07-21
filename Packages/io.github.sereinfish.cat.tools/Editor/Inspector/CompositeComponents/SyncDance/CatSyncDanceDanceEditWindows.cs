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
        
        private CatSyncDanceDanceEditWindows()
        {
            
        }

        private void Init(SerializedObject target)
        {
            _target = target;
            _dances = _target.FindProperty("dances");
            _syncParameterProp = _target.FindProperty("syncDanceConfig").FindPropertyRelative("syncParameterNames");
            
            _list = new ReorderableList(_target, _dances,
                true, false, true, true)
            {
                drawElementCallback = DrawElement,
                elementHeightCallback = ElementHeightCallback,
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
            if (_danceLists.ContainsKey(GetSyncDanceUuid(prop)).Not())
            {
                _danceLists[GetSyncDanceUuid(prop)] = new ItemList(_dances, index, GetSyncParameterNames);
            }
            var itemList = _danceLists[GetSyncDanceUuid(prop)];

            return (lineH + spacing) * 6 + itemList.AnimClipList.GetHeight() + itemList.MusicClipList.GetHeight();
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

            if (_danceLists.ContainsKey(GetSyncDanceUuid(prop)).Not())
            {
                _danceLists[GetSyncDanceUuid(prop)] = new ItemList(_dances, index, GetSyncParameterNames);
            }

            var itemList = _danceLists[GetSyncDanceUuid(prop)];
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), danceName, new GUIContent("舞蹈名称"));
            if (EditorGUI.EndChangeCheck())
            {
                var ci = 0;
                var cName = danceName.stringValue;
                // 遍历
                while (DanceNameCheck(cName).Not())
                {
                    cName = $"{cName} {ci}";
                    ci++;
                }

                if (ci > 0) danceName.stringValue = cName;
            }
            y += lineH + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), pathType, new GUIContent("动画路径类型"));
            y += lineH + spacing;
            itemList.AnimClipList.DoList(new Rect(x, y, rect.width, lineH));
            y += itemList.AnimClipList.GetHeight() + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), musicClip, new GUIContent("音乐剪辑"));
            y += lineH + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), loop, new GUIContent("循环"));
            y += lineH + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), speed, new GUIContent("速度"));
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
        
        private bool DanceNameCheck(string dName)
        {
            for (var i = 0; i < _dances.arraySize; i++)
            {
                var prop = _dances.GetArrayElementAtIndex(i);
                var danceName = prop.FindPropertyRelative("danceName");
                if (danceName.stringValue == dName)
                {
                    return false;
                }
            }
            return true;
        }
        
        private class ItemList
        {
            public readonly ReorderableList AnimClipList;
            public readonly ReorderableList MusicClipList;
            
            public ItemList(SerializedProperty dances,int index, Func<string[]> getSyncParameterNames)
            {
                var lineH = EditorGUIUtility.singleLineHeight + 2f;
                var spacing = EditorGUIUtility.standardVerticalSpacing;
                
                var prop = dances.GetArrayElementAtIndex(index);
                var clips = prop.FindPropertyRelative("clip");
                var danceParameters = prop.FindPropertyRelative("danceParameters");
                
                MusicClipList = new ReorderableList(danceParameters.serializedObject, danceParameters, true, true, true, true)
                {
                    drawHeaderCallback = r => EditorGUI.LabelField(r, "同步参数"),
                    elementHeightCallback = _ => lineH * 2 + spacing,
                    drawElementCallback = (r, i, a, f) =>
                    {
                        var pProp = danceParameters.GetArrayElementAtIndex(i);
                        var parameterName = pProp.FindPropertyRelative("parameterName");
                        var pValue = pProp.FindPropertyRelative("value");
                        // EditorGUI.PropertyField(new Rect(r.x, r.y, r.width, lineH), parameterName, GUIContent.none);
                        // 下拉框
                        var options = getSyncParameterNames();
                        var nowIndex = Array.IndexOf(options, parameterName.stringValue);
                        if (nowIndex == -1)
                        {
                            parameterName.stringValue = options.TryGet(0);
                            nowIndex = 0;
                        }
                        var newIndex = EditorGUI.Popup(new Rect(r.x, r.y, r.width, lineH), "同步参数", nowIndex, options);
                        if (newIndex != nowIndex)
                        {
                            parameterName.stringValue = options[newIndex];
                        }
                        EditorGUI.PropertyField(new Rect(r.x, r.y + lineH + spacing, r.width, lineH), pValue, GUIContent.none);
                    }
                };
                AnimClipList = new ReorderableList(clips.serializedObject, clips, true, true, true, true)
                {
                    drawHeaderCallback = r => EditorGUI.LabelField(r, "动画剪辑"),
                    elementHeightCallback = _ => lineH,
                    drawElementCallback = (r, i, a, f) =>
                    {
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