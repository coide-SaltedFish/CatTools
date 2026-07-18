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

using UnityEngine;

namespace io.github.sereinfish.cat.tools.Components
{
    /// <summary>
    /// 舞蹈数据
    /// </summary>
    [System.Serializable]
    public class CatSyncDanceEntry
    {
        public string danceName; // 舞蹈名称
        public PathType pathType = PathType.Absolute; // 动画合并类型
        public AnimationClip[] clip; // 舞蹈动画 依次播放
        public AnimationClip loopClip = null; // 循环舞蹈动画
        public AudioClip musicClip; // 舞蹈音频
        public bool loop = true; // 是否循环播放
        
        public DanceParameter[] danceParameters; // 舞蹈播放的条件
        
        public enum PathType
        {
            // 相对路径
            Relative,
            // 绝对路径
            Absolute
        }
        
        /// <summary>
        /// 舞蹈播放的条件
        /// </summary>
        [System.Serializable]
        public class DanceParameter
        {
            public string parameterName;
            public float value; // 当参数达到这个值时播放，不可为 0
        }
    }
}