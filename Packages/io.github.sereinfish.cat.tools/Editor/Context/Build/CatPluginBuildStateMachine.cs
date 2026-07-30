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
        private readonly CloneContext _cloneContext;

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

        public ImmutableList<ICatState> States
        {
            get
            {
                return _stateMachine.States
                    .Select(s => s.State)
                    .Where(s => s != null)
                    .Select(s => (ICatState)new CatPluginBuildState(s, _cloneContext))
                    .ToImmutableList();
            }
        }
        
        private ImmutableList<ICatStateTransition> _anyStateTransitions = ImmutableList<ICatStateTransition>.Empty;

        public ImmutableList<ICatStateTransition> AnyStateTransitions
        {
            get => _anyStateTransitions;
            set
            {
                _anyStateTransitions = value;   
                _stateMachine.AnyStateTransitions = _anyStateTransitions.Select(x => x.GetTransition<VirtualStateTransition>()).ToImmutableList();
            }
        }

        public CatPluginBuildStateMachine(ICatLayer layer, VirtualStateMachine stateMachine, CloneContext cloneContext)
        {
            _stateMachine = stateMachine;
            _layer = layer;
            _cloneContext = cloneContext;
            if (_stateMachine.DefaultState != null)
                _defaultState = new CatPluginBuildState(_stateMachine.DefaultState, _cloneContext);
            _anyStateTransitions = _stateMachine.AnyStateTransitions
                .Select(t => (ICatStateTransition)new CatPluginBuildStateTransition(t))
                .ToImmutableList();
        }
        
        public ICatState AddState(string name, Motion motion = null, Vector3? position = null)
        {
            return _layer.AddState(name, motion, position);
        }
    }
}