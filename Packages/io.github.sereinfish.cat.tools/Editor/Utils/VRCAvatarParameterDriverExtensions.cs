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

using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace io.github.sereinfish.cat.tools.editor.utils
{
    public static class VRCAvatarParameterDriverExtensions
    {
        public static void AddParameterDriver(this VRCAvatarParameterDriver driver, VRC_AvatarParameterDriver.Parameter parameter)
        {
            driver.parameters.Add(parameter);
        }

        public static void AddParameterDriverSet(this VRCAvatarParameterDriver driver, string name, float value)
        {
            driver.AddParameterDriver(new VRC_AvatarParameterDriver.Parameter
            {
                name = name,
                value = value,
                type = VRC_AvatarParameterDriver.ChangeType.Set
            });
        }
        
        public static void AddParameterDriverAdd(this VRCAvatarParameterDriver driver, string name, float value)
        {
            driver.AddParameterDriver(new VRC_AvatarParameterDriver.Parameter
            {
                name = name,
                value = value,
                type = VRC_AvatarParameterDriver.ChangeType.Add
            });
        }
        
        public static void AddParameterDriverCopy(this VRCAvatarParameterDriver driver, string name, string source)
        {
            driver.AddParameterDriver(new VRC_AvatarParameterDriver.Parameter
            {
                name = name,
                source = source,
                type = VRC_AvatarParameterDriver.ChangeType.Copy
            });
        }
        
        public static void AddParameterDriverRandom(this VRCAvatarParameterDriver driver, string name, float min, float max)
        {
            driver.AddParameterDriver(new VRC_AvatarParameterDriver.Parameter
            {
                name = name,
                valueMin = min,
                valueMax = max,
                type = VRC_AvatarParameterDriver.ChangeType.Random
            });
        }
        
        public static void AddParameterDriverSet(this VRCAvatarParameterDriver driver, string name, bool value)
        {
            driver.AddParameterDriverSet(name, value ? 1f : 0f);
        }
    }
}