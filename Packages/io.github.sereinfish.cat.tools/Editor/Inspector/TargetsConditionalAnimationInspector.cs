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

using System.Collections.Generic;
using io.github.sereinfish.cat.tools.Components;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(TargetsConditionalAnimation))]
    public class TargetsConditionalAnimationInspector : ConditionalEditor<TargetsConditionalAnimation>
    {
        private SerializedProperty _targetsProp;
        private SerializedProperty _animationsProp;
        private ReorderableList _list;
        private ReorderableList _targetsList;
        
        protected override void Init()
        {
            base.Init();
            _targetsProp = PropGet(nameof(ConditionalAnimation.targets));
            _animationsProp = PropGet(nameof(ConditionalAnimation.animations));
            
            InitList();
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            
            var listRect = GUILayoutUtility.GetRect(0, _targetsList.GetHeight(), GUILayout.ExpandWidth(true));
            _targetsList.DoList(listRect);
            HandleDragAndDrop(listRect);
            
            _list.DoLayoutList();
        }

        private void InitList()
        {
            _list = new ReorderableList(serializedObject, _animationsProp, true, false, true, true)
            {
                drawElementCallback = DrawElement,
                elementHeightCallback = _ => (EditorGUIUtility.singleLineHeight + 2f) * 2 + EditorGUIUtility.standardVerticalSpacing,
                onAddCallback = list =>
                {
                    var prop = list.serializedProperty;
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    // 给新元素设置默认值
                    var newElem = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                    newElem.FindPropertyRelative(nameof(ConditionalAnimation.MergeAnimation.overrideTargetPath)).boolValue = true;
                    list.index = prop.arraySize - 1;
                }
            };
            _targetsList = new ReorderableList(serializedObject, _targetsProp, true, true, true, true)
            {
                drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "目标对象列表"); },
                drawElementCallback = (rect, index, _, _) =>
                {
                    var element = _targetsProp.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y + 1, rect.width, EditorGUIUtility.singleLineHeight),
                        element,
                        GUIContent.none
                    );
                },
                elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 2f
            };
        }
        
        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var lineH = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var prop = _animationsProp.GetArrayElementAtIndex(index);
            
            var clip = prop.FindPropertyRelative(nameof(ConditionalAnimation.MergeAnimation.clip));
            var mergeType = prop.FindPropertyRelative(nameof(ConditionalAnimation.MergeAnimation.mergeType));
            var overrideTargetPath = prop.FindPropertyRelative(nameof(ConditionalAnimation.MergeAnimation.overrideTargetPath));
            var applyToChildren = prop.FindPropertyRelative(nameof(ConditionalAnimation.MergeAnimation.applyToChildren));
            
            var x = rect.x;
            var y = rect.y + 1f;
            // 第一行，动画选择 及 合并类型
            EditorGUI.PropertyField(new Rect(x, y, rect.width * 0.65f, lineH), clip, GUIContent.none);
            x += rect.width * 0.65f + spacing;
            EditorGUI.PropertyField(new Rect(x, y, rect.width * 0.35f, lineH), mergeType, GUIContent.none);
            
            x = rect.x;
            y += lineH + 2f + spacing;
            // 第二行 是否重写目标路径 是否包含所有子对象
            EditorGUI.LabelField(new Rect(x, y, 80f, lineH), "重写目标路径");
            x += 80f + spacing;
            EditorGUI.PropertyField(new Rect(x, y, 20f, lineH), overrideTargetPath, GUIContent.none);
            x += 20f + spacing;
            EditorGUI.LabelField(new Rect(x, y, 104f, lineH), "应用到所有子对象");
            x += 104f + spacing;
            EditorGUI.PropertyField(new Rect(x, y, 20f, lineH), applyToChildren, GUIContent.none);
        }
        
        private void HandleDragAndDrop(Rect dropArea)
        {
            var evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition))
                return;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;
            var draggedObjects = GetDraggedGameObjects(DragAndDrop.objectReferences);
            if (draggedObjects.Length == 0)
                return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                Undo.RecordObject(target, "Add Targets");
                serializedObject.Update();
                foreach (var go in draggedObjects)
                    if (!ContainsTarget(go))
                        AddTarget(go);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
            evt.Use();
        }

        private GameObject[] GetDraggedGameObjects(Object[] refs)
        {
            var result = new List<GameObject>();
            foreach (var obj in refs)
            {
                GameObject go = null;
                if (obj is GameObject)
                    go = obj as GameObject;
                else if (obj is Component comp)
                    go = comp.gameObject;
                else if (obj is Transform tr)
                    go = tr.gameObject;
                if (go != null && !result.Contains(go))
                    result.Add(go);
            }
            return result.ToArray();
        }

        private bool ContainsTarget(GameObject go)
        {
            for (var i = 0; i < _targetsProp.arraySize; i++)
            {
                var element = _targetsProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == go)
                    return true;
            }

            return false;
        }

        private void AddTarget(GameObject go)
        {
            var index = _targetsProp.arraySize;
            _targetsProp.InsertArrayElementAtIndex(index);
            _targetsProp.GetArrayElementAtIndex(index).objectReferenceValue = go;
        }
    }
}