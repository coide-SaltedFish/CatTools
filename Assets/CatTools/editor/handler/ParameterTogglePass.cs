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
using CatTools.editor.utils;
using CatTools.Runtime;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEngine;

namespace CatTools.editor.handler
{
    public class ParameterTogglePass : Pass<ParameterTogglePass>
    {
        private AnimatorServicesContext _asc;
        private CloneContext _cc;

        protected override void Execute(BuildContext context)
        {
            // 遍历 Object
            var targets = context.AvatarRootTransform.GetComponentsInChildren<ParameterToggle>(true);
            _asc = context.Extension<AnimatorServicesContext>();

            var services = context.Extension<AnimatorServicesContext>();
            _cc = services.ControllerContext.CloneContext;

            foreach (var target in targets) ProcessObject(context, target);
        }

        private void ProcessObject(BuildContext context, ParameterToggle parameterToggle)
        {
            if (parameterToggle?.transform == null) throw new Exception("ParameterToggle.transform is null");

            Debug.Log($"处理ObjectParameterToggle: {parameterToggle?.transform?.name} => {parameterToggle?.name}");

            // 获取动画控制器
            var animatorController = _asc.ControllerContext.Controllers[parameterToggle.layerType];

            // 构建开关动画剪辑
            var clipOn = VirtualClip.Clone(_cc,
                AnimationUtils.CreateToggleAnimationClip(context.AvatarRootObject, parameterToggle.transform,
                    parameterToggle.toggle));
            var clipOff = VirtualClip.Clone(_cc,
                AnimationUtils.CreateToggleAnimationClip(context.AvatarRootObject, parameterToggle.transform,
                    !parameterToggle.toggle));

            // 生成动画层名称
            var layerName = AnimationUtils.GetLayerName($"Toggle_{parameterToggle.name}");

            // 创建动画层
            var layer = VirtualLayer.Create(_cc, layerName);
            animatorController.AddLayer(LayerPriority.Default, layer);

            Debug.Log("CatTools " + layer + " -> 创建动画层：" + layerName);

            // 添加状态到动画层，默认到结尾
            var sm = layer.StateMachine!;
            var stateOff = sm.AddState("Off", clipOff, new Vector3(300, 0));
            var stateOn = sm.AddState("On", clipOn, new Vector3(300, 200));

            // 默认off
            sm.DefaultState = stateOff;

            // 创建过渡，当参数满足条件时，从off到on，所有条件均不满足时，从on到off
            parameterToggle.conditions.CreateConditionsTransition(animatorController, stateOn, stateOff);
        }
    }
}