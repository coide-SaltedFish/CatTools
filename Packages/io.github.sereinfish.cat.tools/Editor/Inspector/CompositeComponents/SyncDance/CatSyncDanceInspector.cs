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
    [CustomEditor(typeof(CatSyncDance))]
    public class CatSyncDanceInspector : CatEditor
    {
        private bool _advancedFold = true; // 高级选项折叠开关
        private bool _parameterSetterFold = true; // 参数设置折叠开关
        private bool _stopParameterFold = true; // 停止参数折叠开关
        
        private SerializedProperty _controllerParameterNameProp;
        private SerializedProperty _autoCreateControllerParameterProp;
        private SerializedProperty _syncControllerParameterNameProp;
        private SerializedProperty _volumeParameterProp;
        private SerializedProperty _speedParameterProp;
        private SerializedProperty _autoRegisterOptionalParametersProp;
        private SerializedProperty _disableLocomotionLayerWhenDancingProp;
        private SerializedProperty _disableFxFaceLayerWhenDancingProp;
        private SerializedProperty _faceEmoCompatibleProp;
        private SyncDanceConfigProp _syncDanceConfigProp;
        private SerializedProperty _danceStartParameterSettersProp;
        private SerializedProperty _danceEndParameterSettersProp;
        private SerializedProperty _stopDanceParametersProp;
        private SerializedProperty _dancesProp;
        
        private CatSyncDanceParameterSetterList _startParameterSetterList;
        private CatSyncDanceParameterSetterList _endParameterSetterList;

        private ReorderableList _syncParameterList;
        private ReorderableList _stopParameterList;
        
        protected override void Init()
        {
            _controllerParameterNameProp = PropGet(nameof(CatSyncDance.controllerParameterName));
            _autoCreateControllerParameterProp = PropGet(nameof(CatSyncDance.autoCreateControllerParameter));
            _syncControllerParameterNameProp = PropGet(nameof(CatSyncDance.syncControllerParameterName));
            _volumeParameterProp = PropGet(nameof(CatSyncDance.volumeParameter));
            _speedParameterProp = PropGet(nameof(CatSyncDance.speedParameter));
            _autoRegisterOptionalParametersProp = PropGet(nameof(CatSyncDance.autoRegisterOptionalParameters));
            _disableLocomotionLayerWhenDancingProp = PropGet(nameof(CatSyncDance.disableLocomotionLayerWhenDancing));
            _disableFxFaceLayerWhenDancingProp = PropGet(nameof(CatSyncDance.disableFxFaceLayerWhenDancing));
            _faceEmoCompatibleProp = PropGet(nameof(CatSyncDance.faceEmoCompatible));
            _syncDanceConfigProp = new SyncDanceConfigProp(PropGet(nameof(CatSyncDance.syncDanceConfig)));
            _danceStartParameterSettersProp = PropGet(nameof(CatSyncDance.danceStartParameterSetters));
            _danceEndParameterSettersProp = PropGet(nameof(CatSyncDance.danceEndParameterSetters));
            _stopDanceParametersProp = PropGet(nameof(CatSyncDance.stopDanceParameters));
            _dancesProp = PropGet(nameof(CatSyncDance.dances));
            
            _startParameterSetterList = new CatSyncDanceParameterSetterList("开始时参数设置", _danceStartParameterSettersProp);
            _endParameterSetterList = new CatSyncDanceParameterSetterList("结束时参数设置", _danceEndParameterSettersProp);
            
            _syncParameterList = new ReorderableList(serializedObject, _syncDanceConfigProp.syncParameterNamesProp, true, true, true, true)
            {
                drawHeaderCallback = rect => GUI.Label(rect, "同步参数"),
                elementHeightCallback = _ => (EditorGUIUtility.singleLineHeight + 2f) * 3 + EditorGUIUtility.standardVerticalSpacing * 2,
                drawElementCallback = (rect, index, _, _) =>
                {
                    var lineH = EditorGUIUtility.singleLineHeight + 2f;
                    var spacing = EditorGUIUtility.standardVerticalSpacing;
                    var x = rect.x;
                    var y = rect.y;
                    var syncParameterNameProp = _syncDanceConfigProp.syncParameterNamesProp.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(new Rect(x, y, rect.width,lineH), syncParameterNameProp.FindPropertyRelative("name"), new GUIContent("参数名"));
                    y += lineH + spacing;
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), syncParameterNameProp.FindPropertyRelative("bitParameterNameRule"), new GUIContent("参数构建规则"));
                    y += lineH + spacing;
                    EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), syncParameterNameProp.FindPropertyRelative("suffixStartValue"), new GUIContent("后缀起始值"));
                }
            };
            _stopParameterList = new ReorderableList(serializedObject, _stopDanceParametersProp, true, true, true, true)
            {
                drawHeaderCallback = rect => GUI.Label(rect, "停止参数"),
                elementHeightCallback = _ => (EditorGUIUtility.singleLineHeight + 2f) * 2,
                drawElementCallback = (rect, index, _, _) =>
                {
                    var parameterName = _stopDanceParametersProp.GetArrayElementAtIndex(index).FindPropertyRelative("parameterName");
                    var parameterValue = _stopDanceParametersProp.GetArrayElementAtIndex(index).FindPropertyRelative("value");
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), parameterName);
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 2f, rect.width, EditorGUIUtility.singleLineHeight), parameterValue);
                }
            };
        }
        
            protected override void OnDraw()
            {
                EditorGUILayout.PropertyField(_controllerParameterNameProp, new GUIContent("控制参数", tooltip:"本地播放控制参数"));
                EditorGUILayout.PropertyField(_autoCreateControllerParameterProp, new GUIContent("自动创建控制参数", tooltip:"是否自动创建控制参数，默认自动使用动态Int参数进行压缩"));
                EditorGUILayout.PropertyField(_syncControllerParameterNameProp, new GUIContent("同步开关参数",tooltip:"Bool类型，用于控制同步开关，可选"));
                EditorGUILayout.PropertyField(_volumeParameterProp, new GUIContent("音量参数", tooltip:"Float类型，用于控制音量的参数，可选"));
                EditorGUILayout.PropertyField(_speedParameterProp, new GUIContent("速度参数", tooltip:"Float类型，用于控制速度的参数，可选"));
                EditorGUILayout.PropertyField(_autoRegisterOptionalParametersProp, new GUIContent("自动注册可选参数", tooltip:"是否自动注册可选参数"));
                EditorGUILayout.PropertyField(_disableLocomotionLayerWhenDancingProp, new GUIContent("跳舞时禁用 locomotion 层", tooltip:"启用后会在NDFM框架构建的优化阶段对 Base 层所有层进行处理，在跳舞时跳转到 Empty State"));
                EditorGUILayout.PropertyField(_disableFxFaceLayerWhenDancingProp, new GUIContent("跳舞时禁用FX表情层", tooltip:"启用后在跳舞时自动将FX层1、2层权重设置为0"));
                EditorGUILayout.PropertyField(_faceEmoCompatibleProp, new GUIContent("FaceEmo插件表情兼容", tooltip:"是否兼容FaceEmo插件表情，启用后在跳舞时将FaceEmo状态设置为PASS"));
                
                _parameterSetterFold = EditorGUILayout.Foldout(_parameterSetterFold, "参数设置");
                if (_parameterSetterFold)
                {
                    _syncParameterList.DoLayoutList();
                    _startParameterSetterList.DoLayoutList();
                    _endParameterSetterList.DoLayoutList();
                }
                _stopParameterFold = EditorGUILayout.Foldout(_stopParameterFold, "停止参数");
                if (_stopParameterFold)
                {
                    _stopParameterList.DoLayoutList();
                }
                
                _advancedFold = EditorGUILayout.Foldout(_advancedFold, "高级选项");
                if (_advancedFold)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.PropertyField(_syncDanceConfigProp.autoCreateSyncComponentProp, new GUIContent("自动创建同步组件", tooltip:"非必要请勿关闭"));
                    EditorGUILayout.PropertyField(_syncDanceConfigProp.syncRadiusProp, new GUIContent("同步半径"));
                    EditorGUILayout.PropertyField(_syncDanceConfigProp.manualAdjustSyncComponentPositionOffsetProp, new GUIContent("手动调整同步组件位置偏移"));
                    EditorGUILayout.PropertyField(_syncDanceConfigProp.syncPositionOffsetProp, new GUIContent("同步位置偏移"));
                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("舞蹈列表"))
                {
                    CatSyncDanceDanceEditWindows.ShowWindow(serializedObject);
                }
            }
        
        private class SyncDanceConfigProp
        {
            public SerializedProperty syncParameterNamesProp;
            public SerializedProperty autoCreateSyncComponentProp;
            public SerializedProperty syncRadiusProp;
            public SerializedProperty manualAdjustSyncComponentPositionOffsetProp;
            public SerializedProperty syncPositionOffsetProp; // Vector3

            public SyncDanceConfigProp(SerializedProperty syncDanceConfig)
            {
                syncParameterNamesProp = syncDanceConfig.FindPropertyRelative("syncParameterNames");
                autoCreateSyncComponentProp = syncDanceConfig.FindPropertyRelative("autoCreateSyncComponent");
                syncRadiusProp = syncDanceConfig.FindPropertyRelative("syncRadius");
                manualAdjustSyncComponentPositionOffsetProp = syncDanceConfig.FindPropertyRelative("manualAdjustSyncComponentPositionOffset");
                syncPositionOffsetProp = syncDanceConfig.FindPropertyRelative("syncPositionOffset");
            }
        }
    }
}