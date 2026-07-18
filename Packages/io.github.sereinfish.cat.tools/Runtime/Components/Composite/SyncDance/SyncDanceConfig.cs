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
    /// 同步跳舞相关配置
    /// </summary>
    [System.Serializable]
    public class SyncDanceConfig
    {
        // 同步用的参数名称
        public SyncParameterName[] syncParameterNames;
        
        // 是否自动创建同步组件
        public bool autoCreateSyncComponent = true;
        
        // 同步半径 m
        public float syncRadius = 3f;
        
        
        // 手动调整同步组件位置偏移
        // 自动调整时自动计算位置放置在脚下
        public bool manualAdjustSyncComponentPositionOffset = false;
        
        // 同步组件位置偏移
        // 控制参数同步组件 及 速度同步参数同步组件
        public Vector3 syncPositionOffset = Vector3.zero; // 同步组件位置偏移
        
        [System.Serializable]
        public class SyncParameterName
        {
            public string name;
            // 单 bit 参数名称构建规则
            // 支持 {name} {index}
            public string bitParameterNameRule = "{name}{index}";
        
            // 后缀开始的值
            public int suffixStartValue = 0;
        }
    }
}