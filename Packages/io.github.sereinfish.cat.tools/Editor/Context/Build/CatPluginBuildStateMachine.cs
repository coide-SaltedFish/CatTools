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
using JetBrains.Annotations;
using nadena.dev.ndmf.animator;
using UnityEditor.VersionControl;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context.build
{
    public class CatPluginBuildStateMachine: ICatStateMachine
    {
        private readonly VirtualStateMachine _stateMachine;
        private readonly ICatLayer _layer;

        private ICatState _defaultState;
        public ICatState DefaultState
        {
            get => _defaultState;
            set
            {
                _stateMachine.DefaultState = value.GetState<VirtualState>();
                _defaultState = value;
            }
        }

        public Vector3 EntryPosition { get => _stateMachine.EntryPosition; set => _stateMachine.EntryPosition = value; }
        public Vector3 AnyStatePosition { get => _stateMachine.AnyStatePosition; set => _stateMachine.AnyStatePosition = value; }
        
        private ImmutableList<ICatStateTransition> _anyStateTransitions = ImmutableList<ICatStateTransition>.Empty;

        public ImmutableList<ICatStateTransition> AnyStateTransitions
        {
            get => _anyStateTransitions;
            set
            {
                _stateMachine.AnyStateTransitions = value.Select(x => x.GetTransition<VirtualStateTransition>()).ToImmutableList();
                _anyStateTransitions = value;   
            }
        }

        public CatPluginBuildStateMachine(ICatLayer layer, VirtualStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
            _layer = layer;
        }
        
        public ICatState AddState(string name, Motion motion = null, Vector3? position = null)
        {
            return _layer.AddState(name, motion, position);
        }
    }
}