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
using io.github.sereinfish.cat.tools.editor.inspector.window;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    [CustomEditor(typeof(ConditionalMatchMaterialsSetter))]
    public class ConditionalMatchMaterialsSetterInspector : ConditionalEditor<ConditionalMatchMaterialsSetter>
    {
        private SerializedProperty _includeChildrenProp;
        private SerializedProperty _targetPathProp;
        private SerializedProperty _matchExpressionProp;

        protected override void Init()
        {
            base.Init();

            _includeChildrenProp = PropGet(nameof(ConditionalMatchMaterialsSetter.includeChildren));
            _targetPathProp = PropGet(nameof(ConditionalMatchMaterialsSetter.targetPath));
            _matchExpressionProp = PropGet(nameof(ConditionalMatchMaterialsSetter.matchExpression));
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            
            EditorGUILayout.PropertyField(_includeChildrenProp);
            EditorGUILayout.PropertyField(_targetPathProp);
            EditorGUILayout.PropertyField(_matchExpressionProp);
            
            if (GUILayout.Button("打开调试窗口"))
            {
                ConditionalMatchMaterialsSetterDebugWindows.ShowWindow(target as ConditionalMatchMaterialsSetter);
            }
        }
    }
}