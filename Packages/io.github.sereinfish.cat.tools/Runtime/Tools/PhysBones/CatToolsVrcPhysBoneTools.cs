#if UNITY_EDITOR
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
using io.github.sereinfish.cat.tools.physBones.utils;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace io.github.sereinfish.cat.tools.physBones
{
    public class CatToolsVrcPhysBoneTools : EditorWindow
    {
        private readonly List<Vector2> _pathStatisticsScrolls = new();
        private Vector2 _scrollStatisticsPos;
        private Vector2 _scrollOptimizePos;
        private GameObject _gameObject;

        private VRCPhysBone[] _vrcPhysBones = Array.Empty<VRCPhysBone>();
        private Dictionary<VRCPhysBone, List<VRCPhysBone>> _optimizeGroup;
        private readonly string[] _tabs = { "统计信息", "优化工具" };
        private int _tabIndex = 0;
        
        private void OnGUI()
        {
            _gameObject = (GameObject)EditorGUILayout.ObjectField("目标对象", _gameObject, typeof(GameObject), true);
            using (new EditorGUI.DisabledScope(_gameObject == null))
            {
                if (GUILayout.Button("刷新")) Refresh();
            }

            if (_gameObject == null)
            {
                GUILayout.Label("请选择目标对象");
                return;
            }
            EditorGUILayout.Space();
            _tabIndex = GUILayout.Toolbar(_tabIndex, _tabs);
            EditorGUILayout.Space();
            switch (_tabIndex)
            {
                case 0:
                    OnDrewStatistics();
                    break;
                case 1:
                    OnDrewOptimize();
                    break;
            }
        }
        
        /**
         * 优化工具界面
         */
        private void OnDrewOptimize()
        {
            // 显示优化建议
            if (_optimizeGroup == null || _optimizeGroup.Count == 0)
            {
                EditorGUILayout.LabelField("没有找到可优化的动骨组件");
                return;
            }

            var optimizeCount = 0;
            foreach (var (_, optimizeBones) in _optimizeGroup)
            {
                optimizeCount += optimizeBones.Count;
            }
            
            EditorGUILayout.HelpBox("此操作可能会导致不确定的错误，操作前请进行备份", MessageType.Warning);
            EditorGUILayout.LabelField($"可优化动骨组件：{_vrcPhysBones.Length}->{_vrcPhysBones.Length - optimizeCount}");
            EditorGUILayout.Space();
            _scrollOptimizePos = EditorGUILayout.BeginScrollView(_scrollOptimizePos);
            foreach (var (bone, optimizeBones) in _optimizeGroup)
            {
                EditorGUILayout.LabelField($"可将以下动骨组件进行合并：");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("目标对象：", bone.gameObject, typeof(GameObject), true);
                }
                foreach (var optimizeBone in optimizeBones)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField("目标对象：", optimizeBone.gameObject, typeof(GameObject), true);
                    }
                }
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("合并", GUILayout.Width(80)))
                {
                    // 合并动骨组件
                    var handles = new List<VRCPhysBone> { bone };
                    handles.AddRange(optimizeBones);
                    // 在组件当前层级创建一个对象，将动骨组件粘贴到该对象，将所有组件对象复制到该对象下，并且删除原组件
                    var newBoneGameObject = new GameObject(bone.name + "_merged");
                    Undo.RegisterCreatedObjectUndo(newBoneGameObject, "Create merged bone");
                    newBoneGameObject.transform.SetParent(bone.transform.parent);
                    Undo.RecordObject(newBoneGameObject.transform, "Copy transform");
                    newBoneGameObject.transform.localPosition = bone.transform.localPosition;
                    newBoneGameObject.transform.localRotation = bone.transform.localRotation;
                    newBoneGameObject.transform.localScale = bone.transform.localScale;
                    var components = bone.GetComponent<VRCPhysBone>();
                    // 创建新组件（Undo 版本）
                    var newComp = Undo.AddComponent(newBoneGameObject, components.GetType());
                    EditorUtility.CopySerialized(components, newComp);
                    
                    foreach (var vrcPhysBone in handles)
                    {
                        Undo.SetTransformParent(
                            vrcPhysBone.transform,
                            newBoneGameObject.transform,
                            "Move bone"
                        );
                        Undo.DestroyObjectImmediate(vrcPhysBone.GetComponent<VRCPhysBone>());
                    }
                    
                    // 刷新
                    Refresh();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(4);
            }
            EditorGUILayout.EndScrollView();
        }

        private void OnDrewStatistics()
        {
            EditorGUILayout.LabelField($"动骨数量：{_vrcPhysBones.Length}");
            EditorGUILayout.Space(5);

            // 列表区域
            _scrollStatisticsPos = EditorGUILayout.BeginScrollView(_scrollStatisticsPos);

            for (var i = 0; i < _vrcPhysBones.Length; i++)
            {
                var bone = _vrcPhysBones[i];
                if (bone == null) continue;

                var path = GetHierarchyPath(bone.transform);

                EditorGUILayout.BeginVertical("box");

                // 第一行：只读对象框，显示对应 GameObject
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("目标对象", bone.gameObject, typeof(GameObject), true);
                }
                // 第二行：只读可滚动文本，显示路径
                EnsureScrollSize(i);
                _pathStatisticsScrolls[i] = EditorGUILayout.BeginScrollView(_pathStatisticsScrolls[i], GUILayout.Height(40));
                EditorGUILayout.SelectableLabel(path, EditorStyles.wordWrappedLabel, GUILayout.Height(36));
                EditorGUILayout.EndScrollView();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();
        }

        private void Refresh()
        {
            Debug.Log($"刷新动骨组件列表");
            var vrcPhysBones = _gameObject.GetComponentsInChildren<VRCPhysBone>(true);
            _vrcPhysBones = vrcPhysBones
                .Where(pb => !IsInEditorOnlyHierarchy(pb.transform))
                .ToArray();
            _pathStatisticsScrolls.Clear();
            for (var i = 0; i < _vrcPhysBones.Length; i++)
            {
                _pathStatisticsScrolls.Add(Vector2.zero);
            }

            RefreshVrcPhysBoneOptimizeGroup();
        }

        private void RefreshVrcPhysBoneOptimizeGroup()
        {
            // 按层级路径分类动骨组件
            _optimizeGroup = new Dictionary<VRCPhysBone, List<VRCPhysBone>>();
            var boneGroups = new Dictionary<string, List<VRCPhysBone>>();
            foreach (var vrcPhysBone in _vrcPhysBones)
            {
                var path = GetParentPath(vrcPhysBone.transform);
                if (!boneGroups.ContainsKey(path))
                    boneGroups.Add(path, new List<VRCPhysBone>());
                boneGroups[path].Add(vrcPhysBone);
            }
            // 遍历组件，对同层级下动骨组件进行比较
            var handled = new HashSet<VRCPhysBone>();// 已经处理过的组件列表
            foreach (var (_, physBones) in boneGroups)
            {
                for (var i = 0; i < physBones.Count; i++)
                {
                    var bone1 = physBones[i];
                    if (handled.Contains(bone1)) continue;
                    for (var j = i + 1; j < physBones.Count; j++)
                    {
                        var bone2 = physBones[j];
                        if (handled.Contains(bone2)) continue;
                        if (!bone1.AreEqual(bone2)) continue;
                        if (!_optimizeGroup.ContainsKey(bone1))
                            _optimizeGroup.Add(bone1, new List<VRCPhysBone>());
                        _optimizeGroup[bone1].Add(bone2);
                        handled.Add(bone1);
                        handled.Add(bone2);
                    }
                }
            }
        }

        private void EnsureScrollSize(int index)
        {
            while (_pathStatisticsScrolls.Count <= index) _pathStatisticsScrolls.Add(Vector2.zero);
        }

        private static string GetHierarchyPath(Transform t)
        {
            if (t == null) return string.Empty;

            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }

            return string.Join("/", stack);
        }
        
        /**
         * 获取父对象的路径
         */
        private static string GetParentPath(Transform t)
        {
            if (t == null || t.parent == null) return string.Empty;
            var stack = new Stack<string>();
            t = t.parent;
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }
        
        private static bool IsInEditorOnlyHierarchy(Transform t)
        {
            while (t != null)
            {
                if (t.CompareTag("EditorOnly"))
                    return true;

                t = t.parent;
            }

            return false;
        }

        [MenuItem("CatTools/动骨工具")]
        public static void OpenWindow()
        {
            var window = GetWindow<CatToolsVrcPhysBoneTools>();
            window.titleContent = new GUIContent("动骨统计工具");
            window.Show();
        }
    }
}
#endif