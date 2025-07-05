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

namespace CatTools.editor.utils
{
    public static class IntUtils
    {
        /// <summary>
        /// 将一个 8 位数值拆成指定数量的布尔值（按最低位到最高位顺序）
        /// </summary>
        /// <param name="value">要拆分的 8 位数值（0～255）</param>
        /// <param name="count">要拆出的位数，最大 8</param>
        /// <returns>长度为 count 的 bool 数组</returns>
        public static bool[] SplitToBools(this int value, int count)
        {
            if (count < 1 || count > 8)
                throw new ArgumentOutOfRangeException(nameof(count), "count 必须在 1 到 8 之间");
            if (value < 0 || value > 0xFF)
                throw new ArgumentOutOfRangeException(nameof(value), "value 必须在 0～255 之间");

            bool[] bits = new bool[count];
            for (int i = 0; i < count; i++)
            {
                // 右移 i 位，然后与 1 做与运算，结果为 1 则该位为 true
                bits[i] = ((value >> i) & 1) == 1;
            }
            return bits;
        }
    }
}