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

namespace io.github.sereinfish.cat.tools.Components
{
    [AddComponentMenu("CatTools/ConditionalBlendShapeSetter")]
    public class ConditionalBlendShapeSetter : ConditionalBehaviour
    {
        public GameObject[] targets; // 保留，一点用没有
        public ShapeChangeInfo[] shapeChangeInfos;
        public bool restoreToggle = false; // 当条件不成立时，强制复原为默认值
        
        [Serializable]
        public enum ShapeChangeType
        {
            // Delete,
            Set
        }
        
        [Serializable]
        public struct ShapeChangeInfo
        {
            public Transform target;
            public string shapeName;
            public ShapeChangeType changeType;
            public float value;
        }
    }
}