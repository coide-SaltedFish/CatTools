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
    [CustomEditor(typeof(ParameterMaterial))]
    public class ParameterMaterialEditor : Editor
    {
        private Component _component;

        // 条件列表
        private ParameterConditionList _conditionList;

        // 材质列表
        private MaterialChangeList _materialChangeList;

        // 序列化对象和属性
        private SerializedObject _so;
        private SerializedProperty _validProp;

        private void OnEnable()
        {
            _so = new SerializedObject(target);

            _component = target as ParameterMaterial;

            _validProp = serializedObject.FindProperty("isValid");

            _conditionList = new ParameterConditionList(_so, "conditions");
            _materialChangeList =
                new MaterialChangeList(_so, "materials", () => _component.GetComponent<SkinnedMeshRenderer>());
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("设置条件参数控制对象材质", MessageType.Info);

            if (!_validProp.boolValue)
            {
                EditorGUILayout.HelpBox("此对象下没有任何有效的 SkinnedMeshRenderer 组件，组件暂时不可用。", MessageType.Error);
                return;
            }

            EditorGUILayout.PropertyField(_so.FindProperty("layerType"), new GUIContent("Layer类型"));
            _conditionList.DoLayout();
            _materialChangeList.DoLayout();

            serializedObject.ApplyModifiedProperties();
        }
    }
}