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

using CatTools.editor.ui;
using CatTools.Runtime;
using UnityEditor;
using UnityEngine;

namespace CatTools.editor.Inspector
{
    [CustomEditor(typeof(ParameterToggle))]
    public class ParameterToggleEditor : Editor
    {
        // 序列化对象和属性
        private SerializedObject _so;

        // 条件列表
        private ParameterConditionList _conditionList;

        private void OnEnable()
        {
            _so = new SerializedObject(target);
            _conditionList = new ParameterConditionList(_so, "conditions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("设置条件参数控制对象开关", MessageType.Info);

            EditorGUILayout.PropertyField(_so.FindProperty("layerType"), new GUIContent("Layer类型"));

            EditorGUILayout.PropertyField(_so.FindProperty("toggle"), new GUIContent("条件成立时启用对象"));

            _conditionList.DoLayout();
            
            // 绘制动画构建器界面
            // 准备拿到 animationBuilderType 的 SerializedProperty
            // var builderTypeProp = _so.FindProperty("animationBuilderType");

            // 开始监测它的值有没有变化
            // EditorGUI.BeginChangeCheck();
            // EditorGUILayout.PropertyField(builderTypeProp, new GUIContent("动画构建器"));
            // if (EditorGUI.EndChangeCheck())
            // {
            //     // 先应用修改到 SerializedObject（把枚举值更新到 target 上）
            //     _so.ApplyModifiedProperties();
            //
            //     // 根据新的枚举值创建实例
            //     var newType = (AnimationBuilderType)builderTypeProp.enumValueIndex;
            //     var instance = newType.Create(_so, _so.FindProperty("AnimationBuilder"));
            //
            //     // 直接写入目标对象的字段
            //     ((ParameterToggle)target).AnimationBuilder = instance;
            //
            //     // 标记场景/对象已修改
            //     EditorUtility.SetDirty(target);
            // }
            // // 绘制动画构建器界面
            // if (((ParameterToggle)target).AnimationBuilder != null)
            // {
            //     ((ParameterToggle)target).AnimationBuilder.DoLayout();
            // }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}