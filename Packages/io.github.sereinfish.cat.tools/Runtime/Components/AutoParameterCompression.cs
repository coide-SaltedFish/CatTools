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
    /// 将列表中的参数按类型（Float/Int/Bool）自动分组，通过数据交换通道进行异步同步。
    /// 固定同步间隔 0.1s，每层最多 10 个参数（单层最大 1s 延迟）；
    /// Float 与 Int 不混合，Bool 可填充 Float/Int 层的空位。
    /// </summary>
    [AddComponentMenu("CatTools/AutoParameterCompression")]
    public class AutoParameterCompression : CatAvatarComponent
    {
        public override CatBuildPhase BuildPhase => CatBuildPhase.Optimizing;

        /// <summary>
        /// 需要异步同步的参数名称列表
        /// </summary>
        [Tooltip("需要异步同步的参数名称列表，按 Float/Int/Bool 自动分组处理")]
        public List<string> asyncSyncParameterNames = new();

        /// <summary>
        /// 同步信号变量名称（前缀，每个压缩层会自动追加 /{index} 后缀）
        /// </summary>
        [Tooltip("同步信号变量名称前缀，默认为空则自动生成；每个压缩层会追加 /{index} 后缀")]
        public string syncSignalParameterName;

        /// <summary>
        /// 数据交换变量名称（前缀，每个压缩层会自动追加 /{index} 后缀）
        /// </summary>
        [Tooltip("数据交换变量名称前缀，默认为空则自动生成；每个压缩层会追加 /{index} 后缀")]
        public string dataExchangeParameterName;
    }
}
