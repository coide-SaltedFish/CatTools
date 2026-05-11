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
using System.Collections.Immutable;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context
{
    public interface ICatLayer
    {
        public string Name { get; set; }
        public ICatStateMachine StateMachine { get; }
        
        public ICatState DefaultState { get; set; }
        public Vector3 EntryPosition { get; set; }
        public Vector3 AnyStatePosition { get; set; }
        
        public ImmutableList<ICatStateTransition> AnyStateTransitions { get; set; }
        
        public ICatState AddState(string name, Motion motion = null, Vector3? position = null);

        public static ICatLayer Create(ICatContext context, string name)
        {
            return context.CreateLayer(name);
        }

        public ICatLayer AddToController(ICatAnimatorController controller)
        {
            controller.AddLayer(this);
            return this;
        }

        public T GetLayer<T>() where T : class;
    }
}