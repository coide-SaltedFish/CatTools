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
    public class CatBakeState: ICatState
    {
        private readonly AnimatorState  _state;
        
        public AnimationClip Motion { get => _state.motion as AnimationClip; set => _state.motion = value; }
        public string Name { get => _state.name; set => _state.name = value; }
        public bool WriteDefaultValues { get => _state.writeDefaultValues; set => _state.writeDefaultValues = value; }
        
        private ImmutableList<ICatStateTransition> _transitions = ImmutableList<ICatStateTransition>.Empty;
        public ImmutableList<ICatStateTransition> Transitions
        {
            get => _transitions;
            set
            {
                _state.transitions = value.Select(transition => transition.GetTransition<AnimatorStateTransition>()).ToArray();
                _transitions = value;
            }
        }

        public ImmutableList<StateMachineBehaviour> Behaviours { get => _state.behaviours.ToImmutableList(); set => _state.behaviours = value.ToArray(); }
        
        public CatBakeState(AnimatorState state)
        {
            _state = state;
        }
        
        public T GetState<T>() where T : class
        {
            return _state as T;
        }
    }
}