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

using System;
using UnityEngine;

namespace CatTools.editor.utils
{
    public static class HierarchyHelper
    {
        /// <summary>
        /// 从 root 开始深度优先遍历所有子对象，并对每个对象执行 action。
        /// </summary>
        public static void TraverseGameObject(GameObject root, Action<GameObject> action)
        {
            if (root == null || action == null) return;
        
            // 对当前对象做操作
            action(root);
        
            // 递归遍历所有子 Transform
            foreach (Transform child in root.transform)
            {
                TraverseGameObject(child.gameObject, action);
            }
        }
    }
}