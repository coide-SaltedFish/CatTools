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
    [AddComponentMenu("CatTools/CT ObjectParameterToggle")]
    public class ParameterToggle : CatAvatarComponent
    {
        [SerializeField]
        public VRCAvatarDescriptor.AnimLayerType layerType = VRCAvatarDescriptor.AnimLayerType.FX;
        
        [SerializeField]
        public bool toggle = true;
        
        [Header("条件列表")]
        public List<ParameterConditionsEntry> conditions = new();
        
        // 添加动画构建器
        // [SerializeField]
        // public AnimationBuilderType animationBuilderType;
        //
        // [HideInInspector]
        // public IAnimationBuilder AnimationBuilder = null;
    }
}