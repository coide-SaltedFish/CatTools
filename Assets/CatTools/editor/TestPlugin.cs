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
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

// [assembly: ExportsPlugin(typeof(TestPlugin))]
namespace CatTools.editor
{
    public class TestPlugin : Plugin<TestPlugin>
    {
        protected override void Configure()
        {
            // 在 Transforming 阶段创建一个名为 "MyTransforms" 的 Sequence
            InPhase(BuildPhase.Resolving)
                .Run("Clone FX Controller", ctx => {
                    var fxLayerInfo = ctx.AvatarDescriptor.baseAnimationLayers
                        .First(l => l.type == VRCAvatarDescriptor.AnimLayerType.FX);
                    var original = fxLayerInfo.animatorController;
                    var clone = UnityEngine.Object.Instantiate(original);
                    clone.name = original.name + "_NDMF";
                    fxLayerInfo.animatorController = clone;
                });
            
            // 在 Transforming 阶段注入我们的 Layer 和状态机逻辑
            InPhase(BuildPhase.Transforming)
                .BeforePlugin("nadena.dev.modular-avatar")  // 确保在 MA 正常处理前/后插入
                .Run("Add MyFX Layer", ctx =>
                {
                    // 找到 FX 层的 AnimatorController
                    var fxLayer = ctx.AvatarDescriptor.baseAnimationLayers
                        .First(l => l.type == VRCAvatarDescriptor.AnimLayerType.FX);
                    var controller = fxLayer.animatorController as AnimatorController;

                    // 1. 新建一个 Layer
                    var newLayer = new AnimatorControllerLayer
                    {
                        name = "MyFX",
                        defaultWeight = 1f,
                        stateMachine = new AnimatorStateMachine()
                    };
                    controller.AddLayer(newLayer);

                    // 2. 在新 Layer 的 StateMachine 中添加状态
                    var sm = newLayer.stateMachine;
                    // 必须给新 StateMachine 一个 Asset 路径（否则不会保存到 Controller 里）
                    AssetDatabase.AddObjectToAsset(sm, controller);
                    sm.name = "MyFX_SM";
                    
                    var idle = sm.AddState("Idle");
                    var special = sm.AddState("SpecialFX");

                    // 3. 添加 Transition：从 Idle → SpecialFX
                    var trans = idle.AddTransition(special);
                    trans.hasExitTime = false;
                    trans.duration = 0.2f;
                    
                    if (controller.parameters.All(p => p.name != "PlaySpecial"))
                        controller.AddParameter("PlaySpecial", AnimatorControllerParameterType.Bool);
                    
                    trans.AddCondition(AnimatorConditionMode.If, 0, "PlaySpecial");  // 依赖于一个 Trigger

                    // 4. 确保新状态机也被 AssetDatabase 跟踪
                    // AssetDatabase.AddObjectToAsset(idle, controller);
                    // AssetDatabase.AddObjectToAsset(special, controller);
                    // AssetDatabase.AddObjectToAsset(trans, controller);
                    
                    Debug.Log($"[CatTools] 创建状态机 {controller.name}");
                });
        }
    }
}