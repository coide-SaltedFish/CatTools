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
using io.github.sereinfish.cat.tools.editor.pass;
using io.github.sereinfish.cat.tools.editor.plugin;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor.Build.Reporting;
using Object = UnityEngine.Object;

[assembly: ExportsPlugin(typeof(PluginDefinition))]
namespace io.github.sereinfish.cat.tools.editor.plugin
{
    public class PluginDefinition : Plugin<PluginDefinition>
    {
        public override string QualifiedName => "CatTools";
        public override string DisplayName => "CatTools";
        
        protected override void OnUnhandledException(Exception e)
        {
            ErrorReport.ReportException(e);
        }
        
        protected override void Configure()
        {
            // Resolving：克隆动画控制器与表达式参数，并执行 Resolving 阶段的组件处理
            var seq = InPhase(BuildPhase.Resolving);
            seq.WithRequiredExtension(typeof(AnimatorServicesContext), s =>
            {
                s.Run("Clone animators", _ => { });
                s.Run(CloneExpressionParametersPass.Instance);
                s.Run(ComponentResolvingHandlerPass.Instance);
            });

            // Generating：执行 Generating 阶段的组件处理
            seq = InPhase(BuildPhase.Generating);
            seq.WithRequiredExtension(typeof(AnimatorServicesContext), s =>
            {
                s.Run(ComponentGeneratingHandlerPass.Instance);
            });

            // Transforming：组件处理（MA 之前）
            seq = InPhase(BuildPhase.Transforming);
            seq.BeforePlugin("nadena.dev.modular-avatar");
            seq.WithRequiredExtension(typeof(AnimatorServicesContext), s =>
            {
                s.Run(ComponentTransformingHandlerPass.Instance);
            });

            // Optimizing：优化阶段组件处理（MA 之后），最后移除 CatComponent
            seq = InPhase(BuildPhase.Optimizing);
            seq.AfterPlugin("nadena.dev.modular-avatar");
            seq.WithRequiredExtension(typeof(AnimatorServicesContext), s =>
            {
                s.Run(ComponentOptimizingHandlerPass.Instance);
                s.Run("Remove CatComponent", context =>
                {
                    // 移除所有 CatComponent
                    foreach (var comp in context.AvatarRootObject.GetComponentsInChildren<CatAvatarComponent>(true))
                    {
                        Object.DestroyImmediate(comp);
                    }
                });
            });
        }
    }
}