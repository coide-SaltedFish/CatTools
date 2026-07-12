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
        public float CycleOffset { get => _state.cycleOffset; set => _state.cycleOffset = value; }
        public string CycleOffsetParameter { get => _state.cycleOffsetParameter; set => _state.cycleOffsetParameter = value; }
        public bool IKOnFeet { get => _state.iKOnFeet; set => _state.iKOnFeet = value; }
        public bool Mirror { get => _state.mirror; set => _state.mirror = value; }
        public string MirrorParameter { get => _state.mirrorParameter; set => _state.mirrorParameter = value; }
        public float Speed { get => _state.speed; set => _state.speed = value; }
        public string SpeedParameter { get => _state.speedParameter; set => _state.speedParameter = value; }
        public string Tag { get => _state.tag; set => _state.tag = value; }
        public string TimeParameter { get => _state.timeParameter; set => _state.timeParameter = value; }

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