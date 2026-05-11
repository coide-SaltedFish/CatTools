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

namespace io.github.sereinfish.cat.tools.editor.context.bake
{
    public class CatBakeStateTransition: ICatStateTransition
    {
        private readonly AnimatorStateTransition _transition = new();
        
        public ImmutableList<AnimatorCondition> Conditions { get => _transition.conditions.ToImmutableList(); set => _transition.conditions = value.ToArray(); }
        public float? ExitTime { get => _transition.exitTime; set => _transition.exitTime = value ?? 0f; }
        public float Duration { get => _transition.duration; set => _transition.duration = value; }
        public bool CanTransitionToSelf { get => _transition.canTransitionToSelf; set => _transition.canTransitionToSelf = value; }
        public bool HasFixedDuration { get => _transition.hasFixedDuration; set => _transition.hasFixedDuration = value; }
        public TransitionInterruptionSource InterruptionSource { get => _transition.interruptionSource; set => _transition.interruptionSource = value; }
        public float Offset { get => _transition.offset; set => _transition.offset = value; }
        public bool OrderedInterruption { get => _transition.orderedInterruption; set => _transition.orderedInterruption = value; }
        public bool Mute { get => _transition.mute; set => _transition.mute = value; }
        public bool Solo { get => _transition.solo; set => _transition.solo = value; }
        
        public void SetDestination(ICatState state)
        {
            _transition.destinationState = state.GetState<AnimatorState>();
        }

        public T GetTransition<T>() where T : class
        {
            return _transition as T;
        }
    }
}