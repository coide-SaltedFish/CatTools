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
using System.Linq;
using nadena.dev.ndmf.animator;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context.build
{
    public class CatPluginBuildLayer: ICatLayer
    {
        private readonly VirtualLayer _layer;
        private readonly CloneContext _cloneContext;
        private readonly VirtualStateMachine _stateMachine;
        
        public string Name { get => _layer.Name; set => _layer.Name = value; }
        
        private CatPluginBuildStateMachine _catPluginBuildStateMachine;

        public ICatStateMachine StateMachine
        {
            get
            {
                _catPluginBuildStateMachine ??= new CatPluginBuildStateMachine(this, _layer.StateMachine);
                return _catPluginBuildStateMachine;
            }
        }

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
        
        private ImmutableList<ICatStateTransition> _transitions = ImmutableList<ICatStateTransition>.Empty;
        public ImmutableList<ICatStateTransition> AnyStateTransitions
        {
            get => _transitions;
            set
            {
                _stateMachine.AnyStateTransitions = value.Select(x => x.GetTransition<VirtualStateTransition>()).ToImmutableList();
                _transitions = value;
            }
        }

        public ICatState AddState(string name, Motion motion = null, Vector3? position = null)
        {
            position ??= new Vector3(300, (_layer.StateMachine?.States?.Count ?? 0) * 120);
            
            var virtualState = _stateMachine.AddState(name, _cloneContext.Clone(motion), position);
            var state = new CatPluginBuildState(virtualState, _cloneContext);
            
            return state;
        }

        public T GetLayer<T>() where T : class
        {
            return _layer as T;
        }

        private CatPluginBuildLayer(CloneContext context, string name)
        {
            _layer = VirtualLayer.Create(context, name);
            _cloneContext = context;
            _stateMachine = _layer.StateMachine!;
        }
        public static ICatLayer Create(CloneContext context, string name)
        {
            return new CatPluginBuildLayer(context, name);
        }
    }
}