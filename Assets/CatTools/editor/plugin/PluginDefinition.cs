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

using CatTools.editor.handler;
using CatTools.editor.Inspector;
using CatTools.editor.plugin;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

[assembly: ExportsPlugin(
    typeof(PluginDefinition)
)]
namespace CatTools.editor.plugin
{
    public class PluginDefinition : Plugin<PluginDefinition>
    {
        public override string QualifiedName => "CatTools";
        public override string DisplayName => "CatTools";
        public override Texture2D LogoTexture => LogoDisplay.LOGO_ASSET;

        protected override void Configure()
        {
            var seq = InPhase(BuildPhase.Resolving);
            // 克隆全部动画控制器
            seq.WithRequiredExtension(typeof(AnimatorServicesContext), s =>
            {
                s.Run("Clone animators", _ => { });
            });
            
            seq = InPhase(BuildPhase.Transforming);
            seq.WithRequiredExtension(typeof(AnimatorServicesContext), s =>
            {
                // 克隆参数列表
                s.Run(CloneExpressionParametersPass.Instance);
                // 动态Int参数解析
                s.Run(DynamicIntParameterPass.Instance);
                // 动态异步Int参数解析
                s.Run(SyncDynamicIntParameterPass.Instance);
                // 参数开关解析
                s.Run(ParameterTogglePass.Instance);
                // 参数材质解析
                s.Run(ParameterMaterialPass.Instance);
                // 参数材质匹配解析
                s.Run(ParameterMatchMaterialPass.Instance);
            });
        }
    }
}