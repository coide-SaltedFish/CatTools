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
using JetBrains.Annotations;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.context
{
    public interface ICatState
    {
        [CanBeNull] public AnimationClip Motion { set; }
        public string Name { get; set; }
        public bool WriteDefaultValues { get; set; }
        
        public float CycleOffset { get; set; }
        public string CycleOffsetParameter { get; set; }
        public bool IKOnFeet { get; set; }
        public bool Mirror { get; set; }
        public string MirrorParameter { get; set; }
        public float Speed { get; set; }
        public string SpeedParameter { get; set; }
        public string Tag { get; set; }
        public string TimeParameter { get; set; }
        
        public ImmutableList<ICatStateTransition> Transitions { get; set; }
        public ImmutableList<StateMachineBehaviour> Behaviours { get; set; }

        public T GetState<T>() where T : class;

        public ICatState SetMotion(AnimationClip clip)
        {
            Motion = clip;
            return this;
        }
        public ICatState SetName(string name)
        {
            Name = name;
            return this;
        }
        public ICatState SetWriteDefaultValues(bool writeDefaultValues)
        {
            WriteDefaultValues = writeDefaultValues;
            return this;
        }
        public ICatState SetCycleOffset(float cycleOffset)
        {
            CycleOffset = cycleOffset;
            return this;
        }
        public ICatState SetCycleOffsetParameter(string cycleOffsetParameter)
        {
            CycleOffsetParameter = cycleOffsetParameter;
            return this;
        }
        public ICatState SetIKOnFeet(bool ikOnFeet)
        {
            IKOnFeet = ikOnFeet;
            return this;
        }
        public ICatState SetMirror(bool mirror)
        {
            Mirror = mirror;
            return this;
        }
        public ICatState SetMirrorParameter(string mirrorParameter)
        {
            MirrorParameter = mirrorParameter;
            return this;
        }
        public ICatState SetSpeed(float speed)
        {
            Speed = speed;
            return this;
        }
        public ICatState SetSpeedParameter(string speedParameter)
        {
            SpeedParameter = speedParameter;
            return this;
        }
        public ICatState SetTag(string tag)
        {
            Tag = tag;
            return this;
        }
        public ICatState SetTimeParameter(string timeParameter)
        {
            TimeParameter = timeParameter;
            return this;
        }
    }
}