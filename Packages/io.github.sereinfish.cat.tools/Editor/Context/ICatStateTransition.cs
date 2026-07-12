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
using UnityEditor.Animations;

namespace io.github.sereinfish.cat.tools.editor.context
{
    public interface ICatStateTransition
    {
        public ImmutableList<AnimatorCondition> Conditions { get; set; }
        
        public float? ExitTime { get; set; }
        public float Duration { get; set; }
        public bool CanTransitionToSelf { get; set; }
        public bool HasFixedDuration { get; set; }
        
        public TransitionInterruptionSource InterruptionSource { get; set; }
        
        public float Offset { get; set; }
        
        public bool OrderedInterruption { get; set; }
        
        public bool Mute { get; set; }
        public bool Solo { get; set; }
        
        public void SetDestination(ICatState state);

        public void SetExitDestination();

        public static ICatStateTransition Create(ICatContext context)
        {
            return context.CreateTransition();
        }

        public T GetTransition<T>() where T : class;
    }
}