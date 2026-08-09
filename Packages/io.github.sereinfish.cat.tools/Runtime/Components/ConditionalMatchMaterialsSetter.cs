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
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.Components
{
    [AddComponentMenu("CatTools/ConditionalMatchMaterialsSetter")]
    public class ConditionalMatchMaterialsSetter : ConditionalBehaviour
    {
        public bool includeChildren = true; // 是否包含子级
        public string[] targetPath; // 目标路径
        public string matchExpression = "{name}"; // 匹配表达式
        public string ignoreString; // 忽略指定字符串（设置的字符串部分不参与匹配）
        [SerializeReference]
        public IMaterialHandler materialHandler; // 全局的材质处理接口

        public List<AutoHandleMaterial> autoHandleMaterials; // 需要使用处理脚本自动处理的材质
        
        [System.Serializable]
        public class AutoHandleMaterial
        {
            public string materialPath; // 材质路径
            // public string objectPath; // 材质所在对象的层级路径
            [SerializeReference]
            public IMaterialHandler materialHandler; // 单独材质处理接口，可为空
        }
        
        /// <summary>
        /// 材质处理接口
        /// </summary>
        public interface IMaterialHandler
        {
            public string HandlerName { get; } // 处理接口名称
            
            /// <summary>
            /// 处理材质
            /// </summary>
            public Material HandleMaterial(Material input);
        }
        
        /// <summary>
        /// 扫描所有实现了材质处理的类
        /// </summary>
        /// <returns></returns>
        public static IMaterialHandler[] GetMaterialHandlers()
        {
            var handlerType = typeof(IMaterialHandler);
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException e) { return e.Types.Where(x => x != null); }
                })
                .Where(t => 
                    handlerType.IsAssignableFrom(t) &&
                    !t.IsInterface && !t.IsAbstract
                )
                .Select(t => (IMaterialHandler)Activator.CreateInstance(t)!)
                .ToArray();
        }
    }
}