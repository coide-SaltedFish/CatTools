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

using io.github.sereinfish.cat.tools.editor.context.bake;
using io.github.sereinfish.cat.tools.editor.context.build;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace io.github.sereinfish.cat.tools.editor.plugin.Tools
{
    public static class CatToolsManualBakeAvatar
    {
        [MenuItem("CatTools/Manual Bake Avatar")]
        private static void ManualBakeAvatar()
        {
            var avatar = Selection.activeGameObject;
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();

            if (descriptor == null)
            {
                Debug.LogWarning("当前没有选中 Avatar");
                return;
            }
            // 运行处理程序
            var cloneAvatar = Object.Instantiate(avatar);
            ProcessAvatar(cloneAvatar);
        }

        [MenuItem("Tools/获取当前层级选中对象", true)]
        private static bool ValidateManualBakeAvatar()
        {
            var avatar = Selection.activeGameObject;
            return avatar != null && avatar.GetComponent<VRCAvatarDescriptor>() != null;
        }
        
        private static void ProcessAvatar(GameObject avatar)
        { 
            var handlers = PackageUtils.GetBuildHandlers();
            if (handlers.Length < 1)
            {
                Debug.LogWarning("脚本没有找到任何处理器对组件进行处理，检查脚本完整性");
                return;
            }
            var components = avatar.transform.GetComponentsInChildrenTraverseByHierarchy<CatAvatarComponent>(true);
            var catContext = new CatBakeContext(avatar, "Assets/CatToolsBakeOutput");
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