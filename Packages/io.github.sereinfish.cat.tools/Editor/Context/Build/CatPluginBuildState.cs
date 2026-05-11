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
using JetBrains.Annotations;
using nadena.dev.ndmf.animator;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context.build
{
    public class CatPluginBuildState: ICatState
    {
        private readonly VirtualState _state;
        private readonly CloneContext _cloneContext;
        
        public AnimationClip Motion { set => _state.Motion = _cloneContext.Clone(value); }
        public string Name { get => _state.Name; set => _state.Name = value; }
        public bool WriteDefaultValues { get => _state.WriteDefaultValues; set => _state.WriteDefaultValues = value; }
        
        private ImmutableList<ICatStateTransition> _transitions = ImmutableList<ICatStateTransition>.Empty;
        public ImmutableList<ICatStateTransition> Transitions
        {
            get => _transitions;
            set
            {
                _state.Transitions = value.Select(transition => transition.GetTransition<VirtualStateTransition>()).ToImmutableList();
                _transitions = value;
            }
        }

        public ImmutableList<StateMachineBehaviour> Behaviours
        {
            get => _state.Behaviours;
            set => _state.Behaviours = value;
        }

        public CatPluginBuildState(VirtualState state, CloneContext context)
        {
            _state = state;
            _cloneContext = context;
        }

        public T GetState<T>() where T : class
        {
            return _state as T;
        }
    }
}