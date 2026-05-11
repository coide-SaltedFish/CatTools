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
using UnityEditor.Animations;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context.bake
{
    public class CatBakeStateMachine: ICatStateMachine
    {
        private readonly AnimatorStateMachine _stateMachine;

        private ICatState _defaultState;
        public ICatState DefaultState
        {
            get => _defaultState;
            set
            {
                _stateMachine.defaultState = value.GetState<AnimatorState>();
                _defaultState = value;
            }
        }

        public Vector3 EntryPosition { get => _stateMachine.entryPosition; set => _stateMachine.entryPosition = value; }
        public Vector3 AnyStatePosition { get => _stateMachine.anyStatePosition; set => _stateMachine.anyStatePosition = value; }
        
        private ImmutableList<ICatStateTransition> _transitions = ImmutableList<ICatStateTransition>.Empty;

        public ImmutableList<ICatStateTransition> AnyStateTransitions
        {
            get => _transitions;
            set
            {
                _stateMachine.anyStateTransitions =
                    value.Select(transition => transition.GetTransition<AnimatorStateTransition>()).ToArray();
                _transitions = value;
            }
        }

        public CatBakeStateMachine(AnimatorStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }
        
        public ICatState AddState(string name, Motion motion = null, Vector3? position = null)
        {
            var pos = position ?? Vector3.zero;
            
            var state = _stateMachine.AddState(name, pos);
            state.motion = motion;
            return new CatBakeState(state);
        }
    }
}