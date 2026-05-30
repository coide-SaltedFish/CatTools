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
    public class ConditionalAnimation : ConditionalBehaviour
    {
        public Transform[] targets;
        public MergeAnimation[] animations; // 合并动画
        
        [Serializable]
        public class MergeAnimation
        {
            public AnimationClip clip; // TODO 添加使用内部预制动画
            public MergeType mergeType = MergeType.Overlay;
            public bool overrideTargetPath = true; // 是否重写目标路径
            public bool applyToChildren; // 应用到所有子级
        }
        
        /// <summary>
        /// 动画合并类型
        /// </summary>
        public enum MergeType
        {
            /// <summary>
            /// 直接合并
            /// 当前动画帧保持不变，与目标动画逐帧叠加
            /// 优先级最后
            /// </summary>
            Overlay,

            /// <summary>
            /// 按顺序追加
            /// 优先级其次
            /// </summary>
            Append,

            /// <summary>
            /// 插入到目标动画最开始
            /// 优先级最高
            /// </summary>
            Prepend,

            /// <summary>
            /// 合并到目标动画最后一帧
            /// 优先级第三
            /// </summary>
            MergeToEnd
        }
    }
}