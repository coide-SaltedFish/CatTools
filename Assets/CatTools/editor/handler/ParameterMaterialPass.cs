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

using CatTools.editor.utils;
using CatTools.Runtime;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEngine;

namespace CatTools.editor.handler
{
    public class ParameterMaterialPass : Pass<ParameterMaterialPass>
    {
        private AnimatorServicesContext _asc;
        private CloneContext _cc;

        protected override void Execute(BuildContext context)
        {
            var targets = context.AvatarRootTransform.GetComponentsInChildren<ParameterMaterial
            >(true);
            _asc = context.Extension<AnimatorServicesContext>();

            var services = context.Extension<AnimatorServicesContext>();
            _cc = services.ControllerContext.CloneContext;

            foreach (var target in targets) ProcessComponent(context, target);
        }

        /// <summary>
        ///     写入一个空状态和一个材质切换动画状态，设置 WriteDefault 为 True
        /// </summary>
        /// <param name="context"></param>
        /// <param name="target"></param>
        private void ProcessComponent(BuildContext context, ParameterMaterial target)
        {
            var controller = _asc.ControllerContext.Controllers[target.layerType];
            // 创建材质切换动画
            var clip = VirtualClip.Clone(_cc, AnimationUtils
                .CreateMaterialChangeAnimationClip(context.AvatarRootObject, target.transform,
                    target.materials.ToArray()));

            // 创建动画层
            var layerName = AnimationUtils.GetLayerName($"MaterialChange_{target.name}");
            var layer = VirtualLayer.Create(_cc, layerName);
            controller.AddLayer(LayerPriority.Default, layer);

            // 添加状态
            var sm = layer.StateMachine!;

            // 空状态
            var emptyState = sm.AddState("empty", position: new Vector3(300, 0));
            emptyState.WriteDefaultValues = true;

            // 切换状态
            var materialChangeState = sm.AddState("change", clip, new Vector3(300, 200));
            materialChangeState.WriteDefaultValues = true;

            sm.DefaultState = emptyState;

            // 设置过渡，当满足条件时，从空状态到材质切换状态，所有条件不满足则保持 empty 状态，都设置 WriteDefault 为 True
            target.conditions.CreateConditionsTransition(controller, materialChangeState, emptyState);
            // var tMaterialChange = VirtualStateTransition.Create();
            // tMaterialChange.SetDestination(emptyState);
            // tMaterialChange.ExitTime = null;
            // tMaterialChange.Duration = 0;
            //
            // materialChangeState.Transitions = materialChangeState.Transitions.Add(tMaterialChange);
            //
            // // 遍历或条件
            // foreach (var parameterConditionsEntry in target.conditions)
            // {
            //     var tEmpty = VirtualStateTransition.Create();
            //     tEmpty.SetDestination(materialChangeState);
            //     tEmpty.ExitTime = null;
            //     tEmpty.Duration = 0;
            //
            //     emptyState.Transitions = emptyState.Transitions.Add(tEmpty);
            //
            //     // 遍历与条件
            //     foreach (var parameterConditionEntry in parameterConditionsEntry.conditions)
            //     {
            //         // 找不到参数时添加同名 float 参数
            //         if (controller.Parameters.All(p => p.Value.name != parameterConditionEntry.name))
            //             controller.Parameters = controller.Parameters.Add(parameterConditionEntry.name,
            //                 new AnimatorControllerParameter
            //                 {
            //                     name = parameterConditionEntry.name,
            //                     type = AnimatorControllerParameterType.Float
            //                 });
            //
            //         // 添加条件到过渡
            //         var mode = parameterConditionEntry.condition.ToAnimatorConditionMode();
            //         if (mode is AnimatorConditionMode.If or AnimatorConditionMode.IfNot)
            //             if (parameterConditionEntry.value == "0")
            //                 mode = mode.Inverse();
            //
            //         tEmpty.Conditions = tEmpty.Conditions.Add(new AnimatorCondition
            //         {
            //             mode = mode,
            //             threshold = Convert.ToSingle(parameterConditionEntry.value),
            //             parameter = parameterConditionEntry.name
            //         });
            //         tMaterialChange.Conditions = tMaterialChange.Conditions.Add(new AnimatorCondition
            //         {
            //             mode = mode.Inverse(), // 条件取反
            //             threshold = Convert.ToSingle(parameterConditionEntry.value),
            //             parameter = parameterConditionEntry.name
            //         });
            //     }
            // }
        }
    }
}