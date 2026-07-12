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
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(LightController))]
    public class LightControllerInspector : CatEditor
    {
        private SerializedProperty _controllerParameterName;
        private SerializedProperty _includeChildren;
        
        protected override void Init()
        {
            _controllerParameterName = PropGet(nameof(LightController.controllerParameterName));
            _includeChildren = PropGet(nameof(LightController.includeChildren));
        }
        
        protected override void OnDraw()
        {
            EditorGUILayout.PropertyField(_controllerParameterName, new GUIContent("控制参数名称"));
            EditorGUILayout.HelpBox("控制参数（Float）需要手动注册，该组件不对参数槽进行操作", MessageType.Info);
            EditorGUILayout.PropertyField(_includeChildren, new GUIContent("包含子级"));
        }
    }
}