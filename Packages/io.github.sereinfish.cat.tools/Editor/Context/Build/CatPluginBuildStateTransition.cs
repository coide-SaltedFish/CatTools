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
using UnityEditor.Animations;

namespace io.github.sereinfish.cat.tools.editor.context.build
{
    public class CatPluginBuildStateTransition: ICatStateTransition
    {
        private readonly VirtualStateTransition _transition = VirtualStateTransition.Create();
        
        public ImmutableList<AnimatorCondition> Conditions { get => _transition.Conditions; set => _transition.Conditions = value; }
        public float? ExitTime { get => _transition.ExitTime; set => _transition.ExitTime = value; }
        public float Duration { get => _transition.Duration; set => _transition.Duration = value; }
        public bool CanTransitionToSelf { get => _transition.CanTransitionToSelf; set => _transition.CanTransitionToSelf = value; }
        public bool HasFixedDuration { get => _transition.HasFixedDuration; set => _transition.HasFixedDuration = value; }
        public TransitionInterruptionSource InterruptionSource { get => _transition.InterruptionSource; set => _transition.InterruptionSource = value; }
        public float Offset { get => _transition.Offset; set => _transition.Offset = value; }
        public bool OrderedInterruption { get => _transition.OrderedInterruption; set => _transition.OrderedInterruption = value; }
        public bool Mute { get => _transition.Mute; set => _transition.Mute = value; }
        public bool Solo { get => _transition.Solo; set => _transition.Solo = value; }

        public void SetDestination(ICatState state)
        {
            _transition.SetDestination(state.GetState<VirtualState>());
        }

        public T GetTransition<T>() where T : class
        {
            return _transition as T;
        }

        public static ICatStateTransition Create()
        {
            return new CatPluginBuildStateTransition();
        }
    }
}