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
using System.Collections.Immutable;
using System.Linq;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context.bake
{
    public class CatBakeAnimatorController: ICatAnimatorController
    {
        private readonly AnimatorController _controller;

        public ImmutableDictionary<string, AnimatorControllerParameter> Parameters
        {
            get
            {
                var parameters = _controller.parameters;
                var builder = ImmutableDictionary.CreateBuilder<string, AnimatorControllerParameter>();
                foreach (var t in parameters)
                    builder.Add(t.name, t);
                return builder.ToImmutable();;
            }
            set
            {
                _controller.parameters = value.Select(t => t.Value).ToArray();
            }
        }

        public CatBakeAnimatorController(AnimatorController controller)
        {
            _controller = controller;
        }

        public void AddLayer(ICatLayer layer, LayerPriority? priority = null)
        {
            _controller.AddLayer(layer.GetLayer<AnimatorControllerLayer>());
        }

        public int GetLayerIndex(ICatLayer layer)
        {
            return GetLayerIndex(layer.Name);
        }

        public int GetLayerIndex(string name)
        {
            for (var i = 0; i < _controller.layers.Length; i++)
            {
                var ly = _controller.layers[i];
                if (ly.name == name)
                    return i;
            }
            return -1;
        }
    }
}