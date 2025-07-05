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

// using System;
// using nadena.dev.ndmf;
// using UnityEditor;
// using UnityEngine;
//
// namespace CatTools.Runtime.animation
// {
//     /// <summary>
//     /// 动画构建器，用于提供给组件进行动画构建选择
//     /// </summary>
//     public interface IAnimationBuilder
//     {
//         /// <summary>
//         /// 构建动画
//         /// </summary>
//         /// <param name="context"></param>
//         /// <param name="component"></param>
//         /// <returns></returns>
//         public AnimationClip Build(BuildContext context, Component component);
//
//         /// <summary>
//         /// 构建配置界面
//         /// </summary>
//         public void DoLayout();
//         
//     }
//
//     public enum AnimationBuilderType
//     {
//         Null,
//         LiltoonDissolveToggleAnimation
//     }
//     
//     public static class AnimationBuilderExtensions
//     {
//         public static IAnimationBuilder Create(this AnimationBuilderType type, SerializedObject so, SerializedProperty property)
//         {
//             return type switch
//             {
//                 AnimationBuilderType.Null => null,
//                 AnimationBuilderType.LiltoonDissolveToggleAnimation => new LiltoonDissolveToggleAnimation(so, property),
//                 _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
//             };
//         }
//     }
// }