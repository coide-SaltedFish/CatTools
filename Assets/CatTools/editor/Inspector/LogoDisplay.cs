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
using UnityEditor;
using UnityEngine;

namespace CatTools.editor.Inspector
{
    public static class LogoDisplay
    {
        public static readonly Texture2D LOGO_ASSET;
        
        static LogoDisplay()
        {
            var placeholderPath = AssetDatabase.GUIDToAssetPath("cdd8ab04be21408408a4de007385a3e5");

            var path = placeholderPath.Substring(0, placeholderPath.LastIndexOf("/", StringComparison.Ordinal));
            path += "/Icon_CT_Script.png";
            
            Debug.LogError($"path => {path}");

            var realLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            LOGO_ASSET = realLogo != null ? realLogo : AssetDatabase.LoadAssetAtPath<Texture2D>(placeholderPath);
        }
    }
}