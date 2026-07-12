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
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    public abstract class CatEditor : Editor
    {
        public Dictionary<string, SerializedProperty> Props { get; } = new();
        
        public SerializedProperty PropGet(string pName)
        {
            return PropGet(serializedObject, pName);
        }
        
        public SerializedProperty PropGet(SerializedObject so, string pName)
        {
            if (Props.TryGetValue(pName, out var get)) return get;
            
            return Props[pName] = so.FindProperty(pName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
                
            OnDraw();
            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            Init();
        }
        
        private GameObject _avatarRoot;
        protected GameObject GetAvatarRoot<T>() where T : Component
        {
            _avatarRoot ??= ((T)target).gameObject.GetAvatarRoot();
            return _avatarRoot;
        }

        protected abstract void OnDraw();
        protected abstract void Init();
    }
}