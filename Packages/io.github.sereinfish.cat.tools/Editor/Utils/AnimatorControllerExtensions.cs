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

using System.Collections.Immutable;
using System.Linq;
using HarmonyLib;
using io.github.sereinfish.cat.tools.editor.context;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class AnimatorControllerExtensions
    {
        public static void TryAddParameterOrNot(this VirtualAnimatorController controller, AnimatorControllerParameter parameter)
        {
            if (controller.Parameters.ContainsKey(parameter.name))
            {
                if (controller.Parameters[parameter.name].type == AnimatorControllerParameterType.Float)
                {
                    return;
                }
                controller.Parameters = controller.Parameters.Remove(parameter.name);
            }
            controller.Parameters = controller.Parameters.Add(parameter.name, parameter);
        }
        
        public static void TryAddParameterOrNot(this AnimatorController controller, AnimatorControllerParameter parameter)
        {
            if (controller.parameters.Any(x => x.name == parameter.name))
            {
                if (controller.parameters.Any(x => x.type == AnimatorControllerParameterType.Float))
                {
                    return;
                }
                controller.parameters = controller.parameters.Where(x => x.name != parameter.name).ToArray();
            }
            controller.parameters = controller.parameters.AddItem(parameter).ToArray();
        }
        
        public static void AddParameterIfNot(this ICatAnimatorController controller,
            AnimatorControllerParameter parameter)
        {
            var param = controller.Parameters.GetValueOrDefault(parameter.name);;
            if (param != null && param.type != parameter.type) parameter.type = AnimatorControllerParameterType.Float;

            controller.Parameters = controller.Parameters.Remove(parameter.name);
            controller.Parameters = controller.Parameters.Add(parameter.name, parameter);
        }
        
        public static void AddParameterIfNot(this ICatAnimatorController controller,
            string name, AnimatorControllerParameterType type, float defaultValue = 0f)
        {
            controller.AddParameterIfNot(new AnimatorControllerParameter
            {
                name = name,
                type = type,
                defaultFloat = defaultValue
            });
        }
        
        public static void AddParameterIfNot(this ICatAnimatorController controller,
            string name, AnimatorControllerParameterType type, bool defaultValue = false)
        {
            controller.AddParameterIfNot(new AnimatorControllerParameter
            {
                name = name,
                type = type,
                defaultBool = defaultValue
            });
        }
    }
}