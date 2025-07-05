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

namespace CatTools.Runtime.utils
{
    public enum CatToolsAnimatorConditionMode
    {
        /// <summary>
        ///   <para>The condition is true when the parameter value is true.</para>
        /// </summary>
        If,
        /// <summary>
        ///   <para>The condition is true when the parameter value is false.</para>
        /// </summary>
        IfNot,
        /// <summary>
        ///   <para>The condition is true when parameter value is greater than the threshold.</para>
        /// </summary>
        Greater,
        /// <summary>
        ///   <para>The condition is true when the parameter value is less than the threshold.</para>
        /// </summary>
        Less,
        /// <summary>
        ///   <para>The condition is true when parameter value is equal to the threshold.</para>
        /// </summary>
        Equals,
        /// <summary>
        ///   <para>The condition is true when the parameter value is not equal to the threshold.</para>
        /// </summary>
        NotEqual,
    }
}