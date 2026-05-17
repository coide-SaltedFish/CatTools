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
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace io.github.sereinfish.cat.tools.editor.inspector.ui
{
    public class MaterialSetterList
    {
        private readonly GameObject _avatarRoot;
        private readonly ReorderableList _list;
        private readonly SerializedProperty _rootListProp;
        private readonly SerializedObject _so;
        private Material[] _materials;
        private Object _lastTarget;
        
        public MaterialSetterList(GameObject avatarRoot, SerializedObject so)
        { 
            _so = so;
            _avatarRoot = avatarRoot;
            _rootListProp = _so.FindProperty(nameof(ConditionalMaterialSetter.setters));
            if (_rootListProp == null)
                throw new NullReferenceException("尝试在非ConditionalMaterialSetter组件中使用MaterialSetterList，没有找到 setters 字段");
            
            _list = new ReorderableList(so, _rootListProp, true, false, true, true)
            {
                drawElementCallback = DrawElement,
                elementHeightCallback = GetElementHeight,
                onAddCallback = AddOuterGroup
            };
        }
        
        public void DoLayout()
        {
            _list.DoLayoutList();
        }
        
        private float GetElementHeight(int index)
        {
            return (EditorGUIUtility.singleLineHeight + 2f) * 2 + EditorGUIUtility.standardVerticalSpacing;
        }
        
        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var lineH = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var prop = _rootListProp.GetArrayElementAtIndex(index);

            var targetProp = prop.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.target));
            var slotProp = prop.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.slot));
            var materialProp = prop.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.material));
            
            var x = rect.x;
            var y = rect.y + 1f;
            // 第一行，目标对象及槽位选择
            if (_lastTarget == null) _lastTarget = targetProp.objectReferenceValue;
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(x, y, rect.width * 0.7f, lineH), targetProp, GUIContent.none);
            if (EditorGUI.EndChangeCheck() && targetProp.objectReferenceValue != _lastTarget)
            {
                if (targetProp.objectReferenceValue == null)
                {
                    slotProp.intValue = 0;
                    materialProp.objectReferenceValue = null;
                    _materials = null;
                }
                else
                {
                    // 检查槽位长度是否合法
                    var smr = (targetProp.objectReferenceValue as Transform)?.GetComponent<SkinnedMeshRenderer>();
                    if (smr != null)
                    {
                        if (smr.sharedMaterials.Length < slotProp.intValue)
                            slotProp.intValue = 0;
                    }
                }
                _lastTarget = targetProp.objectReferenceValue;
            }
            
            // 初始化材质槽
            if (targetProp.objectReferenceValue != null)
            {
                var smr = (targetProp.objectReferenceValue as Transform)?.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    _materials = smr.sharedMaterials;
                }
                else
                {
                    y += lineH + 2f + spacing;
                    EditorGUI.LabelField(new Rect(x, y, rect.width , lineH),  "目标对象不包含 SkinnedMeshRenderer 组件");
                    return;   
                }
            }
            
            // 材质槽下拉框
            x += rect.width * 0.7f + spacing;
            var slotOptions = new string[_materials?.Length ?? 0];
            if (slotOptions == null) throw new ArgumentNullException(nameof(slotOptions));
            for (var i = 0; i < _materials?.Length; i++)
            {
                slotOptions[i] = $"Element {i}:{_materials[i].name}";
            }
            slotProp.intValue = EditorGUI.Popup(new Rect(x, y, rect.width * 0.3f, lineH), slotProp.intValue, slotOptions);
            
            x = rect.x;
            y += lineH + 2f + spacing;
            // 第二行 默认材质 及 要设置的材质
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ObjectField(new Rect(x, y, rect.width * 0.45f, lineH), _materials?[slotProp.intValue], typeof(Material), false);
            EditorGUI.EndDisabledGroup();
            x += rect.width * 0.45f + spacing;
            EditorGUI.LabelField(new Rect(x, y, 20f, lineH), "=>");
            x += 20f + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width - rect.width * 0.45f - 20f, lineH), materialProp, GUIContent.none);
        }
        
        private void AddOuterGroup(ReorderableList list)
        {
            var prop = list.serializedProperty;
            prop.InsertArrayElementAtIndex(prop.arraySize);
            // 给新元素设置默认值
            var newElem = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            newElem.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.target)).objectReferenceValue = null;
            newElem.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.slot)).intValue = 0;
            newElem.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.material)).objectReferenceValue = null;
            list.index = prop.arraySize - 1;
        }
    }
}