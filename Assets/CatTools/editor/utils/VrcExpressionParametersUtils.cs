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
using System.Linq;
using JetBrains.Annotations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace CatTools.editor.utils
{
    public static class VrcExpressionParametersUtils
    {
        [CanBeNull]
        public static VRCExpressionParameters.Parameter GetParameterByName(this VRCAvatarDescriptor descriptor,
            string name)
        {
            return descriptor.expressionParameters.parameters.FirstOrDefault(parameter => parameter.name == name);
        }
        
        /// <summary>
        /// 添加参数，如果参数已存在则调用回调处理
        /// 回调为空则抛出异常
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="parameter"></param>
        /// <param name="handlerFunc"></param>
        /// <returns></returns>
        public static VRCExpressionParameters.Parameter AddParameterOrElse(this VRCAvatarDescriptor descriptor,
            VRCExpressionParameters.Parameter parameter, Func<VRCExpressionParameters.Parameter, VRCExpressionParameters.Parameter> handlerFunc = null)
        {
            var param = descriptor.GetParameterByName(parameter.name);
            if (param == null)
            {
                param = parameter;
                descriptor.expressionParameters.parameters = descriptor.expressionParameters.parameters.Append(param).ToArray();
            }
            else
            {
                if (handlerFunc != null)
                {
                    // 移除旧参数
                    descriptor.expressionParameters.parameters = descriptor.expressionParameters.parameters.Where(p => p != param).ToArray();
                    param = handlerFunc(param);
                    descriptor.expressionParameters.parameters = descriptor.expressionParameters.parameters.Append(param).ToArray();
                }
                else
                {
                    throw new Exception($"参数已存在: {parameter.name}");
                }
            }

            return param;
        }
    }
}