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
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.inspector.window;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(ConditionalMatchMaterialsSetter))]
    public class ConditionalMatchMaterialsSetterInspector : ConditionalEditor<ConditionalMatchMaterialsSetter>
    {
        private SerializedProperty _includeChildrenProp;
        private SerializedProperty _targetPathProp;
        private SerializedProperty _matchExpressionProp;
        private SerializedProperty _ignoreStringProp;
        private SerializedProperty _materialHandlerProp;

        protected override void Init()
        {
            base.Init();

            _includeChildrenProp = PropGet(nameof(ConditionalMatchMaterialsSetter.includeChildren));
            _targetPathProp = PropGet(nameof(ConditionalMatchMaterialsSetter.targetPath));
            _matchExpressionProp = PropGet(nameof(ConditionalMatchMaterialsSetter.matchExpression));
            _ignoreStringProp = PropGet(nameof(ConditionalMatchMaterialsSetter.ignoreString));
            _materialHandlerProp = PropGet(nameof(ConditionalMatchMaterialsSetter.materialHandler));
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            
            EditorGUILayout.PropertyField(_includeChildrenProp);
            EditorGUILayout.PropertyField(_targetPathProp);
            EditorGUILayout.PropertyField(_matchExpressionProp);
            EditorGUILayout.PropertyField(_ignoreStringProp);
            
            DrawMaterialHandlerSelector();
            
            if (GUILayout.Button("打开调试窗口"))
            {
                ConditionalMatchMaterialsSetterDebugWindows.ShowWindow(target as ConditionalMatchMaterialsSetter);
            }
        }
        
        private void DrawMaterialHandlerSelector()
        {
            var currentName = "未配置材质处理器";
            if (_materialHandlerProp.managedReferenceValue != null)
            {
                var handler = _materialHandlerProp.managedReferenceValue as ConditionalMatchMaterialsSetter.IMaterialHandler;
                if (handler != null)
                {
                    currentName = handler.HandlerName;
                    if (string.IsNullOrEmpty(currentName))
                    {
                        currentName = _materialHandlerProp.managedReferenceValue
                            .GetType()
                            .Name;
                    }
                }
                else
                {
                    currentName = _materialHandlerProp.managedReferenceValue
                        .GetType()
                        .Name;
                }
            }
            if (GUILayout.Button($"{currentName}", EditorStyles.popup))
            {
                ShowMaterialHandlerMenu();
            }
        }
        
        private void ShowMaterialHandlerMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("None"), _materialHandlerProp.managedReferenceValue == null, () =>
            {
                SetHandler(null);
            });


            // 点击这里以后才扫描
            var handlers = ConditionalMatchMaterialsSetter.GetMaterialHandlers();
            Debug.Log("ConditionalMatchMaterialsSetter: 执行 IMaterialHandler 扫描...");

            foreach (var materialHandler in handlers)
            {
                Debug.Log($"ConditionalMatchMaterialsSetter: -- {materialHandler.HandlerName} -> {materialHandler.GetType()}");
                var itemName = materialHandler.HandlerName;
                
                if (string.IsNullOrEmpty(itemName)) itemName = materialHandler.GetType().Name;
                
                menu.AddItem(new GUIContent(itemName), false, () => 
                {
                    SetHandler(materialHandler.GetType());
                });
            }
            menu.ShowAsContext();
        }
        
        private void SetHandler(Type type)
        {
            _materialHandlerProp.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
        }
    }
}