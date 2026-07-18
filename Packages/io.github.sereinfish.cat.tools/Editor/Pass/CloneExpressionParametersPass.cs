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

using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace io.github.sereinfish.cat.tools.editor.pass
{
    public class CloneExpressionParametersPass : Pass<CloneExpressionParametersPass>
    {
        protected override void Execute(BuildContext context)
        {
            var original = context.VRChatAvatarDescriptor().expressionParameters;
            if (original == null)
            {
                context.VRChatAvatarDescriptor().expressionParameters =
                    ScriptableObject.CreateInstance<VRCExpressionParameters>();
                return;
            }

            var cloneExpr = Object.Instantiate(original);
            cloneExpr.name = original.name + "_NDMF_CT_Clone";

            context.VRChatAvatarDescriptor().expressionParameters = cloneExpr;
        }
    }
}