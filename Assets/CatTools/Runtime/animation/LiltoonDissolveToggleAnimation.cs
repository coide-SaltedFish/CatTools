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
//     /// liltoon 溶解切换动画
//     /// </summary>
//     [Serializable]
//     public class LiltoonDissolveToggleAnimation : IAnimationBuilder
//     {
//         private SerializedObject _so;
//         private SerializedProperty _property;
//         
//         [SerializeField] public int time = 60;
//         [SerializeField] public LiltoonDissolveType type;
//         
//         // Alpha 配置
//         [SerializeField] public float border = 0f;
//         [SerializeField] public float blur = 0.1f;
//
//         // UV 配置
//         [SerializeField] public LiltoonDissolveShape shape;
//         [SerializeField] public Vector2 position;
//         
//         // Position 配置
//         [SerializeField] public Texture2D noise;
//         
//         [SerializeField] public Texture2D mask;
//         [SerializeField] public Color color = Color.white;
//
//         public LiltoonDissolveToggleAnimation(SerializedObject so, SerializedProperty  property)
//         {
//             _so = so;
//             _property = property;
//         }
//         
//         
//         public AnimationClip Build(BuildContext context, Component component)
//         {
//             throw new System.NotImplementedException();
//         }
//
//         public void DoLayout()
//         {
//             // 绘制下拉框选择类型
//             EditorGUILayout.PropertyField(_so.FindProperty("type"), new GUIContent("溶解类型"));
//             
//             switch (type)
//             {
//                 case LiltoonDissolveType.Alpha:
//                     DoLayoutAlpha();
//                     break;
//                 case LiltoonDissolveType.None:
//                     break;
//                 case LiltoonDissolveType.UV:
//                     break;
//                 case LiltoonDissolveType.Position:
//                     break;
//                 default:
//                     throw new ArgumentOutOfRangeException();
//             }
//         }
//
//         private void DoLayoutNone()
//         {
//             GUILayout.Label("None");
//         }
//
//
//         private void DoLayoutAlpha()
//         {
//             
//         }
//         
//         private void DoLayoutUV()
//         {
//             
//         }
//         
//         private void DoLayoutPosition()
//         {
//             
//         }
//     }
//
//     public enum LiltoonDissolveShape
//     {
//         Point,
//         Line,
//     }
//     
//     public enum LiltoonDissolveType
//     {
//         None,
//         Alpha,
//         UV,
//         Position,
//     }
// }