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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using io.github.sereinfish.cat.tools.Components;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class MaterialPathHelper
    {
        /// <summary>
        /// 根据 ConditionalMatchMaterialsSetter 的设置，找到目标材质
        /// 支持占位符 {name}
        /// </summary>
        /// <param name="setter"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Material[] FindTargetsMaterial(this ConditionalMatchMaterialsSetter setter, Material source)
        {
            if (source == null || setter == null) return Array.Empty<Material>();
            
            var result = new List<Material>();
            
            // 遍历目标文件夹
            foreach (var tp in setter.targetPath)
            {
                if (string.IsNullOrEmpty(tp)) continue;
                var path = tp.Replace("\\", "/");
                // 路径拼接
                if (path.EndsWith("/").Not()) path += "/";
                // 构建材质名称
                var materialName = ReplacePlaceholder(
                    setter.matchExpression,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {{"name", source.name}});
                // 构建正则表达式
                var pattern = "^" + materialName + "$";
                var options = RegexOptions.Compiled;
                
                
                var guids = AssetDatabase.FindAssets($"t:Material", new[] { path });
                Debug.Log($"查找目录 {path} 下文件 {materialName} 从 {path}");
                foreach (var guid in guids)
                {
                    var matPath = AssetDatabase.GUIDToAssetPath(guid);
                    var matName = Path.GetFileNameWithoutExtension(matPath);

                    if (Regex.IsMatch(matName, pattern, options))
                    {
                        Debug.Log($"由 {path} 文件夹下找到目标材质 {materialName} 从 {pattern}");
                        result.Add(AssetDatabase.LoadAssetAtPath<Material>(matPath));
                    }
                }
            }

            return result.ToArray();
        }
        
        public static string ReplacePlaceholder(string matchExpression, Dictionary<string, string> context)
        {
            return context == null ? matchExpression : context.Aggregate(matchExpression, (current, keyValuePair) => current.Replace("{" + keyValuePair.Key + "}", keyValuePair.Value));
        }
        
        public static HashSet<Material> FindChildMaterials(
            this GameObject go,
            bool includeInactive = true)
        {
            if (go == null)
                return new HashSet<Material>();

            return go.GetComponentsInChildren<Renderer>(includeInactive)
                .SelectMany(r => r.sharedMaterials)
                .Where(m => m != null)
                .ToHashSet();
        }
    }
}