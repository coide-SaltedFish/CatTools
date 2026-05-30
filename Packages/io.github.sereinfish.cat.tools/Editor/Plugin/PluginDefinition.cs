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

using io.github.sereinfish.cat.tools.editor.pass;
using io.github.sereinfish.cat.tools.editor.plugin;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEngine;

[assembly: ExportsPlugin(typeof(PluginDefinition))]
namespace io.github.sereinfish.cat.tools.editor.plugin
{
    public class PluginDefinition : Plugin<PluginDefinition>
    {
        public override string QualifiedName => "CatTools";
        public override string DisplayName => "CatTools";
        
        protected override void Configure()
        {
            // 克隆全部动画控制器
            InPhase(BuildPhase.Resolving).WithRequiredExtension(typeof(AnimatorServicesContext), s =>
            {
                s.Run("Clone animators", _ => { });
            });
            InPhase(BuildPhase.Transforming).WithRequiredExtension(typeof(AnimatorServicesContext), s =>
            {
                // 克隆参数列表
                s.Run(CloneExpressionParametersPass.Instance);
            });
            // 执行
            InPhase(BuildPhase.Transforming).WithRequiredExtension(typeof(AnimatorServicesContext), s =>
            {
                s.Run(ComponentHandlerPass.Instance);
            });
            // 移除所有 CatComponent
            InPhase(BuildPhase.Optimizing).Run("Remove CatComponent", context =>
            {
                foreach (var comp in context.AvatarRootObject.GetComponentsInChildren<CatAvatarComponent>(true))
                {
                    Object.DestroyImmediate(comp);
                }
            });
        }
    }
}