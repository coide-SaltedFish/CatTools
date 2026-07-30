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
using VRC.SDK3.Avatars.Components;

namespace io.github.sereinfish.cat.tools.Components
{
    /// <summary>
    /// 锁定范围：锁定整个控制器还是锁定指定图层
    /// </summary>
    public enum AnimatorLayerLockScope
    {
        EntireController,
        SpecificLayer
    }

    /// <summary>
    /// 锁定操作类型：锁定到当前默认状态 或 新建空状态并锁定到空状态
    /// </summary>
    public enum AnimatorLayerLockOperation
    {
        LockToCurrentDefault,
        CreateEmptyStateAndLock
    }

    [Serializable]
    public class AnimatorLayerLockEntry
    {
        /// <summary>
        /// 要锁定的控制器类型
        /// </summary>
        public VRCAvatarDescriptor.AnimLayerType animLayerType;

        /// <summary>
        /// 锁定范围
        /// </summary>
        public AnimatorLayerLockScope lockScope;

        /// <summary>
        /// 要锁定的指定图层名称（当 lockScope 为 SpecificLayer 时生效）
        /// </summary>
        public string layerName;

        /// <summary>
        /// 锁定操作类型
        /// </summary>
        public AnimatorLayerLockOperation lockOperation;
    }
}
