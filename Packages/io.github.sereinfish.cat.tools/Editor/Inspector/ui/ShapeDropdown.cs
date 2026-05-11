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
using UnityEditor.IMGUI.Controls;

namespace io.github.sereinfish.cat.tools.editor.inspector.ui
{
    internal class ShapeDropdown : AdvancedDropdown
    {
        private readonly string[] _options;
        private readonly Action<string> _onSelected;

        public ShapeDropdown(string[] options, Action<string> onSelected)
            : base(new AdvancedDropdownState())
        {
            _options = options;
            _onSelected = onSelected;

            minimumSize = new UnityEngine.Vector2(300, 400);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("BlendShapes");

            foreach (var option in _options)
            {
                root.AddChild(new AdvancedDropdownItem(option));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            _onSelected?.Invoke(item.name);
        }
    }
}