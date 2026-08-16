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
using io.github.sereinfish.cat.tools.editor.handler;
using io.github.sereinfish.cat.tools.editor.plugin;
using io.github.sereinfish.cat.tools.editor.utils;
using nadena.dev.ndmf;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.pass
{
    /// <summary>
    /// 组件处理 Pass 基类。按 <see cref="Phase"/> 指定的构建阶段收集组件，
    /// 并分别执行两套相互独立的处理逻辑：
    /// <list type="bullet">
    /// <item><see cref="IComponentHandler"/>：逐组件处理；</item>
    /// <item><see cref="IComponentProcessor"/>：按组件类型批量处理。</item>
    /// </list>
    /// </summary>
    public abstract class ComponentHandlerPass<T> : Pass<T> where T : Pass<T>, new()
    {
        /// <summary>
        /// 当前 Pass 处理的构建阶段
        /// </summary>
        protected abstract CatBuildPhase Phase { get; }

        protected sealed override void Execute(BuildContext context)
        {
            var handlers = PackageUtils.GetBuildHandlers();
            var processors = PackageUtils.GetBuildProcessors();

            if (handlers.Length < 1 && processors.Length < 1)
            {
                Debug.LogWarning("脚本没有找到任何处理器对组件进行处理，检查脚本完整性");
            }

            var components = context.AvatarRootTransform
                .GetComponentsInChildrenTraverseByHierarchy<CatAvatarComponent>(true)
                .Where(c => c.BuildPhase == Phase)
                .ToArray();

            // IComponentHandler：逐组件处理
            foreach (var component in components)
            {
                foreach (var handler in handlers)
                {
                    if (!handler.Match(component)) continue;
                    handler.Execute(context, component);

                    Debug.Log($"{handler.GetType().Name} handled {component.GetType().Name} by {component.transform.name}");
                }
            }

            // IComponentProcessor：按组件类型批量处理
            foreach (var processor in processors)
            {
                var matched = components.Where(processor.Match).ToArray();
                if (matched.Length == 0) continue;

                processor.Execute(context, matched);

                Debug.Log($"{processor.GetType().Name} processed {matched.Length} x {matched[0].GetType().Name}");
            }
        }
    }
}
