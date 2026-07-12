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
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.animator.builder
{
    public class AnimationCurveBuilder
    {
        private readonly AnimationCurve _curve = new AnimationCurve();
        private float _frameRate;

        public AnimationCurveBuilder(){}

        public AnimationCurveBuilder(float frameRate)
        {
            _frameRate = frameRate;
        }
        
        public AnimationCurveBuilder AddKey(Keyframe keyframe)
        {
            _curve.AddKey(keyframe);
            return this;
        }
        
        public AnimationCurveBuilder AddKey(float time, float value, float inTangent = 0, float outTangent = 0, float inWeight = 0, float outWeight = 0)
        {
            var ft = time / _frameRate;
            
            _curve.AddKey(new Keyframe(ft, value, inTangent, outTangent, inWeight, outWeight));
            return this;
        }
        
        public AnimationCurveBuilder SetFps(int fps)
        {
            _frameRate = fps;
            return this;
        }
        
        public AnimationCurveBuilder Run(Action<AnimationCurve> action)
        {
            action(_curve);
            return this;
        }
        
        public AnimationCurve Build()
        {
            return _curve;
        }
    }
}