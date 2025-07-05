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
using CatTools.Runtime.entity;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace CatTools.Runtime
{
    
    /// <summary>
    /// 当满足参数条件时，批量替换自身及子级材质
    /// 添加调试窗口显示匹配到的材质
    ///
    /// 匹配方式：查找同目录下匹配名称的材质，采用正则匹配，{name} 填充为原本的材质名称
    /// </summary>
    [AddComponentMenu("CatTools/CT ParameterMatchMaterial")]
    public class ParameterMatchMaterial : CatAvatarComponent
    {
        [SerializeField]
        public VRCAvatarDescriptor.AnimLayerType layerType = VRCAvatarDescriptor.AnimLayerType.FX;
        
        [Header("条件列表")]
        public List<ParameterConditionsEntry> conditions = new();

        [SerializeField]
        public string nameRegex = "{name}"; // 正则表达式
    }
}