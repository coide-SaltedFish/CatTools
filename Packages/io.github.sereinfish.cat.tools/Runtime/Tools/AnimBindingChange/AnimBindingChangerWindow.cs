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

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.Tools.AnimBindingChange
{
    public class AnimBindingChangerWindow : EditorWindow
    {
        private Vector2 _scrollStatisticsPos;
        
        private AnimationClip animClip; // 目标动画
        private AnimationClip lastAnimClip; // 上一个目标动画
        private Transform rootTransform; // 根 Transform
        
        private Dictionary<EditorCurveBinding, CurveBindingsData> curveBindingsDataList = new();
        
        // private readonly string[] _tabs = { "CurveBindings", "ObjectReferenceCurveBindings" };
        // private int _tabIndex = 0;
        
        [MenuItem("CatTools/动画绑定编辑器")]
        private static void Open()
        {
            GetWindow<AnimBindingChangerWindow>("Anim Binding Changer");
        }
        
        private void OnGUI()
        {
            animClip = (AnimationClip)EditorGUILayout.ObjectField("目标动画：", animClip, typeof(AnimationClip), false);
            rootTransform = (Transform)EditorGUILayout.ObjectField("根对象", rootTransform, typeof(Transform), true);

            if (GUILayout.Button("刷新", GUILayout.Height(30)))
                RefreshData();

            if (animClip != null && lastAnimClip == null)
                lastAnimClip = animClip;

            if (!Equals(animClip, lastAnimClip))
            {
                lastAnimClip = animClip;
                RefreshData();
            }

            if (animClip == null || rootTransform == null)
                return;

            EditorGUILayout.Space();

            // 可滚动区域
            _scrollStatisticsPos = EditorGUILayout.BeginScrollView(_scrollStatisticsPos);

            foreach (var binding in curveBindingsDataList.Values)
            {
                if (binding.isObjIsActive || binding.isBlendShape)
                {
                    CurveBindingsItem(binding);   
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 固定在最下方，不参与滚动
            if (GUILayout.Button("应用", GUILayout.Height(35)))
            {
                Save();
            }
        }

        /// <summary>
        /// 绑定列表项
        /// </summary>
        private void CurveBindingsItem(CurveBindingsData binding)
        {
            var halfWidth = (position.width - 10) * 0.5f;
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical("box", GUILayout.Width(halfWidth - 10f));

            GUILayout.Label($"Target: {binding.Path}");
            GUILayout.Label($"Property: {binding.PropertyName}");

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            // 右侧
            EditorGUILayout.BeginVertical("box", GUILayout.Width(halfWidth));
            
            // 编辑界面
            var buttonWidth = halfWidth * 0.15f;
            var editWidth = halfWidth - buttonWidth - 10;
            if (binding.isObjIsActive)
            {
                if (binding.changeSetter == null)
                {
                    if (GUILayout.Button("修改")) binding.changeSetter = new CurveBindingsObjIsActiveChangeSetter(rootTransform);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    // 左侧编辑区域
                    GUILayout.BeginVertical(GUILayout.Width(editWidth));
                    ObjIsActiveChangeUI(binding);
                    GUILayout.EndVertical();
                    // 间隔
                    GUILayout.Space(10);
                    // 右侧按钮
                    if (GUILayout.Button("移除", GUILayout.Width(buttonWidth)))
                    {
                        binding.changeSetter = null;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else if (binding.isBlendShape)
            {
                if (binding.changeSetter == null)
                {
                    if (GUILayout.Button("修改")) binding.changeSetter = new CurveBindingsBlendShapeChangeSetter();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    // 左侧编辑区域
                    GUILayout.BeginVertical(GUILayout.Width(editWidth));
                    BlendShapeChangeUI(binding);
                    GUILayout.EndVertical();
                    // 间隔
                    GUILayout.Space(10);
                    // 右侧按钮
                    if (GUILayout.Button("移除", GUILayout.Width(buttonWidth)))
                    {
                        binding.changeSetter = null;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        
        // private void ObjectReferenceCurveBindingsItem(EditorCurveBinding binding)
        // {
        //     
        // }

        private void ObjIsActiveChangeUI(CurveBindingsData binding)
        {
            var setter = binding.GetSetter<CurveBindingsObjIsActiveChangeSetter>();
            GUILayout.Label("重定向目标对象: ");
            if (setter == null) return;
            // 显示目标对象
            setter.target = (Transform)EditorGUILayout.ObjectField("目标对象", setter.target, typeof(Transform), true);
        }
        
        private void BlendShapeChangeUI(CurveBindingsData binding)
        {
            var setter = binding.GetSetter<CurveBindingsBlendShapeChangeSetter>();
            if (setter == null) return;
            GUILayout.Label("重定向形态键: ");
            // 显示目标形态键
            setter.target = (Transform)EditorGUILayout.ObjectField("目标对象", setter.target, typeof(Transform), true);
            // 目标不为空，获取 SkinnedMeshRenderer
            var smr = setter.target?.GetComponent<SkinnedMeshRenderer>();
            if (smr == null)
            {
                GUILayout.Label("目标对象上没有 SkinnedMeshRenderer 组件");
            }
            else if (smr.sharedMesh == null)
            {
                GUILayout.Label("SkinnedMeshRenderer 没有 Mesh");
            }
            else
            {
                var mesh = smr.sharedMesh;
                // 获取 BlendShape 名称
                var blendShapeNames = new List<string>();
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    blendShapeNames.Add(mesh.GetBlendShapeName(i));
                }

                // 当前是否存在
                int index = blendShapeNames.IndexOf(setter.blendShapeName);
                bool missing = index < 0;

                // 不存在的话，把旧名字插到第一项
                if (missing)
                {
                    blendShapeNames.Insert(0, setter.blendShapeName);
                    index = 0;
                }

                // 不存在时文字标红
                var oldColor = GUI.contentColor;
                if (missing) GUI.contentColor = Color.red;

                var newIndex = EditorGUILayout.Popup(
                    "Blend Shape",
                    index,
                    blendShapeNames.ToArray());

                GUI.contentColor = oldColor;

                // 更新选择
                setter.blendShapeName = blendShapeNames[newIndex];
            }
        }
        
        private void RefreshData()
        {
            curveBindingsDataList.Clear();
            if (animClip == null) return;
            foreach (var binding in AnimationUtility.GetCurveBindings(animClip))
            {
                if (!curveBindingsDataList.ContainsKey(binding))
                    curveBindingsDataList.Add(binding, new CurveBindingsData(CurveBindingsType.FloatCurves, binding));
            }
            foreach (var obj in AnimationUtility.GetObjectReferenceCurveBindings(animClip))
            {
                if (!curveBindingsDataList.ContainsKey(obj))
                    curveBindingsDataList.Add(obj, new CurveBindingsData(CurveBindingsType.ObjectReferenceCurves, obj));
            }
        }
        
        private void Save()
        {
            Undo.RegisterCompleteObjectUndo(animClip, "Replace Animation Binding");
            foreach (var binding in curveBindingsDataList.Values.ToList())
            {
                if (binding.changeSetter == null) continue;
                
                var curve = AnimationUtility.GetEditorCurve(animClip, binding.binding);
                if (curve == null) continue;
                
                AnimationUtility.SetEditorCurve(animClip, binding.binding, null);
                
                var buildBinding = binding.Build();
                AnimationUtility.SetEditorCurve(animClip, buildBinding, curve);
            }
            EditorUtility.SetDirty(animClip);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Replace Finished.");
        }
        
        private static class PropertyNameEnum
        {
            public static string ObjIsActive = "m_IsActive";
            public static string MaterialsSlotData(int slot) => $"m_Materials.Array.data[{slot}]";
        }
        
        private enum CurveBindingsType
        {
            FloatCurves,
            ObjectReferenceCurves   
        }
        
        private class CurveBindingsData
        {
            public CurveBindingsType type;
            public EditorCurveBinding binding;
            public CurveBindingsChangeSetter changeSetter;
            
            public string Path => binding.path;
            public string PropertyName => binding.propertyName;
            public bool isObjIsActive => PropertyName.Equals(PropertyNameEnum.ObjIsActive);
            public bool isBlendShape => PropertyName.StartsWith("blendShape.");

            public CurveBindingsData(CurveBindingsType type, EditorCurveBinding binding)
            {
                this.type = type;
                this.binding = binding;
            }
            
            public CurveBindingsData(CurveBindingsType type, EditorCurveBinding binding, CurveBindingsChangeSetter changeSetter)
            {
                this.type = type;
                this.binding = binding;
                this.changeSetter = changeSetter;
            }

            public void SetPath(string path)
            {
                changeSetter.path = path;
            }
            
            public void SetProperty(string property)
            {
                changeSetter.property = property;
            }

            public T GetSetter<T>() where T : CurveBindingsChangeSetter
            {
                return changeSetter as T;
            }
            
            public EditorCurveBinding Build()
            {
                return changeSetter?.Build(binding) ?? binding;
            }
        }
        
        private abstract class CurveBindingsChangeSetter
        {
            public string path;
            public string property;

            public abstract EditorCurveBinding Build(EditorCurveBinding clip);
        }
        
        private class CurveBindingsObjIsActiveChangeSetter : CurveBindingsChangeSetter
        {
            public Transform rootTransform;
            public Transform target;

            public CurveBindingsObjIsActiveChangeSetter(Transform rootTransform)
            {
                this.rootTransform = rootTransform;
            }
            
            public override EditorCurveBinding Build(EditorCurveBinding binding)
            {
                if (rootTransform == null || target == null)
                {
                    Debug.LogWarning("Root Transform or Target is null");
                    return binding;
                }
                var newPath = target.GetRelativePath(rootTransform);
                binding.path = newPath;
                return binding;
            }
        }
        
        private class CurveBindingsBlendShapeChangeSetter : CurveBindingsChangeSetter
        {
            public Transform target;
            public string blendShapeName;
            
            public override EditorCurveBinding Build(EditorCurveBinding binding)
            {
                const string prefix = "blendShape.";
                binding.propertyName = $"{prefix}{blendShapeName}";
                return binding;
            }
        }
    }
}
#endif