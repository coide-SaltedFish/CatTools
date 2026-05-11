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

using io.github.sereinfish.cat.tools.Conditions;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context
{
    public interface ICatBlendTree
    {
        /// <summary>
        /// 检查是否可以加入BlendTree
        /// </summary>
        /// <param name="conditions"></param>
        /// <returns></returns>
        public bool Check(ParameterOrConditions conditions);
        
        /// <summary>
        /// 添加动画片段
        /// </summary>
        public bool Add(ParameterOrConditions conditions, AnimationClip clip);

        public string[] GetDirectBlendParameters();
        
        public T GetBlendTree<T>() where T : class;
    }
}