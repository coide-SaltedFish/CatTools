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
using io.github.sereinfish.cat.tools.editor.handler;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(AutoParameterCompression))]
    public class AutoParameterCompressionInspector : CatEditor
    {
        private SerializedProperty _asyncSyncParameterNamesProp;
        private SerializedProperty _syncSignalParameterNameProp;
        private SerializedProperty _dataExchangeParameterNameProp;
        private ReorderableList _parameterNamesList;
        private bool _showOther;

        private static GUIStyle _yellowWarningStyle;
        private static GUIStyle _redWarningStyle;

        protected override void Init()
        {
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
            EditorGUILayout.HelpBox(
                "自动类型参数压缩：按参数类型（Float/Int/Bool）自动分组，同步间隔 0.1s，每层最多 10 个参数（单层最大 1s 延迟）",
                MessageType.Info);
            _parameterNamesList.DoLayoutList();
            DrawGroupingPreview();

            _showOther = EditorGUILayout.Foldout(_showOther, "其他", true);
            if (_showOther)
            {
                EditorGUILayout.PropertyField(_syncSignalParameterNameProp, new GUIContent("同步信号变量名称前缀"));
                EditorGUILayout.PropertyField(_dataExchangeParameterNameProp, new GUIContent("数据交换变量名称前缀"));
                EditorGUILayout.LabelField("每个压缩层会为上述前缀自动追加 /{index} 后缀。", GetWarningStyle(ref _yellowWarningStyle, Color.gray));
            }
        }

        /// <summary>
        /// 读取当前 Avatar 的 ExpressionParameters，按 Float/Int/Bool 分类配置的参数并预览分组结果。
        /// </summary>
        private void DrawGroupingPreview()
        {
            var floatNames = new List<string>();
            var intNames = new List<string>();
            var boolNames = new List<string>();
            var missingNames = new List<string>();
            var seen = new HashSet<string>();

            var typeByName = GetParameterTypeMap();
            for (var i = 0; i < _asyncSyncParameterNamesProp.arraySize; i++)
            {
                var name = _asyncSyncParameterNamesProp.GetArrayElementAtIndex(i).stringValue;
                if (string.IsNullOrEmpty(name)) continue;
                if (!seen.Add(name)) continue;

                if (typeByName == null || !typeByName.TryGetValue(name, out var type))
                {
                    missingNames.Add(name);
                    continue;
                }

                switch (type)
                {
                    case VRCExpressionParameters.ValueType.Float:
                        floatNames.Add(name);
                        break;
                    case VRCExpressionParameters.ValueType.Int:
                        intNames.Add(name);
                        break;
                    default:
                        boolNames.Add(name);
                        break;
                }
            }

            if (typeByName == null)
            {
                EditorGUILayout.LabelField("未找到 ExpressionParameters，无法预览参数分组。", GetWarningStyle(ref _yellowWarningStyle, Color.yellow));
                return;
            }

            var groups = AutoParameterCompressionBuilder.GroupParameters(floatNames, intNames, boolNames);
            var builtCount = 0;
            var abandonedCount = 0;
            var abandonedNames = new List<string>();
            var maxDelay = 0f;
            foreach (var group in groups)
            {
                if (group.ShouldAbandon)
                {
                    abandonedCount++;
                    abandonedNames.AddRange(group.ParameterNames);
                    continue;
                }
                builtCount++;
                maxDelay = Mathf.Max(maxDelay, group.Count * AutoParameterCompressionBuilder.SyncIntervalSeconds);
            }

            EditorGUILayout.LabelField(
                $"Float {floatNames.Count} 个 / Int {intNames.Count} 个 / Bool {boolNames.Count} 个 → 共 {groups.Count} 层（构建 {builtCount} 层、放弃 {abandonedCount} 层）" +
                $"\n单层最大同步延迟 {maxDelay:F1}s（每层最多 {AutoParameterCompressionBuilder.MaxParametersPerLayer} 个参数）");

            if (abandonedCount > 0)
            {
                EditorGUILayout.LabelField($"以下参数因所在层过小（≤3 个参数）将被放弃、保持默认同步：{string.Join(", ", abandonedNames)}",
                    GetWarningStyle(ref _yellowWarningStyle, Color.yellow));
            }

            if (missingNames.Count > 0)
            {
                EditorGUILayout.LabelField($"以下 {missingNames.Count} 个参数未在 ExpressionParameters 中找到：{string.Join(", ", missingNames)}",
                    GetWarningStyle(ref _redWarningStyle, Color.red));
            }
        }

        private Dictionary<string, VRCExpressionParameters.ValueType> GetParameterTypeMap()
        {
            var avatarRoot = GetAvatarRoot<AutoParameterCompression>();
            var descriptor = avatarRoot?.GetComponent<VRCAvatarDescriptor>();
            var expressionParameters = descriptor?.expressionParameters;
            if (expressionParameters == null || expressionParameters.parameters == null) return null;

            var map = new Dictionary<string, VRCExpressionParameters.ValueType>();
            foreach (var parameter in expressionParameters.parameters)
            {
                if (!string.IsNullOrEmpty(parameter.name)) map[parameter.name] = parameter.valueType;
            }
            return map;
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
