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

namespace CatTools.Runtime.entity
{
    [Serializable]
    public class ParametersEntry
    {
        public string name; // 参数名称
        public AnimatorControllerParameterType type; // 参数类型
        public float defaultValue; // 参数默认值，可为 Null，为 Null 则不写入或写入 0
        public bool isLocal; // 是否是本地参数，如果是本地参数，则不会被注册到Expression Parameters
        public bool save; // 是否保存
        public bool sync; // 是否同步到VRChat
    }
}