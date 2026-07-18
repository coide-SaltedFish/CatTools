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
using UnityEngine;

namespace io.github.sereinfish.cat.tools.Components
{
    [AddComponentMenu("CatTools/集成组件/CatSyncDance")]
    public class CatSyncDance : CatAvatarComponent
    {
        // 控制参数名称，用于控制跳舞 Int
        // 菜单用
        public string controllerParameterName;
        
        // 是否自动生成控制参数
        public bool autoCreateControllerParameter = true;

        // 同步开关参数名称
        public string syncControllerParameterName;
        
        // 音量参数，可留空，留空不生成音量控制组件
        public string volumeParameter;
        
        // 速度参数，可留空，留空不生成速度控制组件
        public string speedParameter;
        
        // 当开始跳舞时禁用 Locomotion 层
        public bool disableLocomotionLayerWhenDancing = true;
        
        // 在跳舞时自动禁用 FX 表情层
        public bool disableFxFaceLayerWhenDancing = true;
        
        // FaceEmo兼容
        public bool faceEmoCompatible = false;
        
        /// <summary>
        /// 同步跳舞相关配置
        /// </summary>
        public SyncDanceConfig syncDanceConfig;
        
        // 当满足下面条件时，停止跳舞
        public CatSyncDanceEntry.DanceParameter[] stopDanceParameters;
        
        // 当跳舞开始时如何设置参数
        public CatSyncDanceParameterSetterEntry[] danceStartParameterSetters;
        
        // 当跳舞结束时如何设置参数
        public CatSyncDanceParameterSetterEntry[] danceEndParameterSetters;
        
        // 舞蹈数据
        public CatSyncDanceEntry[] dances;

        public int GetBitWidth(string parameterName)
        {
            var maxValue = GetSyncParameterMaxValue(parameterName);
            var bitWidth = 1;
            while ((1 << bitWidth) <= maxValue)
            {
                bitWidth++;
            }

            return bitWidth;
        }

        public int[] GetSyncParameterValues(string parameterName)
        {
            var list = new List<int> { 0 };
            foreach (var catSyncDanceEntry in dances)
            {
                foreach (var danceParameter in catSyncDanceEntry.danceParameters)
                {
                    if (danceParameter.parameterName == parameterName)
                    {
                        list.Add((int) danceParameter.value);
                    }
                }
            }
            return list.ToArray();
        }

        public int GetSyncParameterMaxValue(string parameterName)
        {
            var max = 0;
            foreach (var catSyncDanceEntry in dances)
            {
                foreach (var danceParameter in catSyncDanceEntry.danceParameters)
                {
                    if (danceParameter.parameterName == parameterName && danceParameter.value > max)
                    {
                        max = (int) danceParameter.value;
                    }
                }
            }
            return max;
        }

        public string[] GetBitNames(SyncDanceConfig.SyncParameterName parameterName)
        {
            var bitWidth = GetBitWidth(parameterName.name);
            if (bitWidth == 0)
            {
                Debug.LogWarning($"Sync Parameter Name: {parameterName.name} bitWidth is 0");
                return null;
            }
                
            var bitsNames = new List<string>();
            if (bitsNames == null) throw new ArgumentNullException(nameof(bitsNames));
            for (var i = parameterName.suffixStartValue; i < bitWidth + parameterName.suffixStartValue; i++)
            {
                bitsNames.Add(GetBitParameterName(new Dictionary<string, string>
                {
                    {"name", parameterName.name},
                    {"index", i.ToString()}
                }, parameterName.bitParameterNameRule));
            }
            
            return bitsNames.ToArray();
        }
        
        public int GetControllerParameterWidth()
        {
            var maxValue = dances.Length;
            var bitWidth = 1;
            while ((1 << bitWidth) <= maxValue)
            {
                bitWidth++;
            }

            return bitWidth;
        }
        
        /// <summary>
        /// 根据规则获取位参数名称
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        private string GetBitParameterName(Dictionary<string, string> pc, string rule)
        {
            if (pc == null || string.IsNullOrEmpty(rule))
                return rule;
            return pc.Aggregate(rule, (current, item) => current.Replace($"{{{item.Key}}}", item.Value ?? string.Empty));
        }
    }
}