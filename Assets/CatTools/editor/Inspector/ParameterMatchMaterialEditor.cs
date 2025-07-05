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
using System.IO;
using System.Text.RegularExpressions;
using CatTools.editor.ui;
using CatTools.Runtime;
using UnityEditor;
using UnityEngine;

namespace CatTools.editor.Inspector
{
    [CustomEditor(typeof(ParameterMatchMaterial))]
    public class ParameterMatchMaterialEditor : Editor
    {
        private ParameterConditionList _conditionList;
        private SerializedProperty _layerTypeProp;
        private SerializedProperty _nameRegexProp;
        private SerializedObject _so;

        private void OnEnable()
        {
            _so = new SerializedObject(target);
            _layerTypeProp = _so.FindProperty("layerType");
            _nameRegexProp = _so.FindProperty("nameRegex");

            _conditionList = new ParameterConditionList(_so, "conditions");
        }

        public override void OnInspectorGUI()
        {
            _so.Update();
            EditorGUILayout.HelpBox("设置条件参数控制材质切换", MessageType.Info);
            EditorGUILayout.PropertyField(_layerTypeProp, new GUIContent("Layer类型"));

            _conditionList.DoLayout();

            // 匹配条件文本框
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("匹配条件：");
            _nameRegexProp.stringValue = EditorGUILayout.TextField(_nameRegexProp.stringValue, GUILayout.MinWidth(50));
            EditorGUILayout.EndHorizontal();

            // 调试按钮
            if (GUILayout.Button("调试")) ParameterMatchMaterialDebugWindow.ShowWindow((ParameterMatchMaterial)target);

            _so.ApplyModifiedProperties();
        }
    }

    public class ParameterMatchMaterialDebugWindow : EditorWindow
    {
        private readonly List<ConditionMaterialPair> _data = new();
        private string _currentRegex;
        private bool _needsRefresh;
        private Vector2 _scrollPos;
        private ParameterMatchMaterial _target;

        private void OnGUI()
        {
            // 顶部：匹配条件（只读） + 刷新按钮，高亮提示
            EditorGUILayout.BeginHorizontal(GUILayout.Height(40));
            EditorGUILayout.LabelField("匹配条件：", GUILayout.Width(60));
            GUI.enabled = false;
            EditorGUILayout.TextField(_currentRegex, GUILayout.ExpandWidth(true));
            GUI.enabled = true;

            var originalColor = GUI.backgroundColor;
            if (_needsRefresh) GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("刷新", GUILayout.Width(60), GUILayout.Height(20)))
            {
                RefreshData();
                _needsRefresh = false;
            }

            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 列表区域，占满宽度
            _scrollPos =
                EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            foreach (var item in _data)
            {
                EditorGUILayout.BeginHorizontal();

                // 左侧：源材质，宽度一致
                GUI.enabled = false;
                EditorGUILayout.ObjectField(item.SourceMaterial, typeof(Material), false,
                    GUILayout.Width(position.width * 0.45f), GUILayout.Height(20));
                GUI.enabled = true;

                GUILayout.Label("替换为：", GUILayout.Width(60));

                // 右侧：目标材质
                GUI.enabled = item.TargetMaterial != null;
                EditorGUILayout.ObjectField(item.TargetMaterial, typeof(Material), false,
                    GUILayout.Width(position.width * 0.45f), GUILayout.Height(20));
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnInspectorUpdate()
        {
            if (_target == null) return;
            if (_target.nameRegex != _currentRegex)
            {
                _currentRegex = _target.nameRegex;
                _needsRefresh = true;
                Repaint();
            }
        }

        public static void ShowWindow(ParameterMatchMaterial target)
        {
            var wnd = GetWindow<ParameterMatchMaterialDebugWindow>(true, "调试 - 材质替换预览");
            wnd._target = target;
            wnd._currentRegex = target.nameRegex;
            wnd.RefreshData();
            wnd._needsRefresh = false;
            wnd.minSize = new Vector2(500, 400);
            wnd.Show();
        }

        private void RefreshData()
        {
            _data.Clear();
            if (_target == null) return;

            var renderers = _target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var uniqueMats = new HashSet<Material>();
            foreach (var rend in renderers)
            foreach (var mat in rend.sharedMaterials)
                if (mat != null)
                    uniqueMats.Add(mat);

            foreach (var src in uniqueMats)
            {
                var pair = new ConditionMaterialPair { SourceMaterial = src };
                var path = AssetDatabase.GetAssetPath(src);
                var dir = Path.GetDirectoryName(path);
                var baseName = Path.GetFileNameWithoutExtension(path);

                // 1) 按照 {name} 分割
                var parts = _currentRegex.Split(new[] { "{name}" }, StringSplitOptions.None);
                // 2) 对文字部分做转义
                var before = Regex.Escape(parts[0]);
                var after = Regex.Escape(parts.Length > 1 ? parts[1] : "");
                // 3) 插入被转义的 baseName
                var pattern = before + Regex.Escape(baseName) + after;

                if (!string.IsNullOrEmpty(dir))
                {
                    var guids = AssetDatabase.FindAssets("t:Material", new[] { dir });
                    foreach (var guid in guids)
                    {
                        var matPath = AssetDatabase.GUIDToAssetPath(guid);
                        var matName = Path.GetFileNameWithoutExtension(matPath);
                        // 忽略大小写匹配
                        if (Regex.IsMatch(matName, pattern))
                        {
                            pair.TargetMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                            break;
                        }
                    }
                }

                _data.Add(pair);
            }
        }

        private class ConditionMaterialPair
        {
            public Material SourceMaterial;
            public Material TargetMaterial;
        }
    }
}