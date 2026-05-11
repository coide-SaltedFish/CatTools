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
    [CustomEditor(typeof(TargetsConditionalToggle))]
    public class TargetsConditionalToggleInspector : ConditionalEditor<TargetsConditionalToggle>
    {
        private SerializedProperty _defaultActiveProp;
        private SerializedProperty _reverseToggleProp;

        private ReorderableList _targetsList;
        private SerializedProperty _targetsProp;
        private SerializedProperty _toggleProp;
        private SerializedProperty _isSetDefaultActiveProp;

        protected override void Init()
        {
            base.Init();

            _toggleProp = PropGet(nameof(TargetsConditionalToggle.toggle));
            _reverseToggleProp = PropGet(nameof(TargetsConditionalToggle.reverseToggle));
            _defaultActiveProp = PropGet(nameof(TargetsConditionalToggle.defaultActive));
            _targetsProp = PropGet(nameof(TargetsConditionalToggle.targets));
            _isSetDefaultActiveProp = PropGet(nameof(TargetsConditionalToggle.isSetDefaultActive));

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

        protected override void OnDraw()
        {
            base.OnDraw();
            var listRect = GUILayoutUtility.GetRect(0, _targetsList.GetHeight(), GUILayout.ExpandWidth(true));
            _targetsList.DoList(listRect);

            HandleDragAndDrop(listRect);

            EditorGUILayout.PropertyField(_toggleProp, new GUIContent("条件满足时状态"));
            EditorGUILayout.PropertyField(_reverseToggleProp, new GUIContent("条件不满足时是否反转状态"));
            EditorGUILayout.PropertyField(_isSetDefaultActiveProp, new GUIContent("构建时设置默认状态"));
            if (_isSetDefaultActiveProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(_defaultActiveProp, new GUIContent("构建时将开关状态设置为"));
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
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