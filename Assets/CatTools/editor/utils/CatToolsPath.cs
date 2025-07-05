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
using System.Collections.Generic;
using UnityEngine;

namespace CatTools.editor.utils
{
    public static class CatToolsPath
    {
        /// <summary>
        /// 获取从 root 到 target 的 Transform 路径（各级名称以 '/' 分隔）。
        /// 如果 target == root，返回空字符串（表示根自身）。
        /// 如果 target 不是 root 的子级（或同级更上层），抛出异常。
        /// </summary>
        public static string GetRelativePath(Transform root, Transform target)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (target == null) throw new ArgumentNullException(nameof(target));
        
            // 如果就是同一个 Transform，则路径为空
            if (root == target)
                return string.Empty;
        
            // 从 target 向上收集名称
            var names = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }
        
            // 如果走到了顶层还没遇到 root，说明不是子集
            if (current != root)
                throw new ArgumentException($"Transform '{target.name}' is not a child of '{root.name}'.");
        
            // 反转顺序：root 下的第一层子先出现在前面
            names.Reverse();
        
            // 拼接成 "child/grandchild/..." 形式
            return string.Join("/", names);
        }
    }
}