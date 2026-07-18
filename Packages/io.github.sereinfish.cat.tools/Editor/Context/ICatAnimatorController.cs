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
using nadena.dev.ndmf.animator;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context
{
    public interface ICatAnimatorController
    {
        public ImmutableDictionary<string, AnimatorControllerParameter> Parameters { get; set; }

        public void AddLayer(ICatLayer layer, LayerPriority? priority = null);

        public int GetLayerIndex(ICatLayer layer);
        public int GetLayerIndex(string name);
    }
}