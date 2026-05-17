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

using System.Linq;
using io.github.sereinfish.cat.tools.Components;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(SelfConditionalMaterialSetter))]
    public class SelfConditionalMaterialSetterInspector : ConditionalEditor<SelfConditionalMaterialSetter>
    {
        private SerializedProperty _rootListProp;
        private SerializedProperty _restoreToggleProp;
        private ReorderableList _list;
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        
        protected override void Init()
        {
            base.Init();
            _rootListProp = PropGet(nameof(ConditionalMaterialSetter.setters));
            _restoreToggleProp = PropGet(nameof(ConditionalMaterialSetter.restoreToggle));
            
            _list = new ReorderableList(serializedObject, _rootListProp, true, false, true, true)
            {
                drawElementCallback = DrawElement,
                elementHeightCallback = GetElementHeight,
                onAddCallback = AddOuterGroup
            };
            
            _skinnedMeshRenderer = Target.GetComponent<SkinnedMeshRenderer>();
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            if (_skinnedMeshRenderer == null)
            {
                EditorGUILayout.HelpBox("此对象下没有任何有效的 SkinnedMeshRenderer 组件，组件暂时不可用。", MessageType.Error);
                return;
            }
            _list.DoLayoutList();
            
            EditorGUILayout.PropertyField(_restoreToggleProp, new GUIContent("条件不满足时恢复为默认值"));
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

            var slotProp = prop.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.slot));
            var materialProp = prop.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.material));
            
            var x = rect.x;
            var y = rect.y + 1f;
            // 第一行，槽位选择 及 默认材质
            slotProp.intValue = EditorGUI.Popup(new Rect(x, y, rect.width * 0.3f, lineH), slotProp.intValue, GetMaterialNames());
            x += rect.width * 0.3f + spacing;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ObjectField(new Rect(x, y, rect.width * 0.7f, lineH), _skinnedMeshRenderer?.sharedMaterials?[slotProp.intValue], typeof(Material), false);
            EditorGUI.EndDisabledGroup();
            
            x = rect.x;
            y += lineH + 2f + spacing;
            // 第二行 要设置的材质
            EditorGUI.LabelField(new Rect(x, y, 50f, lineH), "设置为：");
            x += 50f + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width - 50f, lineH), materialProp, GUIContent.none);
        }
        
        private void AddOuterGroup(ReorderableList list)
        {
            var prop = list.serializedProperty;
            prop.InsertArrayElementAtIndex(prop.arraySize);
            // 给新元素设置默认值
            var newElem = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            newElem.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.target)).objectReferenceValue = Target.transform;
            newElem.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.slot)).intValue = 0;
            newElem.FindPropertyRelative(nameof(ConditionalMaterialSetter.MaterialSetter.material)).objectReferenceValue = null;
            list.index = prop.arraySize - 1;
        }
        
        private string[] GetMaterialNames()
        { 
            return _skinnedMeshRenderer?.sharedMaterials.Select(x => x.name).ToArray();
        }
    }
}