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

using io.github.sereinfish.cat.tools.editor.context.build;
using io.github.sereinfish.cat.tools.editor.utils;
using nadena.dev.ndmf;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.pass
{
    public class ComponentHandlerPass : Pass<ComponentHandlerPass>
    {
        protected override void Execute(BuildContext context)
        {
            // 获取所有的处理器
            var handlers = PackageUtils.GetBuildHandlers();

            if (handlers.Length < 1) Debug.LogWarning("脚本没有找到任何处理器对组件进行处理，检查脚本完整性");
            
            // 依次进行处理
            var components = context.AvatarRootTransform.GetComponentsInChildrenTraverseByHierarchy<CatAvatarComponent>(true);
            var catContext = new CatPluginBuildContext(context);
            foreach (var catAvatarComponent in components)
            {
                foreach (var handler in handlers)
                {
                    if (!handler.Match(catAvatarComponent)) continue;
                    handler.Execute(catContext, catAvatarComponent);
                        
                    Debug.Log($"{handler.GetType().Name} handled {catAvatarComponent.GetType().Name} by {catAvatarComponent.transform.name}");
                }
            }
            catContext.AfterBuild();
        }
    }
}