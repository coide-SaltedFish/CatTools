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
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(AutoParameterCompression))]
    public class AutoParameterCompressionInspector : CatEditor
    {
        private SerializedProperty _syncIntervalProp;
        private SerializedProperty _asyncSyncParameterNamesProp;
        private SerializedProperty _syncSignalParameterNameProp;
        private SerializedProperty _dataExchangeParameterNameProp;
        private ReorderableList _parameterNamesList;
        private bool _showOther;

        private static GUIStyle _yellowWarningStyle;
        private static GUIStyle _redWarningStyle;

        protected override void Init()
        {
            _syncIntervalProp = PropGet(nameof(AutoParameterCompression.syncInterval));
            _asyncSyncParameterNamesProp = PropGet(nameof(AutoParameterCompression.asyncSyncParameterNames));
            _syncSignalParameterNameProp = PropGet(nameof(AutoParameterCompression.syncSignalParameterName));
            _dataExchangeParameterNameProp = PropGet(nameof(AutoParameterCompression.dataExchangeParameterName));
            
            _parameterNamesList = new ReorderableList(serializedObject, _asyncSyncParameterNamesProp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "异步同步参数名称"),
                drawElementCallback = DrawParameterNameElement,
                elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 2f,
                onAddCallback = AddParameterName
            };
        }

        protected override void OnDraw()
        {
            EditorGUILayout.HelpBox("自动类型参数压缩：自动判断参数类型，并按同步间隔进行异步同步。", MessageType.Info);
            DrawSyncInterval();
            _parameterNamesList.DoLayoutList();
            DrawTotalSyncDuration();

            _showOther = EditorGUILayout.Foldout(_showOther, "其他", true);
            if (_showOther)
            {
                EditorGUILayout.PropertyField(_syncSignalParameterNameProp, new GUIContent("同步信号变量名称"));
                EditorGUILayout.PropertyField(_dataExchangeParameterNameProp, new GUIContent("数据交换变量名称"));
            }
        }

        /// <summary>
        /// 绘制同步间隔字段，并带时长校验提示
        /// </summary>
        private void DrawSyncInterval()
        {
            EditorGUILayout.PropertyField(_syncIntervalProp, new GUIContent("同步间隔（秒）"));

            const float minInterval = 0.05f;
            // 小于最小值的输入进行纠正
            if (_syncIntervalProp.floatValue < minInterval)
            {
                _syncIntervalProp.floatValue = minInterval;
            }

            var interval = _syncIntervalProp.floatValue;
            if (interval > 1f)
            {
                EditorGUILayout.LabelField("同步间隔大于1s可能会导致参数更新过慢", GetWarningStyle(ref _yellowWarningStyle, Color.yellow));
            }
            else if (interval < 0.1f)
            {
                EditorGUILayout.LabelField("同步小于0.1s可能会导致参数同步不稳定", GetWarningStyle(ref _redWarningStyle, Color.red));
            }
        }

        /// <summary>
        /// 计算并提示总同步时长（同步间隔 × 异步同步参数数量）
        /// </summary>
        private void DrawTotalSyncDuration()
        {
            var totalSyncDuration = _syncIntervalProp.floatValue * _asyncSyncParameterNamesProp.arraySize;
            if (totalSyncDuration > 1f)
            {
                EditorGUILayout.LabelField($"总当前总同步时长为{totalSyncDuration}，参数同步延迟较大", GetWarningStyle(ref _yellowWarningStyle, Color.yellow));
            }
        }

        /// <summary>
        /// 获取提示文字样式（黄色/红色小字）
        /// </summary>
        private static GUIStyle GetWarningStyle(ref GUIStyle cache, Color color)
        {
            if (cache == null)
            {
                cache = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    wordWrap = true
                };
                cache.normal.textColor = color;
            }
            return cache;
        }

        private void DrawParameterNameElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _asyncSyncParameterNamesProp.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element, GUIContent.none);
        }

        private void AddParameterName(ReorderableList list)
        {
            var prop = list.serializedProperty;
            prop.InsertArrayElementAtIndex(prop.arraySize);
            prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = "New Parameter";
            list.index = prop.arraySize - 1;
        }
    }
}
