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
using System.Collections.Generic;
using System.Linq;
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.animator.builder;
using io.github.sereinfish.cat.tools.editor.Conditions.Build;
using io.github.sereinfish.cat.tools.editor.context;
using io.github.sereinfish.cat.tools.editor.context.Extensions;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using PropertyName = io.github.sereinfish.cat.tools.editor.animator.builder.PropertyName;

namespace io.github.sereinfish.cat.tools.editor.handler
{
    public class CatSyncDanceHandler : ComponentHandler<CatSyncDance>
    {
        private Dictionary<string, List<string>> _syncParameterNames; // 同步参数名称
        private Transform _songsTransform; // 音乐对象
        // 发送器和接收器对象
        private readonly Dictionary<string, List<Transform>> _senderContactTransforms = new();
        private readonly Dictionary<string, List<Transform>> _receiverContactTransforms = new();
        
        public override void Execute(ICatContext context, CatSyncDance entity)
        {
            RegisterParameters(context, entity); // 注册参数
            DynamicParameterBuild(context, entity); // 构建动态参数

            if (entity.syncDanceConfig.autoCreateSyncComponent) SyncContactsBuild(context, entity); // 构建同步控制器
            
            InitSongsComponent(context, entity); // 初始化音乐组件
            
            ActionLayerBuild(context, entity); // 构建 Action 控制器
            FxLayerBuild(context, entity); // 构建 Fx 控制器
        }

        /// <summary>
        /// 构建同步控制器
        /// 在组件当前所在对象下创建 Contacts 对象，并创建 同步组件
        /// </summary>
        private void SyncContactsBuild(ICatContext context, CatSyncDance entity)
        {
            var worldTransform = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("0c7d244cb1bb7d24d9af6d34c9e6560c"));
            
            // 当前对象下新建对象
            var contacts = new GameObject("Contacts")
            {
                transform =
                {
                    localScale = new Vector3(entity.syncDanceConfig.syncRadius * 2,
                        entity.syncDanceConfig.syncRadius, entity.syncDanceConfig.syncRadius * 2)
                }
            };
            // 固定对象
            var contactsScaleConstraint = contacts.AddComponent<VRCScaleConstraint>();
            contactsScaleConstraint.IsActive = true;
            contactsScaleConstraint.GlobalWeight = 1f;
            contactsScaleConstraint.Locked = true;
            contactsScaleConstraint.ScaleOffset = new Vector3(entity.syncDanceConfig.syncRadius * 2,
                entity.syncDanceConfig.syncRadius, entity.syncDanceConfig.syncRadius * 2);
            contactsScaleConstraint.ScaleAtRest = new Vector3(entity.syncDanceConfig.syncRadius * 2,
                entity.syncDanceConfig.syncRadius, entity.syncDanceConfig.syncRadius * 2);
            contactsScaleConstraint.AffectsScaleX = true;
            contactsScaleConstraint.AffectsScaleY = true;
            contactsScaleConstraint.AffectsScaleZ = true;
            contactsScaleConstraint.Sources.Add(new VRCConstraintSource(worldTransform.transform, 1f));
            
            contacts.transform.SetParent(entity.transform);
            // 构建同步组件参数名称
            var syncParameterNames = new Dictionary<string, string[]>();
            foreach (var syncParameterName in entity.syncDanceConfig.syncParameterNames)
            {
                var parameterNames = new List<string>();
                for (var i = syncParameterName.suffixStartValue; i < entity.GetBitWidth(syncParameterName.name) + syncParameterName.suffixStartValue; i++)
                {
                    parameterNames.Add($"{syncParameterName.name}{i}"); // TODO 待优化为占位格式化
                }
                syncParameterNames.Add(syncParameterName.name, parameterNames.ToArray());
            }
            // 循环遍历同步参数名称，构建同步组件
            foreach (var (nameKey, syncParameters) in syncParameterNames)
            {
                foreach (var pName in syncParameters)
                {
                    // 添加Sender组件
                    var senderContact = new GameObject($"SC_{pName}_S");
                    if (!_senderContactTransforms.ContainsKey(nameKey)) _senderContactTransforms.Add(nameKey, new List<Transform>());
                    _senderContactTransforms[nameKey].Add(senderContact.transform);
                    senderContact.SetActive(false);
                    senderContact.transform.SetParent(contacts.transform);
                    senderContact.transform.position = new Vector3(0, -3, 0);
                    senderContact.transform.localScale = Vector3.one;
                    var senderContactComponent = senderContact.AddComponent<VRCContactSender>();
                    senderContactComponent.shapeType = ContactBase.ShapeType.Sphere;
                    senderContactComponent.radius = 100f;
                    senderContactComponent.contentTypes = DynamicsUsageFlags.Everything;
                    senderContactComponent.collisionTags = new List<string> { pName };
                    // 添加Receiver组件
                    var receiverContact = new GameObject($"RC_{pName}_R");
                    if (!_receiverContactTransforms.ContainsKey(nameKey)) _receiverContactTransforms.Add(nameKey, new List<Transform>());
                    _receiverContactTransforms[nameKey].Add(receiverContact.transform);
                    receiverContact.SetActive(true);
                    receiverContact.transform.SetParent(contacts.transform);
                    receiverContact.transform.position = new Vector3(0, -3, 0);
                    receiverContact.transform.localScale = Vector3.one;
                    var receiverContactComponent = receiverContact.AddComponent<VRCContactReceiver>();
                    receiverContactComponent.shapeType = ContactBase.ShapeType.Sphere;
                    receiverContactComponent.radius = 0.5f;
                    receiverContactComponent.contentTypes = DynamicsUsageFlags.Everything;
                    receiverContactComponent.collisionTags = new List<string> { pName };
                    receiverContactComponent.receiverType = ContactReceiver.ReceiverType.Constant;
                    receiverContactComponent.allowSelf = false;
                    receiverContactComponent.allowOthers = true;
                    receiverContactComponent.parameter = pName;
                }
            }

            if (string.IsNullOrEmpty(entity.speedParameter).Not())
            {
                // 速度同步组件
                var speedObj = new GameObject("SpeedController");
                speedObj.transform.SetParent(entity.transform);
                var speedScaleConstraint = speedObj.AddComponent<VRCScaleConstraint>();
                speedScaleConstraint.IsActive = true;
                speedScaleConstraint.GlobalWeight = 1f;
                speedScaleConstraint.Locked = true;
                speedScaleConstraint.ScaleOffset = new Vector3(entity.syncDanceConfig.syncRadius * 2,
                    entity.syncDanceConfig.syncRadius, entity.syncDanceConfig.syncRadius * 2);
                speedScaleConstraint.ScaleAtRest = new Vector3(entity.syncDanceConfig.syncRadius * 2,
                    entity.syncDanceConfig.syncRadius, entity.syncDanceConfig.syncRadius * 2);
                speedScaleConstraint.AffectsScaleX = true;
                speedScaleConstraint.AffectsScaleY = true;
                speedScaleConstraint.AffectsScaleZ = true;
                speedScaleConstraint.Sources.Add(new VRCConstraintSource(worldTransform.transform, 1f));
                var speedParentConstraint = speedObj.AddComponent<VRCParentConstraint>();
                speedParentConstraint.IsActive = true;
                speedParentConstraint.GlobalWeight = 1f;
                speedParentConstraint.Locked = true;
                speedParentConstraint.AffectsPositionX = true;
                speedParentConstraint.AffectsPositionY = true;
                speedParentConstraint.AffectsPositionZ = true;
                speedParentConstraint.AffectsRotationX = true;
                speedParentConstraint.AffectsRotationY = true;
                speedParentConstraint.AffectsRotationZ = true;
                speedParentConstraint.Sources.Add(new VRCConstraintSource(worldTransform.transform, 1f));
                // 固定组件
                var speedUpCR = new GameObject("SpeedUp")
                {
                    transform =
                    {
                        position = new Vector3(0, -5f, 0)
                    }
                };
                speedUpCR.transform.SetParent(speedObj.transform);
                var speedUpCReceiver = speedUpCR.AddComponent<VRCContactReceiver>();
                speedUpCReceiver.shapeType = ContactBase.ShapeType.Sphere;
                speedUpCReceiver.radius = 0.5f;
                speedUpCReceiver.contentTypes = DynamicsUsageFlags.Everything;
                speedUpCReceiver.collisionTags = new List<string> { entity.speedParameter };
                speedUpCReceiver.receiverType = ContactReceiver.ReceiverType.Proximity;
                speedUpCReceiver.parameter = "SpeedUp";
                // 测距组件
                var overlapController = new GameObject("OverlapController")
                {
                    transform =
                    {
                        position = new Vector3(0, -5.5f, 0)
                    }
                };
                overlapController.transform.SetParent(speedObj.transform);
                var overlapControllerComponent = overlapController.AddComponent<VRCContactReceiver>();
                overlapControllerComponent.shapeType = ContactBase.ShapeType.Sphere;
                overlapControllerComponent.radius = 0.5f;
                overlapControllerComponent.contentTypes = DynamicsUsageFlags.Everything;
                overlapControllerComponent.collisionTags = new List<string> { entity.speedParameter };
                overlapControllerComponent.receiverType = ContactReceiver.ReceiverType.Constant;
                overlapControllerComponent.parameter = "NoOverlap";
                // 碰撞检测组件
                var speedControllerCSender = new GameObject("SpeedController")
                {
                    transform =
                    {
                        position = new Vector3(0, -7f, 0)
                    }
                };
                speedControllerCSender.transform.SetParent(speedObj.transform);
                speedControllerCSender.SetActive(false);
                var speedControllerCSenderComponent = speedControllerCSender.AddComponent<VRCContactSender>();
                speedControllerCSenderComponent.shapeType = ContactBase.ShapeType.Sphere;
                speedControllerCSenderComponent.radius = 100f;
                speedControllerCSenderComponent.contentTypes = DynamicsUsageFlags.Everything;
                speedControllerCSenderComponent.collisionTags = new List<string> { entity.speedParameter };
            }
        }

        private void InitSongsComponent(ICatContext context, CatSyncDance entity)
        {
            // 音乐组件
            var songsObj = new GameObject("Songs");
            _songsTransform = songsObj.transform;
            songsObj.transform.SetParent(entity.transform);
            var audioSourceComponent = songsObj.AddComponent<AudioSource>();
            audioSourceComponent.bypassEffects = true;
            audioSourceComponent.bypassListenerEffects = true;
            audioSourceComponent.bypassReverbZones = true;
            audioSourceComponent.playOnAwake = true;
            audioSourceComponent.loop = true;
            audioSourceComponent.priority = 0;
            audioSourceComponent.volume = 0.7f;
            audioSourceComponent.pitch = 1f;
            audioSourceComponent.panStereo = 0f;
            audioSourceComponent.spatialBlend = 1f;
            audioSourceComponent.reverbZoneMix = 1f;
            
            audioSourceComponent.dopplerLevel = 1f; // 多普勒级别
            audioSourceComponent.spread = 180;
            audioSourceComponent.rolloffMode = AudioRolloffMode.Linear;
            audioSourceComponent.minDistance = 1f;
            audioSourceComponent.maxDistance = 5f;
            var songsVrcSpatialAudioSource = songsObj.AddComponent<VRCSpatialAudioSource>();
            songsVrcSpatialAudioSource.Gain = 5f;
            songsVrcSpatialAudioSource.Far = 30f;
            songsVrcSpatialAudioSource.Near = 6f;
            songsVrcSpatialAudioSource.VolumetricRadius = 0.5f;
            songsVrcSpatialAudioSource.EnableSpatialization = true;
            songsVrcSpatialAudioSource.UseAudioSourceVolumeCurve = true;
        }
        
        /// <summary>
        /// 构建 Action 控制器
        /// </summary>
        private void ActionLayerBuild(ICatContext context, CatSyncDance entity)
        {
            var controller = context.GetAnimatorController(VRCAvatarDescriptor.AnimLayerType.Action);
            var layer = ICatLayer.Create(context, $"CatSyncDance_{StringHelper.GetRandomString()}")
                .AddToController(controller);
            var waitState = layer.AddState("Wait");
            layer.DefaultState = waitState;
            waitState.CreateScriptableObject<VRCAnimatorLayerControl>(animatorLayerControl =>
            {
                animatorLayerControl.playable = VRC_AnimatorLayerControl.BlendableLayer.Action;
                // animatorLayerControl.layer = controller.GetLayerIndex(layer);
                animatorLayerControl.layer = controller.GetLayerIndex(layer);
                animatorLayerControl.goalWeight = 0f;
                animatorLayerControl.blendDuration = 0;
            });
            var initState = layer.AddState("Init");
            initState.CreateScriptableObject<VRCPlayableLayerControl>(playableLayerControl =>
            {
                playableLayerControl.layer = VRC_PlayableLayerControl.BlendableLayer.Action;
                playableLayerControl.goalWeight = 1f;
                playableLayerControl.blendDuration = 0.25f;
            });
            initState.CreateScriptableObject<VRCAnimatorTrackingControl>(animatorTrackingControl =>
            {
                animatorTrackingControl.trackingHead = VRC_AnimatorTrackingControl.TrackingType.Animation;
                animatorTrackingControl.trackingLeftHand = VRC_AnimatorTrackingControl.TrackingType.Animation;
                animatorTrackingControl.trackingRightHand = VRC_AnimatorTrackingControl.TrackingType.Animation;
                animatorTrackingControl.trackingHip = VRC_AnimatorTrackingControl.TrackingType.Animation;
                animatorTrackingControl.trackingLeftFoot = VRC_AnimatorTrackingControl.TrackingType.Animation;
                animatorTrackingControl.trackingRightFoot = VRC_AnimatorTrackingControl.TrackingType.Animation;
                animatorTrackingControl.trackingLeftFingers = VRC_AnimatorTrackingControl.TrackingType.Animation;
                animatorTrackingControl.trackingRightFingers = VRC_AnimatorTrackingControl.TrackingType.Animation;
                animatorTrackingControl.trackingEyes = VRC_AnimatorTrackingControl.TrackingType.NoChange;
                animatorTrackingControl.trackingMouth = VRC_AnimatorTrackingControl.TrackingType.NoChange;
            });
            initState.CreateScriptableObject<VRCAnimatorLayerControl>(animatorLayerControl =>
            {
                animatorLayerControl.playable = VRC_AnimatorLayerControl.BlendableLayer.Action;
                animatorLayerControl.layer = controller.GetLayerIndex(layer);
                animatorLayerControl.goalWeight = 1f;
                animatorLayerControl.blendDuration = 0.1f;
            });
            // OnlyOneState
            var onlyOneState = layer.AddState("OnlyOne");
            onlyOneState.CreateScriptableObject<VRCAvatarParameterDriver>(avatarParameterDriver =>
            {
                avatarParameterDriver.AddParameterDriverSet(entity.controllerParameterName, 0f);
            });
            // stop State
            var stopState = layer.AddState("Stop");
            stopState.CreateScriptableObject<VRCAnimatorLayerControl>(animatorLayerControl =>
            {
                animatorLayerControl.playable = VRC_AnimatorLayerControl.BlendableLayer.Action;
                animatorLayerControl.layer = controller.GetLayerIndex(layer);
                animatorLayerControl.goalWeight = 0f;
                animatorLayerControl.blendDuration = 0f;
            });
            stopState.CreateScriptableObject<VRCPlayableLayerControl>(playableLayerControl =>
            {
                playableLayerControl.layer = VRC_PlayableLayerControl.BlendableLayer.Action;
                playableLayerControl.goalWeight = 0f;
                playableLayerControl.blendDuration = 0f;
            });
            // 设置wait到init的条件
            var toInitConditions = ConditionsBuilder.Create()
                .Run(builder =>
                {
                    var init = false;
                    foreach (var syncDanceEntry in entity.dances)
                    {
                        if (init.Not()) init = true; else builder.Or();
                        foreach (var danceParameter in syncDanceEntry.danceParameters)
                        {
                            builder.Equal(danceParameter.parameterName, danceParameter.value);
                        }
                    }

                    for (var i = 0; i < entity.dances.Length; i++)
                    {
                        builder.Or().Equal(entity.controllerParameterName, i + 1);
                    }
                }).Build();
            toInitConditions.CreateConditionsTransitionTo(context, controller, waitState, initState);
            // next 条件
            var runDanceConditions = ConditionsBuilder.Create()
                .If("pleasenext_false", false)
                .Build();
            // onlyOne to stop
            runDanceConditions.CreateConditionsTransitionTo(context, controller, onlyOneState, stopState);
            // stop to exit
            runDanceConditions.CreateConditionsTransitionToExit(context, controller, stopState);
            // 添加动画剪辑状态
            var danceStateY = 0;
            for (var i = 0; i < entity.dances.Length; i++)
            {
                var danceStateX = 600;
                var syncDanceEntry = entity.dances[i];
                // 开始条件
                var toDanceConditions = ConditionsBuilder.Create()
                    .Equal(entity.controllerParameterName, i + 1).Or()
                    .ForEach(syncDanceEntry.danceParameters, (builder, parameter) =>
                    {
                        builder.Equal(parameter.parameterName, parameter.value);
                    }, (builder, _) =>
                    {
                        builder.Or();
                    })
                    .Build();
                // 不循环的动作
                controller.AddParameterIfNot("pleasenext_false", AnimatorControllerParameterType.Bool);
                var stopDanceConditions = ConditionsBuilder.Create()
                    .ForEach(entity.stopDanceParameters, (builder, parameter) =>
                    {
                        builder.Equal(parameter.parameterName, parameter.value);
                    }, (builder, _) => builder.Or())
                    .Or()
                    .NotEqual(entity.controllerParameterName, i + 1)
                    .ForEach(syncDanceEntry.danceParameters, (builder, parameter) =>
                    {
                        builder.NotEqual(parameter.parameterName, parameter.value);
                    }).Build();
                var firstDanceClip = syncDanceEntry.clip.TryGet(0);
                var stateDanceState = layer.AddState(syncDanceEntry.danceName, firstDanceClip, new Vector3(danceStateX, danceStateY));
                SetSyncDanceSpeed(stateDanceState, entity);
                danceStateX += 200;
                toDanceConditions.CreateConditionsTransitionTo(context, controller, initState, stateDanceState);
                stopDanceConditions.CreateConditionsTransitionTo(context, controller, stateDanceState, stopState);
                var endDanceState = stateDanceState;
                for (var i1 = 1; i1 < syncDanceEntry.clip.Length; i1++)
                {
                    var animationClip = syncDanceEntry.clip[i1];
                    var nextDanceState = layer.AddState($"{syncDanceEntry.danceName}_{i1}", animationClip, new Vector3(danceStateX, danceStateY));
                    SetSyncDanceSpeed(nextDanceState, entity);
                    danceStateX += 200;
                    runDanceConditions.CreateConditionsTransitionTo(context, controller, endDanceState, nextDanceState, exitTime:1f);
                    stopDanceConditions.CreateConditionsTransitionTo(context, controller, nextDanceState, stopState);
                    endDanceState = nextDanceState;
                    
                    if (syncDanceEntry.loop && animationClip != null && i1 == syncDanceEntry.clip.Length - 1 && syncDanceEntry.loopClip == null)
                    {
                        // 报错，设置为 loop 但是没有设置动画的 loop 选项
                        throw new ArgumentException(
                            $"Loop is enabled but animation loop option is not configured: {animationClip.name}"
                        );
                    }
                }

                if (syncDanceEntry.loopClip != null)
                {
                    if (!syncDanceEntry.loopClip.isLooping)
                    {
                        // 报错，设置为 loop 但是没有设置动画的 loop 选项
                        throw new ArgumentException(
                            $"Loop is enabled but animation loop option is not configured: {syncDanceEntry.loopClip.name}"
                        );
                    }
                    var nextDanceState = layer.AddState(syncDanceEntry.danceName + "_loop", syncDanceEntry.loopClip, new Vector3(danceStateX, danceStateY));
                    SetSyncDanceSpeed(nextDanceState, entity);
                    runDanceConditions.CreateConditionsTransitionTo(context, controller, endDanceState, nextDanceState, exitTime:1f);
                    stopDanceConditions.CreateConditionsTransitionTo(context, controller, nextDanceState, stopState);
                }
                else if (!syncDanceEntry.loop)
                {
                    runDanceConditions.CreateConditionsTransitionTo(context, controller, endDanceState, onlyOneState, exitTime:1f);
                }
                danceStateY += 120;
            }
        }
        
        /// <summary>
        /// 构建 Fx 控制器
        /// </summary>
        private void FxLayerBuild(ICatContext context, CatSyncDance entity)
        {
            var controller = context.GetAnimatorController(VRCAvatarDescriptor.AnimLayerType.FX);
            // bits sender set
            FxBitsSenderControllerLayerBuild(context, entity, controller);
            // music play
            FxMusicPlayerLayerBuild(context, entity, controller);
            // volume control
            if (string.IsNullOrEmpty(entity.volumeParameter).Not())
            {
                FxMusicVolumeControllerLayerBuild(context, entity, controller);
            }
            // speed control
            if (string.IsNullOrEmpty(entity.speedParameter).Not())
            {
                FxMusicPitchControllerLayerBuild(context, entity, controller);
            }
        }

        private void FxBitsSenderControllerLayerBuild(ICatContext context, CatSyncDance entity,
            ICatAnimatorController controller)
        {
            var layer = ICatLayer.Create(context, $"CatSyncDanceFXBitsSender_{StringHelper.GetRandomString()}")
                .AddToController(controller);
            // 当 mmd dance 为 0 时, all sender off
            var offClip = AnimationBuilder.Create()
                .Run(builder =>
                {
                    foreach (var transform in _senderContactTransforms.Values.SelectMany(senderContactTransform => senderContactTransform))
                    {
                        builder.SetCurve(CatToolsPath.GetRelativePath(context.AvatarRootTransform, transform),
                            typeof(GameObject), PropertyName.ObjIsActive,
                            curveBuilder =>
                            {
                                curveBuilder.AddKey(0f, 0f);
                            });
                    }
                }).Build();
            ConditionsBuilder.Create()
                .Equal(entity.controllerParameterName, 0f)
                .Build().CreateAnyStateConditionsTransition(context, controller, layer, layer.AddState("OFF", offClip));
            // 依次生成 State
            for (var i = 0; i < entity.dances.Length; i++)
            {
                var danceInfo = entity.dances[i];
                var clip = AnimationBuilder.Create()
                    .Run(builder =>
                    {
                        foreach (var danceParameter in danceInfo.danceParameters)
                        {
                            var width = entity.GetBitWidth(danceParameter.parameterName);
                            var bits = ((int)danceParameter.value).SplitToBools(width);
                            if (_senderContactTransforms.ContainsKey(danceParameter.parameterName).Not())
                                continue;
                            for (var i1 = 0; i1 < _senderContactTransforms[danceParameter.parameterName].Count; i1++)
                            {
                                var i2 = i1;
                                builder.SetCurve(CatToolsPath.GetRelativePath(context.AvatarRootTransform, _senderContactTransforms[danceParameter.parameterName][i1]),
                                    typeof(GameObject), PropertyName.ObjIsActive,
                                    curveBuilder =>
                                    {
                                        curveBuilder.AddKey(0f, bits[i2] ? 1f : 0f);
                                    });
                            }
                        }
                    }).Build();
                ConditionsBuilder.Create()
                    .Equal(entity.controllerParameterName, i + 1)
                    .Build().CreateAnyStateConditionsTransition(context, controller, layer, layer.AddState($"dance {i + 1}", clip));
            }
        }
        
        private void FxMusicPlayerLayerBuild(ICatContext context, CatSyncDance entity, ICatAnimatorController controller)
        {
            // 参数注册
            var nowPlayParameterName = $"CatSyncDanceNowMusicPlay_{StringHelper.GetRandomString()}";
            controller.AddParameterIfNot(nowPlayParameterName,
                AnimatorControllerParameterType.Int);
            var musicPlayLayer = ICatLayer.Create(context, "CatSyncDanceMusicPlayer")
                .AddToController(controller);
            // 动画
            var songsPath = CatToolsPath.GetRelativePath(context.AvatarRootTransform, _songsTransform);
            var offClip = AnimationBuilder.Create()
                .SetCurve(songsPath, typeof(GameObject), PropertyName.ObjIsActive, builder =>
                {
                    builder.AddKey(new Keyframe(0f, 0f));
                }).Build();
            var onClip = AnimationBuilder.Create()
                .SetCurve(songsPath, typeof(GameObject), PropertyName.ObjIsActive, builder =>
                {
                    builder.AddKey(new Keyframe(0f, 1f));
                }).Build();
            // OFF
            var offState =musicPlayLayer.AddState("OFF", offClip);
            musicPlayLayer.DefaultState = offState;
            // ON
            var onState = musicPlayLayer.AddState("ON", onClip);
            onState.CreateScriptableObject<VRCAnimatorPlayAudio>(animationPlayAudio =>
            {
                animationPlayAudio.Source = _songsTransform.GetComponent<AudioSource>();
                animationPlayAudio.SourcePath = songsPath;

                animationPlayAudio.PlaybackOrder = VRC_AnimatorPlayAudio.Order.Parameter;
                animationPlayAudio.ClipsApplySettings = VRC_AnimatorPlayAudio.ApplySettings.AlwaysApply;
                animationPlayAudio.ParameterName = entity.controllerParameterName;
                animationPlayAudio.Volume = Vector2.one;
                animationPlayAudio.VolumeApplySettings = VRC_AnimatorPlayAudio.ApplySettings.AlwaysApply;
                animationPlayAudio.Pitch = Vector2.one;
                animationPlayAudio.PitchApplySettings = VRC_AnimatorPlayAudio.ApplySettings.AlwaysApply;
                animationPlayAudio.Loop = true;
                animationPlayAudio.LoopApplySettings = VRC_AnimatorPlayAudio.ApplySettings.AlwaysApply;
                animationPlayAudio.StopOnEnter = true;
                animationPlayAudio.StopOnExit = true;
                animationPlayAudio.PlayOnEnter = true;
                animationPlayAudio.PlayOnExit = false;
                animationPlayAudio.DelayInSeconds = 0f;
                // 歌曲剪辑
                var musicClips = new List<AudioClip> { null };
                musicClips.AddRange(entity.dances.Select(syncDanceEntry => syncDanceEntry.musicClip));
                animationPlayAudio.Clips = musicClips.ToArray();
            });
            onState.CreateScriptableObject<VRCAvatarParameterDriver>(driver =>
            {
                driver.AddParameterDriverCopy(nowPlayParameterName, entity.controllerParameterName);
            });
            // 设置过渡 off -> on
            ConditionsBuilder.Create()
                .Greater(entity.controllerParameterName, 0f)
                .Build()
                .CreateConditionsTransitionTo(context, controller, offState, onState);
            // 设置过渡 on -> off
            ConditionsBuilder.Create()
                .Equal(entity.controllerParameterName, 0f)
                .Run(build =>
                {
                    for (var i = 0; i < entity.dances.Length; i++)
                    {
                        build.Or()
                            .Equal(entity.controllerParameterName, i + 1)
                            .NotEqual(nowPlayParameterName, i + 1);
                    }
                }).Build()
                .CreateConditionsTransitionTo(context, controller, onState, offState);
        }

        private void FxMusicVolumeControllerLayerBuild(ICatContext context, CatSyncDance entity,
            ICatAnimatorController controller)
        {
            var layer = ICatLayer.Create(context, $"MusicVolume_{StringHelper.GetRandomString()}")
                .AddToController(controller);
            var setVolumeClip = AnimationBuilder.Create()
                .SetCurve(CatToolsPath.GetRelativePath(context.AvatarRootTransform, _songsTransform), typeof(AudioSource), PropertyName.Volume, builder =>
                {
                    builder.AddKey(new Keyframe(0f, 0f));
                    builder.AddKey(new Keyframe(1f, 1f));
                })
                .Build();
            var volumeState = layer.AddState("Volume", setVolumeClip);
            layer.DefaultState = volumeState;
            volumeState.TimeParameter = entity.volumeParameter;
        }
        
        private void FxMusicPitchControllerLayerBuild(ICatContext context, CatSyncDance entity,
            ICatAnimatorController controller)
        {
            var layer = ICatLayer.Create(context, $"MusicPitch_{StringHelper.GetRandomString()}")
                .AddToController(controller);
            var setPitchClip = AnimationBuilder.Create()
                .SetCurve(CatToolsPath.GetRelativePath(context.AvatarRootTransform, _songsTransform), typeof(AudioSource), PropertyName.Pitch, builder =>
                {
                    builder.AddKey(new Keyframe(0f, 0f));
                    builder.AddKey(new Keyframe(1f, 1f));
                    builder.AddKey(new Keyframe(2f, 2f));
                }).Build();
            var pitchState = layer.AddState("Pitch", setPitchClip);
            layer.DefaultState = pitchState;
            pitchState.TimeParameter = entity.speedParameter;
        }

        /// <summary>
        /// 构建同步参数层
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        private void DynamicParameterBuild(ICatContext context, CatSyncDance entity)
        {
            var controller = context.GetAnimatorController(VRCAvatarDescriptor.AnimLayerType.FX);
            
            foreach (var syncParameterName in entity.syncDanceConfig.syncParameterNames)
            {
                var bitWidth = entity.GetBitWidth(syncParameterName.name);
                if (bitWidth == 0)
                {
                    Debug.LogWarning($"Sync Parameter Name: {syncParameterName.name} bitWidth is 0");
                    continue;
                }

                DynamicIntParameterHandler.CreateDynamicInt(context, controller, syncParameterName.name, entity.GetBitNames(syncParameterName),
                    bitWidth, false, false, 0, true, false, true);
            }
        }
        
        /// <summary>
        /// 对参数进行注册处理
        /// </summary>
        private void RegisterParameters(ICatContext context, CatSyncDance entity)
        {
            var fxController = context.GetAnimatorController(VRCAvatarDescriptor.AnimLayerType.FX);
            // VRC Expressions Parameters
            if (entity.autoCreateControllerParameter)
            {
                DynamicIntParameterHandler.CreateDynamicInt(context, fxController, entity.controllerParameterName, null,
                    entity.GetControllerParameterWidth(), false, true, 0,true, true, false);
            }
            
            // 速度和音量参数
            context.GetAvatarDescriptor().ExpressionParameters()
                .AddIfNameNotEmptyOrNull(entity.speedParameter, VRCExpressionParameters.ValueType.Float, 0.5f, true)
                .AddIfNameNotEmptyOrNull(entity.volumeParameter, VRCExpressionParameters.ValueType.Float, 0.7f, true)
                .Build();
            
            // Animator Controller
            var actinController = context.GetAnimatorController(VRCAvatarDescriptor.AnimLayerType.Action);

            if (!string.IsNullOrEmpty(entity.speedParameter))
            {
                actinController.AddParameterIfNot(entity.speedParameter, AnimatorControllerParameterType.Float, 0.5f);
                fxController.AddParameterIfNot(entity.speedParameter, AnimatorControllerParameterType.Float, 0.5f);
            }

            if (!string.IsNullOrEmpty(entity.volumeParameter))
            {
                actinController.AddParameterIfNot(entity.volumeParameter, AnimatorControllerParameterType.Float, 0.7f);
                fxController.AddParameterIfNot(entity.volumeParameter, AnimatorControllerParameterType.Float, 0.7f);
            }
            
            actinController.AddParameterIfNot(entity.controllerParameterName, AnimatorControllerParameterType.Int);
            fxController.AddParameterIfNot(entity.controllerParameterName, AnimatorControllerParameterType.Int);
            foreach (var syncParameterName in entity.syncDanceConfig.syncParameterNames)
            {
                actinController.AddParameterIfNot(syncParameterName.name, AnimatorControllerParameterType.Int);
                fxController.AddParameterIfNot(syncParameterName.name, AnimatorControllerParameterType.Int);
                foreach (var bitName in entity.GetBitNames(syncParameterName))
                {
                    // actinController.AddParameterIfNot(bitName, AnimatorControllerParameterType.Bool);
                    fxController.AddParameterIfNot(bitName, AnimatorControllerParameterType.Bool);
                }
            }
        }

        private static void SetSyncDanceSpeed(ICatState state, CatSyncDance entity)
        {
            if (string.IsNullOrEmpty(entity.speedParameter)) return;
            state.Speed = 2f;
            state.SpeedParameter = entity.speedParameter;
            
            Debug.LogWarning($"state.SpeedParameter {state.SpeedParameter}");
        }
    }
}