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

using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.inspector.ui;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(AnimatorLayerLock))]
    public class AnimatorLayerLockInspector : CatEditor
    {
        // 枚举中文显示名称，后续可在此扩展
        private static readonly string[] LockScopeDisplayNames =
        {
            "整个控制器",
            "指定图层"
        };

        private static readonly string[] LockOperationDisplayNames =
        {
            "锁定到当前默认状态",
            "新建空状态并锁定"
        };

        private static Vector2? _lockScopeLabelSize;
        private static Vector2? _lockOperationLabelSize;

        private SerializedProperty _layerLockEntriesProp;
        private ParameterConditionList<ConditionalBehaviour> _parameterConditionList;
        private ReorderableList _layerLockEntriesList;

        protected override void Init()
        {
            _layerLockEntriesProp = PropGet(nameof(AnimatorLayerLock.layerLockEntries));
            _parameterConditionList = new ParameterConditionList<ConditionalBehaviour>(serializedObject);

            _layerLockEntriesList = new ReorderableList(serializedObject, _layerLockEntriesProp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "图层锁定配置"),
                drawElementCallback = DrawLayerLockEntry,
                elementHeightCallback = GetLayerLockEntryHeight,
                onAddCallback = AddLayerLockEntry
            };
        }

        protected override void OnDraw()
        {
            EditorGUILayout.HelpBox("根据条件对目标 Animator 控制器图层进行锁定处理。", MessageType.Info);
            _parameterConditionList.DoLayout();
            _layerLockEntriesList.DoLayoutList();
        }

        private float GetLayerLockEntryHeight(int index)
        {
            var lineH = EditorGUIUtility.singleLineHeight + 2f;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var entryProp = _layerLockEntriesProp.GetArrayElementAtIndex(index);
            var lockScopeProp = entryProp.FindPropertyRelative(nameof(AnimatorLayerLockEntry.lockScope));
            var scope = (AnimatorLayerLockScope)lockScopeProp.enumValueIndex;

            // 第 1 行：控制器类型；第 2 行：锁定范围 + 锁定操作
            var lines = 2;
            if (scope == AnimatorLayerLockScope.SpecificLayer)
                lines++;

            return lines * lineH + (lines - 1) * spacing;
        }

        private void DrawLayerLockEntry(Rect rect, int index, bool isActive, bool isFocused)
        {
            _lockScopeLabelSize ??= GUI.skin.label.CalcSize(new GUIContent("锁定范围："));
            _lockOperationLabelSize ??= GUI.skin.label.CalcSize(new GUIContent("锁定操作："));

            var lineH = EditorGUIUtility.singleLineHeight + 2f;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var entryProp = _layerLockEntriesProp.GetArrayElementAtIndex(index);

            var animLayerTypeProp = entryProp.FindPropertyRelative(nameof(AnimatorLayerLockEntry.animLayerType));
            var lockScopeProp = entryProp.FindPropertyRelative(nameof(AnimatorLayerLockEntry.lockScope));
            var layerNameProp = entryProp.FindPropertyRelative(nameof(AnimatorLayerLockEntry.layerName));
            var lockOperationProp = entryProp.FindPropertyRelative(nameof(AnimatorLayerLockEntry.lockOperation));

            var scope = (AnimatorLayerLockScope)lockScopeProp.enumValueIndex;
            var y = rect.y;
            var halfW = rect.width * 0.5f - spacing * 0.5f;

            // 第 1 行：控制器类型
            EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineH), animLayerTypeProp, new GUIContent("控制器类型"));
            y += lineH + spacing;

            // 第 2 行：锁定范围（50%） + 锁定操作（50%）
            var x = rect.x;
            var scopeLabelW = _lockScopeLabelSize?.x ?? 50f;
            EditorGUI.LabelField(new Rect(x, y, scopeLabelW, lineH), "锁定范围：");
            x += scopeLabelW;
            lockScopeProp.enumValueIndex = EditorGUI.Popup(
                new Rect(x, y, halfW - scopeLabelW, lineH),
                lockScopeProp.enumValueIndex,
                LockScopeDisplayNames);
            x = rect.x + halfW + spacing;

            var operationLabelW = _lockOperationLabelSize?.x ?? 50f;
            EditorGUI.LabelField(new Rect(x, y, operationLabelW, lineH), "锁定操作：");
            x += operationLabelW;
            lockOperationProp.enumValueIndex = EditorGUI.Popup(
                new Rect(x, y, halfW - operationLabelW, lineH),
                lockOperationProp.enumValueIndex,
                LockOperationDisplayNames);

            y += lineH + spacing;

            // 第 3 行（条件显示）：图层名称
            if (scope == AnimatorLayerLockScope.SpecificLayer)
            {
                EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineH), layerNameProp, new GUIContent("图层名称"));
            }
        }

        private void AddLayerLockEntry(ReorderableList list)
        {
            var prop = list.serializedProperty;
            prop.InsertArrayElementAtIndex(prop.arraySize);
            var newEntry = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            newEntry.FindPropertyRelative(nameof(AnimatorLayerLockEntry.animLayerType)).enumValueIndex = 0;
            newEntry.FindPropertyRelative(nameof(AnimatorLayerLockEntry.lockScope)).enumValueIndex = 0;
            newEntry.FindPropertyRelative(nameof(AnimatorLayerLockEntry.layerName)).stringValue = "";
            newEntry.FindPropertyRelative(nameof(AnimatorLayerLockEntry.lockOperation)).enumValueIndex = 0;
        }
    }
}
