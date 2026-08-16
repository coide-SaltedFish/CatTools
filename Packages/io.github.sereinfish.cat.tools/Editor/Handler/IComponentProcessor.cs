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

using System.Linq;
using nadena.dev.ndmf;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    /// <summary>
    /// 组件处理器（批量）：与 <see cref="IComponentHandler"/> 相互独立、互不影响。
    /// 与 IComponentHandler 逐组件处理不同，它会一次性接收当前构建阶段内所有匹配到的指定类型组件。
    /// </summary>
    public interface IComponentProcessor
    {
        bool Match(object t);
        void Execute(BuildContext context, object[] entities);
    }

    /// <summary>
    /// 组件处理器（批量）的强类型基类。实现时仅需重写 <see cref="Execute(nadena.dev.ndmf.BuildContext,T[])"/>，
    /// 其中 entities 为当前构建阶段内所有 T 类型组件。
    /// </summary>
    public abstract class ComponentProcessor<T> : IComponentProcessor
    {
        bool IComponentProcessor.Match(object t) => t is T;

        public abstract void Execute(BuildContext context, T[] entities);
        public void Execute(BuildContext context, object[] entities) => Execute(context, entities.Cast<T>().ToArray());
    }
}
