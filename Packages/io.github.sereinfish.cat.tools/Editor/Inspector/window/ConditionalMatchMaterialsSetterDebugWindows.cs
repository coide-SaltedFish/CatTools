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
using VRC.SDK3.Avatars.Components;

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
            // 设置自动材质处理
            DrawMaterialHandlerSelector(target);
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

            return (from renderer in _target.GetComponentsInChildren<Renderer>(true)
                where renderer.sharedMaterials.Any(sharedMaterial => sharedMaterial == source)
                select renderer.transform)
                .ToArray();
        }
        
        private void RefreshData()
        {
            // 执行再次扫描，更新材质列表数据
            _data = new Dictionary<Material, List<Material>>();
            _foldoutStates.Clear();
            _transformStates.Clear();
            foreach (var material in _target.gameObject.FindChildMaterials())
            {
                var targets = _target.FindTargetsMaterial(material);
                _data.Add(material, new List<Material>(targets));
            }
            // 刷新材质处理器数据
            RefreshMaterialHandlerData();
        }

        private void RefreshMaterialHandlerData()
        {
            _target.autoHandleMaterials = _target.autoHandleMaterials.Where(CheckMaterialHandler).ToList();
        }

        private void RemoveMaterialHandlerData(Material material)
        {
            if (material == null) return;
            _target.autoHandleMaterials = _target.autoHandleMaterials
                .Where(handleMaterial => handleMaterial.materialPath != GlobalObjectId.GetGlobalObjectIdSlow(material).ToString())
                .ToList();
        }

        /// <summary>
        /// 检查材质处理器配置是否还可用
        /// </summary>
        /// <param name="autoHandleMaterial"></param>
        /// <returns></returns>
        private bool CheckMaterialHandler(ConditionalMatchMaterialsSetter.AutoHandleMaterial autoHandleMaterial)
        {
            foreach (var sourceMaterial in _data.Keys)
            {
                var sourceMaterialPath = GlobalObjectId.GetGlobalObjectIdSlow(sourceMaterial).ToString();
                if (autoHandleMaterial.materialPath == sourceMaterialPath) return true;
            }

            return false;
        }

        private ConditionalMatchMaterialsSetter.AutoHandleMaterial GetAutoHandler(Material material)
        {
            foreach (var targetAutoHandleMaterial in _target.autoHandleMaterials)
            {
                var sourceMaterialPath = GlobalObjectId.GetGlobalObjectIdSlow(material).ToString();
                if (targetAutoHandleMaterial.materialPath == sourceMaterialPath)
                {
                    return targetAutoHandleMaterial;
                }
            }
            return null;
        }
        
        private void DrawMaterialHandlerSelector(Material targetMaterial)
        {
            // 查找当前材质处理器
            var autoHandler = GetAutoHandler(targetMaterial);
            var currentHandler = autoHandler?.materialHandler;
            if (currentHandler == null && targetMaterial == null)
            {
                currentHandler = _target.materialHandler;
            }

            var currentName = "未配置材质处理器";
            if (currentHandler != null)
            {
                currentName = currentHandler.HandlerName;
                if (string.IsNullOrEmpty(currentName))
                {
                    currentName = currentHandler.GetType().Name;
                }  
            }
            var popupStyle = new GUIStyle(EditorStyles.popup);
            if (currentName == "未配置材质处理器")
            {
                popupStyle = new GUIStyle(EditorStyles.popup)
                {
                    normal =
                    {
                        textColor = Color.yellow
                    }
                };
            }
            
            if (GUILayout.Button($"{currentName}", popupStyle))
            {
                ShowMaterialHandlerMenu(autoHandler, targetMaterial);
            }
        }
        
        private void ShowMaterialHandlerMenu(ConditionalMatchMaterialsSetter.AutoHandleMaterial autoHandle, Material targetMaterial)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("None"), autoHandle == null, () =>
            {
                SetHandler(null, targetMaterial);
            });


            // 点击这里以后才扫描
            var handlers = ConditionalMatchMaterialsSetter.GetMaterialHandlers();

            foreach (var materialHandler in handlers)
            {
                var itemName = materialHandler.HandlerName;
                
                if (string.IsNullOrEmpty(itemName)) itemName = materialHandler.GetType().Name;
                
                menu.AddItem(new GUIContent(itemName), false, () => 
                {
                    SetHandler(materialHandler.GetType(), targetMaterial);
                });
            }
            menu.ShowAsContext();
        }
        
        private void SetHandler(Type type, Material targetMaterial)
        {
            var autoHandler = GetAutoHandler(targetMaterial);
            if (autoHandler == null)
            {
                autoHandler = new ConditionalMatchMaterialsSetter.AutoHandleMaterial
                {
                    materialPath = GlobalObjectId.GetGlobalObjectIdSlow(targetMaterial).ToString(),
                    materialHandler = type == null ? null : Activator.CreateInstance(type) as ConditionalMatchMaterialsSetter.IMaterialHandler
                };
                _target.autoHandleMaterials.Add(autoHandler);
            }
            else
            {
                autoHandler.materialHandler = type == null
                    ? null
                    : Activator.CreateInstance(type) as ConditionalMatchMaterialsSetter.IMaterialHandler;
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