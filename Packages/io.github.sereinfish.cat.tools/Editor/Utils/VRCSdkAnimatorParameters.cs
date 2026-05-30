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

using UnityEditorInternal;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class VRCSdkAnimatorParameters
    {
        public static readonly Parameter IsLocal = GetParameter("IsLocal", VRCExpressionParameters.ValueType.Bool);
        public static readonly Parameter PreviewMode = GetParameter("PreviewMode", VRCExpressionParameters.ValueType.Int);
        public static readonly Parameter Viseme = GetParameter("Viseme", VRCExpressionParameters.ValueType.Int);
        public static readonly Parameter Voice = GetParameter("Voice", VRCExpressionParameters.ValueType.Float);
        // Int：GestureLeft、GestureRight
        // Float：GestureLeftWeight、GestureRightWeight、AngularY、VelocityX、VelocityY、VelocityZ、VelocityMagnitude、Upright
        // Bool：Grounded、Seated、AFK
        // Int：TrackingType、VRMode、AvatarVersion
        // Bool：MuteSelf、InStation、Earmuffs、IsOnFriendsList、IsAnimatorEnabled
        public static readonly Parameter GestureLeft = GetParameter("GestureLeft", VRCExpressionParameters.ValueType.Int);
        public static readonly Parameter GestureRight = GetParameter("GestureRight", VRCExpressionParameters.ValueType.Int);
        public static readonly Parameter GestureLeftWeight = GetParameter("GestureLeftWeight", VRCExpressionParameters.ValueType.Float);
        public static readonly Parameter GestureRightWeight = GetParameter("GestureRightWeight", VRCExpressionParameters.ValueType.Float);
        public static readonly Parameter AngularY = GetParameter("AngularY", VRCExpressionParameters.ValueType.Float);
        public static readonly Parameter VelocityX = GetParameter("VelocityX", VRCExpressionParameters.ValueType.Float);
        public static readonly Parameter VelocityY = GetParameter("VelocityY", VRCExpressionParameters.ValueType.Float);
        public static readonly Parameter VelocityZ = GetParameter("VelocityZ", VRCExpressionParameters.ValueType.Float);
        public static readonly Parameter VelocityMagnitude = GetParameter("VelocityMagnitude", VRCExpressionParameters.ValueType.Float);
        public static readonly Parameter Upright = GetParameter("Upright", VRCExpressionParameters.ValueType.Float);
        public static readonly Parameter Grounded = GetParameter("Grounded", VRCExpressionParameters.ValueType.Bool);
        public static readonly Parameter Seated = GetParameter("Seated", VRCExpressionParameters.ValueType.Bool);
        public static readonly Parameter AFK = GetParameter("AFK", VRCExpressionParameters.ValueType.Bool);
        public static readonly Parameter TrackingType = GetParameter("TrackingType", VRCExpressionParameters.ValueType.Int);
        public static readonly Parameter VRMode = GetParameter("VRMode", VRCExpressionParameters.ValueType.Int);
        public static readonly Parameter MuteSelf = GetParameter("MuteSelf", VRCExpressionParameters.ValueType.Bool);
        public static readonly Parameter InStation = GetParameter("InStation", VRCExpressionParameters.ValueType.Bool);
        public static readonly Parameter Earmuffs = GetParameter("Earmuffs", VRCExpressionParameters.ValueType.Bool);
        public static readonly Parameter IsOnFriendsList = GetParameter("IsOnFriendsList", VRCExpressionParameters.ValueType.Bool);
        public static readonly Parameter IsAnimatorEnabled = GetParameter("IsAnimatorEnabled", VRCExpressionParameters.ValueType.Bool);
        private static Parameter GetParameter(string name, VRCExpressionParameters.ValueType type)
        {
            return new Parameter {Name = name, Type = type};
        }
        
        public struct Parameter
        {
            public string Name;
            public VRCExpressionParameters.ValueType Type;
        }
    }
}