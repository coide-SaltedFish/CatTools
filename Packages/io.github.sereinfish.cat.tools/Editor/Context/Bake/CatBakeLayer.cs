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
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context.bake
{
    public class CatBakeLayer: ICatLayer
    {
        private readonly AnimatorControllerLayer _layer;
        private readonly AnimatorStateMachine _stateMachine;
        
        public string Name { get => _layer.name; set => _layer.name = value; }
        
        private CatBakeStateMachine _catStateMachine;

        public ICatStateMachine StateMachine
        {
            get
            {
                _catStateMachine ??= new CatBakeStateMachine(_stateMachine);
                return _catStateMachine;
            }
        }
        
        private ICatState _defaultState;

        public ICatState DefaultState
        {
            get => _defaultState;
            set
            {
                _layer.stateMachine.defaultState = value.GetState<AnimatorState>();
                _defaultState = value;
            }
        }

        public Vector3 EntryPosition { get => _stateMachine.entryPosition; set => _stateMachine.entryPosition = value; }
        public Vector3 AnyStatePosition { get => _stateMachine.anyStatePosition; set => _stateMachine.anyStatePosition = value; }
        
        private ImmutableList<ICatStateTransition> _anyStateTransitions = ImmutableList<ICatStateTransition>.Empty;

        public ImmutableList<ICatStateTransition> AnyStateTransitions
        {
            get => _anyStateTransitions;
            set
            {
                _stateMachine.anyStateTransitions =
                    value.Select(transition => transition.GetTransition<AnimatorStateTransition>()).ToArray();
                _anyStateTransitions = value;
            }
        }

        public CatBakeLayer(string name = "Empty")
        {
            _stateMachine = new AnimatorStateMachine();
            _layer = new AnimatorControllerLayer
            {
                name = name,
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = _stateMachine
            };
        }
        
        public ICatState AddState(string name, Motion motion = null, Vector3? position = null)
        {
            position ??= new Vector3(300, _layer.stateMachine.states.Length * 120);
            
            var aState = _stateMachine.AddState(name, position ?? Vector3.zero);
            aState.motion = motion;
            var state = new CatBakeState(aState);
            return state;
        }

        public T GetLayer<T>() where T : class
        {
            return _layer as T;
        }
    }
}