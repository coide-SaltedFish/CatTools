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

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;

namespace CatTools.editor.ui
{
    public class ParametersList
    {
        private readonly ReorderableList _list;
        private readonly SerializedProperty _listProp;
        private readonly SerializedObject _serializedObject;

        public ParametersList(SerializedObject so, string propertyName)
        {
            _serializedObject = so;
            _listProp = so.FindProperty(propertyName);
            if (_listProp == null)
            {
                Debug.LogError($"Could not find property '{propertyName}'");
                return;
            }

            _list = new ReorderableList(so, _listProp, true, true, true, true);
            _list.drawHeaderCallback = DrawHeader;
            _list.elementHeightCallback = GetElementHeight;
            _list.drawElementCallback = DrawElement;
            _list.onAddCallback = OnAddElement;
        }

        public void DoLayoutList()
        {
            _list.DoLayoutList();
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, _listProp.displayName);
        }

        private float GetElementHeight(int index)
        {
            var singleLine = EditorGUIUtility.singleLineHeight;
            var verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            return singleLine * 2 + verticalSpacing * 2;
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _listProp.GetArrayElementAtIndex(index);
            var nameProp = element.FindPropertyRelative("name");
            var typeProp = element.FindPropertyRelative("type");
            var defaultProp = element.FindPropertyRelative("defaultValue");
            var isLocalProp = element.FindPropertyRelative("isLocal");
            var saveProp = element.FindPropertyRelative("save");
            var syncProp = element.FindPropertyRelative("sync");

            var row1 = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            var row2 = new Rect(rect.x,
                rect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, rect.width,
                EditorGUIUtility.singleLineHeight);

            // 第一行: 名称 + 类型
            var typeWidth = rect.width * 0.3f;
            var nameWidth = rect.width - typeWidth - 4;
            var nameRect = new Rect(row1.x, row1.y, nameWidth, row1.height);
            var typeRect = new Rect(nameRect.xMax + 4, row1.y, typeWidth, row1.height);
            EditorGUI.PropertyField(nameRect, nameProp, GUIContent.none);
            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

            // 第二行: 默认值标签 + 默认值输入 + 切换选项
            var labelWidth = 50f;
            var fieldWidth = rect.width * 0.2f;
            var labelRect = new Rect(row2.x, row2.y, labelWidth, row2.height);
            var defaultRect = new Rect(labelRect.xMax + 4, row2.y, fieldWidth, row2.height);
            EditorGUI.LabelField(labelRect, "默认值:");

            // 根据参数类型显示不同控件
            var paramType = (AnimatorControllerParameterType)typeProp.enumValueFlag;
            if (paramType == AnimatorControllerParameterType.Bool ||
                paramType == AnimatorControllerParameterType.Trigger)
            {
                var boolIndex = Mathf.Approximately(defaultProp.floatValue, 1f) ? 1 : 0;
                boolIndex = EditorGUI.Popup(defaultRect, boolIndex, new[] { "False", "True" });
                defaultProp.floatValue = boolIndex;
            }
            else
            {
                var val = EditorGUI.FloatField(defaultRect, defaultProp.floatValue);
                defaultProp.floatValue = Mathf.Clamp(val, 0f, 255f);
            }

            // 三个单选框: isLocal, save, sync
            var togglesTotalWidth = rect.width - labelWidth - fieldWidth - 12;
            var toggleWidth = togglesTotalWidth / 3f;
            var localRect = new Rect(defaultRect.xMax + 8, row2.y, toggleWidth, row2.height);
            var saveRect = new Rect(localRect.xMax + 4, row2.y, toggleWidth, row2.height);
            var syncRect = new Rect(saveRect.xMax + 4, row2.y, toggleWidth, row2.height);

            // "仅本地" 开关
            isLocalProp.boolValue = EditorGUI.ToggleLeft(localRect, "仅本地", isLocalProp.boolValue);

            // 如果仅本地选中，则禁用 保存 和 同步
            EditorGUI.BeginDisabledGroup(isLocalProp.boolValue);
            saveProp.boolValue = EditorGUI.ToggleLeft(saveRect, "保存", saveProp.boolValue);
            syncProp.boolValue = EditorGUI.ToggleLeft(syncRect, "同步", syncProp.boolValue);
            EditorGUI.EndDisabledGroup();
        }

        private void OnAddElement(ReorderableList list)
        {
            // 增加数组长度
            _listProp.arraySize++;
            _serializedObject.ApplyModifiedProperties();

            var newIndex = _listProp.arraySize - 1;
            var newElem = _listProp.GetArrayElementAtIndex(newIndex);

            // 如果之前已有元素，则参考上一个元素的默认值
            if (newIndex > 0)
            {
                var prevElem = _listProp.GetArrayElementAtIndex(newIndex - 1);
                var prevDefault = prevElem.FindPropertyRelative("defaultValue").floatValue;
                newElem.FindPropertyRelative("defaultValue").floatValue = prevDefault;
            }
            else
            {
                // 默认初始化为0
                newElem.FindPropertyRelative("defaultValue").floatValue = 0f;
            }

            // 默认不本地、不保存、不同步
            newElem.FindPropertyRelative("isLocal").boolValue = false;
            newElem.FindPropertyRelative("save").boolValue = false;
            newElem.FindPropertyRelative("sync").boolValue = false;

            _serializedObject.ApplyModifiedProperties();
        }
    }
}