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
using io.github.sereinfish.cat.tools.editor.plugin;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.Components
{
    /// <summary>
    /// 自动类型参数压缩组件
    /// 自动将列表中的参数进行类型压缩（如将多个布尔参数按位打包为 Int 参数），
    /// 并在 Optimizing 阶段按照同步间隔执行异步同步。
    /// </summary>
    [AddComponentMenu("CatTools/AutoParameterCompression")]
    public class AutoParameterCompression : CatAvatarComponent
    {
        public override CatBuildPhase BuildPhase => CatBuildPhase.Optimizing;
        
        /// <summary>
        /// 同步间隔（秒）
        /// </summary>
        [Tooltip("同步间隔（秒），每隔该时间执行一次参数的异步同步，最小 0.05 秒")]
        public float syncInterval = 0.1f;
        
        /// <summary>
        /// 需要异步同步的参数名称列表
        /// </summary>
        [Tooltip("需要异步同步的参数名称列表")]
        public List<string> asyncSyncParameterNames = new List<string>();

        /// <summary>
        /// 同步信号变量名称
        /// </summary>
        [Tooltip("用于同步信号的变量名称，默认为空")]
        public string syncSignalParameterName;

        /// <summary>
        /// 数据交换变量名称
        /// </summary>
        [Tooltip("用于数据交换的变量名称，默认为空")]
        public string dataExchangeParameterName;
    }
}
