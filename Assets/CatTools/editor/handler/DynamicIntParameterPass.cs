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

using System.Collections.Generic;
using System.Linq;
using CatTools.editor.utils;
using CatTools.Runtime;
using CatTools.Runtime.entity;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

namespace CatTools.editor.handler
{
    public class DynamicIntParameterPass : Pass<DynamicIntParameterPass>
    {
        private AnimatorServicesContext _asc;
        private CloneContext _cc;

        protected override void Execute(BuildContext context)
        {
            // 遍历 Object
            var targets = context.AvatarRootTransform.GetComponentsInChildren<DynamicIntParameter>(true);
            _asc = context.Extension<AnimatorServicesContext>();

            var services = context.Extension<AnimatorServicesContext>();
            _cc = services.ControllerContext.CloneContext;

            foreach (var target in targets) ProcessComponent(context, target);
        }

        /// <summary>
        ///     1. 向控制器中添加参数
        ///     2. 向菜单参数中注册同步参数
        ///     3. 遍历构建参数动画
        /// </summary>
        /// <param name="context"></param>
        /// <param name="component"></param>
        private void ProcessComponent(BuildContext context, DynamicIntParameter component)
        {
            // 获取动画控制器
            var animatorController = _asc.ControllerContext.Controllers[component.layerType];
            // 获取参数列表
            var parameters = context.AvatarDescriptor.expressionParameters.parameters.ToList();
            // 遍历参数
            foreach (var parameter in component.parameters) ProcessParameter(parameters, animatorController, parameter);

            context.AvatarDescriptor.expressionParameters.parameters = parameters.ToArray();
        }

        private void ProcessParameter(List<VRCExpressionParameters.Parameter> parameters,
            VirtualAnimatorController controller, DynamicIntParameterEntry parameter)
        {
            var randomTag = CryptoRandomString.GetRandomString();
            // 生成同步参数名称
            var bitNames = Enumerable
                .Range(1, parameter.width)
                .Select(i => $"CT_{randomTag}_BIT_{parameter.name}_{i}")
                .ToArray();
            // 向菜单参数注册
            parameters.Add(new VRCExpressionParameters.Parameter
            {
                name = parameter.name,
                valueType = VRCExpressionParameters.ValueType.Int,
                saved = false,
                networkSynced = false,
                defaultValue = 0
            });
            parameters.AddRange(bitNames.Select(bitName => new VRCExpressionParameters.Parameter
            {
                name = bitName,
                valueType = VRCExpressionParameters.ValueType.Bool,
                defaultValue = 0,
                saved = true,
                networkSynced = true
            }));
            // 向动画控制器注册
            controller.Parameters = controller.Parameters.Add(parameter.name, new AnimatorControllerParameter
            {
                name = parameter.name,
                type = AnimatorControllerParameterType.Int,
                defaultInt = 0
            });
            var initStateParameterName = $"CT_{randomTag}_BIT_{parameter.name}_Init_State";
            controller.Parameters = controller.Parameters.Add(initStateParameterName, new AnimatorControllerParameter
            {
                name = initStateParameterName,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = false
            });
            foreach (var bitName in bitNames)
                controller.Parameters = controller.Parameters.Add(bitName, new AnimatorControllerParameter
                {
                    name = bitName,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = false
                });
            // 没有 IsLocal 参数自动添加
            if (!controller.Parameters.ContainsKey("IsLocal"))
                controller.Parameters = controller.Parameters.Add("IsLocal", new AnimatorControllerParameter
                {
                    name = "IsLocal",
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = false
                });
            // 构建动画
            PrepareAnimatorController(controller, bitNames, initStateParameterName, parameter);
        }

        private void PrepareAnimatorController(VirtualAnimatorController controller, string[] bitNames,
            string initStateParameterName, DynamicIntParameterEntry parameter)
        {
            var valueMax = 1 << parameter.width;
            const int stateHeight = 60;
            var startY = -(stateHeight * parameter.width / 2);

            // 新建动画层
            var layerName = AnimationUtils.GetLayerName($"DynamicIntParameter_{parameter.name}");
            var layer = VirtualLayer.Create(_cc, layerName);
            controller.AddLayer(LayerPriority.Default, layer);

            var readStates = new List<VirtualState>();
            var writeStates = new List<VirtualState>();

            // 添加状态
            var sm = layer.StateMachine!;

            // 初始化状态
            var initState = sm.AddState("Init_0", position: new Vector3(0, startY));
            // 设置动作
            var parameterDriverInitState = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
            parameterDriverInitState.isLocalPlayer = false;
            parameterDriverInitState.isEnabled = true;
            // 读取参数设置值
            parameterDriverInitState.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                name = parameter.name,
                value = 0,
                type = VRC_AvatarParameterDriver.ChangeType.Set
            });
            // 设置初始化状态标识
            parameterDriverInitState.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                name = initStateParameterName,
                value = 1,
                type = VRC_AvatarParameterDriver.ChangeType.Set
            });
            initState.Behaviours = initState.Behaviours.Add(parameterDriverInitState);

            // 根据位数创建写入和读取状态
            for (var i = 0; i < valueMax; i++)
            {
                var stateR = sm.AddState($"R_{i}", position: new Vector3(300, startY + i * stateHeight));
                var parameterDriverR = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                parameterDriverR.isLocalPlayer = false;
                parameterDriverR.isEnabled = true;
                // 读取参数设置值
                parameterDriverR.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    name = parameter.name,
                    value = i,
                    type = VRC_AvatarParameterDriver.ChangeType.Set
                });
                // 设置初始化状态标识
                parameterDriverR.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    name = initStateParameterName,
                    value = 1,
                    type = VRC_AvatarParameterDriver.ChangeType.Set
                });

                stateR.Behaviours = stateR.Behaviours.Add(parameterDriverR);
                readStates.Add(stateR);

                var stateW = sm.AddState($"W_{i}", position: new Vector3(-300, startY + i * stateHeight));
                var parameterDriverW = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                parameterDriverR.isLocalPlayer = false;
                parameterDriverR.isEnabled = true;

                // 拆分Int
                var boolValues = i.SplitToBools(parameter.width);
                for (var j = 0; j < boolValues.Length; j++)
                {
                    var bitName = bitNames[j];
                    var bitValue = boolValues[j] ? 1 : 0;

                    parameterDriverW.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Set,
                        name = bitName,
                        value = bitValue
                    });
                }

                stateW.Behaviours = stateW.Behaviours.Add(parameterDriverW);
                writeStates.Add(stateW);
            }

            // 默认R_0
            sm.DefaultState = readStates[0];

            // 开始连线
            // 初始化状态
            var initVirtualStateTransition = VirtualStateTransition.Create();
            initVirtualStateTransition.SetDestination(initState);
            initVirtualStateTransition.ExitTime = null;
            initVirtualStateTransition.Duration = 0;

            // 设置过渡
            initVirtualStateTransition.Conditions = initVirtualStateTransition.Conditions.Add(new AnimatorCondition
            {
                mode = AnimatorConditionMode.IfNot,
                threshold = 0,
                parameter = initStateParameterName
            });
            foreach (var bitName in bitNames)
                initVirtualStateTransition.Conditions = initVirtualStateTransition.Conditions.Add(new AnimatorCondition
                {
                    mode = AnimatorConditionMode.IfNot,
                    threshold = 0,
                    parameter = bitName
                });
            sm.AnyStateTransitions = sm.AnyStateTransitions.Add(initVirtualStateTransition);

            // R_0 过渡
            var read0StateTransition = VirtualStateTransition.Create();
            read0StateTransition.SetDestination(readStates[0]);
            read0StateTransition.ExitTime = null;
            read0StateTransition.Duration = 0;

            read0StateTransition.Conditions = read0StateTransition.Conditions.Add(new AnimatorCondition
            {
                mode = AnimatorConditionMode.IfNot,
                threshold = 0,
                parameter = "IsLocal"
            });
            foreach (var bitName in bitNames)
                read0StateTransition.Conditions = read0StateTransition.Conditions.Add(new AnimatorCondition
                {
                    mode = AnimatorConditionMode.IfNot,
                    threshold = 0,
                    parameter = bitName
                });
            sm.AnyStateTransitions = sm.AnyStateTransitions.Add(read0StateTransition);

            // 读状态
            for (var i = 1; i < readStates.Count; i++)
            {
                var readState = readStates[i];

                // 本地条件过渡
                var readStateIsLocalTransition = VirtualStateTransition.Create();
                readStateIsLocalTransition.SetDestination(readState);
                readStateIsLocalTransition.ExitTime = null;
                readStateIsLocalTransition.Duration = 0;

                readStateIsLocalTransition.Conditions = readStateIsLocalTransition.Conditions.Add(new AnimatorCondition
                {
                    mode = AnimatorConditionMode.IfNot,
                    threshold = 0,
                    parameter = "IsLocal"
                });
                // 初始化过渡
                var readStateInitTransition = VirtualStateTransition.Create();
                readStateInitTransition.SetDestination(readState);
                readStateInitTransition.ExitTime = null;
                readStateInitTransition.Duration = 0;

                readStateInitTransition.Conditions = readStateInitTransition.Conditions.Add(new AnimatorCondition
                {
                    mode = AnimatorConditionMode.IfNot,
                    threshold = 0,
                    parameter = initStateParameterName
                });
                var boolValues = i.SplitToBools(parameter.width);
                for (var j = 0; j < boolValues.Length; j++)
                {
                    var bitName = bitNames[j];
                    var bitValue = boolValues[j] ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot;

                    readStateIsLocalTransition.Conditions = readStateIsLocalTransition.Conditions.Add(
                        new AnimatorCondition
                        {
                            mode = bitValue,
                            threshold = 0,
                            parameter = bitName
                        });
                    readStateInitTransition.Conditions = readStateInitTransition.Conditions.Add(new AnimatorCondition
                    {
                        mode = bitValue,
                        threshold = 0,
                        parameter = bitName
                    });
                }

                sm.AnyStateTransitions = sm.AnyStateTransitions.Add(readStateIsLocalTransition);
                sm.AnyStateTransitions = sm.AnyStateTransitions.Add(readStateInitTransition);
            }

            for (var i = 0; i < writeStates.Count; i++)
            {
                var writeState = writeStates[i];

                // 设置过渡
                var writeStateIsLocalTransition = VirtualStateTransition.Create();
                writeStateIsLocalTransition.SetDestination(writeState);
                writeStateIsLocalTransition.ExitTime = null;
                writeStateIsLocalTransition.Duration = 0;

                // 过渡条件
                writeStateIsLocalTransition.Conditions = writeStateIsLocalTransition.Conditions.Add(
                    new AnimatorCondition
                    {
                        mode = AnimatorConditionMode.If,
                        threshold = 1,
                        parameter = "IsLocal"
                    });
                writeStateIsLocalTransition.Conditions = writeStateIsLocalTransition.Conditions.Add(
                    new AnimatorCondition
                    {
                        mode = AnimatorConditionMode.If,
                        threshold = 1,
                        parameter = initStateParameterName
                    });
                writeStateIsLocalTransition.Conditions = writeStateIsLocalTransition.Conditions.Add(
                    new AnimatorCondition
                    {
                        mode = AnimatorConditionMode.Equals,
                        threshold = i,
                        parameter = parameter.name
                    });

                sm.AnyStateTransitions = sm.AnyStateTransitions.Add(writeStateIsLocalTransition);
            }
        }
    }
}