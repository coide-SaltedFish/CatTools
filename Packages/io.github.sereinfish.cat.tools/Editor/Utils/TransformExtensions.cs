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
using VRC.SDK3.Avatars.Components;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class TransformExtensions
    {
        /// <summary>
        /// 按层级（从上到下）遍历 transform 树。
        /// 深度更小的节点会先被 yield 返回。
        /// </summary>
        public static IEnumerable<Transform> TraverseByHierarchy(this Transform root)
        {
            if (root == null) yield break;

            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                yield return current;

                // 按照 inspector 面板中子物体的顺序入队
                foreach (Transform child in current)
                    queue.Enqueue(child);
            }
        }

        /// <summary>
        /// 按层级遍历 transform 树
        /// 按顺序获取指定的组件列表（在同一对象时顶层的组件优先）
        /// </summary>
        /// <param name="root"></param>
        /// <param name="includeInactive"></param>
        /// <returns></returns>
        public static T[] GetComponentsInChildrenTraverseByHierarchy<T>(this Transform root, bool includeInactive = false)
        {
            if (root == null)
                return System.Array.Empty<T>();

            var results = new List<T>();

            foreach (var t in root.TraverseByHierarchy())
            {
                // 如果不包含 inactive，则跳过不活跃的节点
                if (!includeInactive && !t.gameObject.activeInHierarchy)
                    continue;

                // 获取当前节点上的所有 T 组件（顺序与 Inspector 中组件顺序一致）
                var comps = t.GetComponents<T>();
                if (comps != null && comps.Length > 0)
                    results.AddRange(comps);
            }

            return results.ToArray();
        }
        
        public static Transform[] GetChildrenIterative(this Transform root)
        {
            if (root == null) return Array.Empty<Transform>();

            var stack = new Stack<Transform>();
            var results = new List<Transform> { root };

            foreach (Transform child in root)
                stack.Push(child);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                results.Add(node);
                foreach (Transform child in node)
                    stack.Push(child);
            }

            return results.ToArray();
        }
        
        public static GameObject GetAvatarRoot(this GameObject child)
        {
            if (child == null) return null;
            var parent = child;
            while (parent != null)
            {
                if (parent.GetComponent<VRCAvatarDescriptor>() != null)
                    return parent;
                parent = parent.transform.parent?.gameObject;
            }

            return null;
        }
        
        public static Transform GetAvatarRoot(this Transform child)
        {
            if (child == null) return null;
            var parent = child;
            while (parent != null)
            {
                if (parent.GetComponent<VRCAvatarDescriptor>() != null)
                    return parent;
                parent = parent.parent;
            }

            return null;
        }
    }
}