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
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector.window
{
    public class ConditionalMatchMaterialsSetterDebugWindows : EditorWindow
    {
        private ConditionalMatchMaterialsSetter _target;
        private Vector2 _scrollPos;
        private Dictionary<Material, List<Material>> _data;
        private readonly Dictionary<Material, bool> _foldoutStates = new();
        private readonly Dictionary<Material, Transform[]> _transformStates = new();
        
        private ConditionalMatchMaterialsSetterDebugWindows()
        {
            
        }
        
        private void OnGUI()
        {
            // 顶部显示 匹配规则 刷新按钮
            EditorGUILayout.BeginHorizontal(GUILayout.Height(40));
            EditorGUILayout.LabelField("匹配条件：", GUILayout.Width(60));
            GUI.enabled = false;
            EditorGUILayout.TextField(_target.matchExpression, GUILayout.ExpandWidth(true));
            GUI.enabled = true;
            if (GUILayout.Button("刷新",GUILayout.Width(60), GUILayout.Height(20)))
            {
                RefreshData();
            }
            EditorGUILayout.EndHorizontal();
            
            // 显示匹配材质列表
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            foreach (var keyValuePair in _data)
            {
                ListItem(keyValuePair.Key, keyValuePair.Value.ToArray());
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void ListItem(Material source, Material[] targets)
        {
            var hasMultipleTargets = targets.Length > 1;

            // 行背景标红
            var oldColor = GUI.color;
            if (hasMultipleTargets)
            {
                GUI.color = new Color(1f, 0.7f, 0.7f);
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight + 2f));

            // 左侧：源材质
            GUI.enabled = false;
            EditorGUILayout.ObjectField(
                source,
                typeof(Material),
                false,
                GUILayout.Width(position.width * 0.45f),
                GUILayout.Height(20));
            GUI.enabled = true;
            GUILayout.Label("替换为：", GUILayout.Width(60));

            // 右侧：目标材质
            var target = targets.Length > 0 ? targets[0] : null;
            GUI.enabled = target != null;
            EditorGUILayout.ObjectField(
                target,
                typeof(Material),
                false,
                GUILayout.Width(position.width * 0.45f),
                GUILayout.Height(20));
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            GUI.color = oldColor;
            // 多个匹配提示
            if (hasMultipleTargets)
            {
                EditorGUILayout.HelpBox(
                    $"匹配到多个材质（{targets.Length} 个），该材质替换将被忽略。",
                    MessageType.Warning);
            }
            
            // Foldout
            _foldoutStates.TryAdd(source, false);

            _foldoutStates[source] = EditorGUILayout.Foldout(
                _foldoutStates[source],
                $"引用对象列表",
                true);

            if ( _foldoutStates[source])
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 这里替换成你的 Transform 列表
                if (!_transformStates.ContainsKey(source))
                {
                    _transformStates[source] = GetTransforms(source);
                }
                
                foreach (var transform in _transformStates[source])
                {
                    EditorGUILayout.ObjectField(
                        transform,
                        typeof(Transform),
                        true,
                        GUILayout.ExpandWidth(true));
                }

                EditorGUILayout.EndVertical();
            }

            
            EditorGUILayout.Space(4);
        }
        
        private Transform[] GetTransforms(Material source)
        {
            if (_target == null || source == null)
            {
                return Array.Empty<Transform>();
            }

            return (from skinnedMeshRenderer in _target.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                where skinnedMeshRenderer.sharedMaterials.Any(sharedMaterial => sharedMaterial == source)
                select skinnedMeshRenderer.transform)
                .ToArray();
        }
        
        private void RefreshData()
        {
            // 执行再次扫描，更新材质列表数据
            _data = new Dictionary<Material, List<Material>>();
            _foldoutStates.Clear();
            foreach (var material in _target.gameObject.FindChildMaterials())
            {
                var targets = _target.FindTargetsMaterial(material);
                _data.Add(material, new List<Material>(targets));
            }
        }
        
        public static void ShowWindow(ConditionalMatchMaterialsSetter target)
        {
            var wnd = GetWindow<ConditionalMatchMaterialsSetterDebugWindows>(true, "调试 - 材质替换预览");
            wnd._target = target;
            if (wnd._target == null)
            {
                EditorUtility.DisplayDialog(
                    "提示",
                    "错误的组件对象：null",
                    "确定"
                );
                return;
            }
            wnd.RefreshData();
            wnd.minSize = new Vector2(500, 400);
            wnd.Show();
        }
    }
}