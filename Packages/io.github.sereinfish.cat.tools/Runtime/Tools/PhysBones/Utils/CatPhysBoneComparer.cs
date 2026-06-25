#if UNITY_EDITOR
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

using System.Text;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace io.github.sereinfish.cat.tools.physBones.utils
{
    public static class CatPhysBoneComparer
    {
        public static bool AreEqual(this VRCPhysBone a, VRCPhysBone b)
        {
            var ret = a.AreEqual(b, out var info);
            var msg = new StringBuilder().Append("CatTools PhysBone Tool ->")
                .Append("(bone1: " + a.name)
                .Append("，bone2: " + b.name + ")")
                .Append(" -> " + info);
            Debug.Log(msg.ToString());
            return ret;
        }
        
        public static bool AreEqual(this VRCPhysBone a, VRCPhysBone b, out string differentPath)
        {
            differentPath = null;

            if (ReferenceEquals(a, b))
                return true;

            if (a == null || b == null)
                return false;

            var soA = new SerializedObject(a);
            var soB = new SerializedObject(b);
            soA.Update();
            soB.Update();

            var itA = soA.GetIterator();
            var itB = soB.GetIterator();

            var enterChildren = true;

            while (true)
            {
                var movedA = itA.NextVisible(enterChildren);
                var movedB = itB.NextVisible(enterChildren);

                if (movedA != movedB)
                {
                    differentPath = movedA ? itA.propertyPath : itB.propertyPath;
                    return false;
                }

                if (!movedA)
                    break;

                // 跳过脚本引用
                if (itA.propertyPath == "m_Script")
                {
                    enterChildren = false;
                    continue;
                }

                if (itA.propertyPath != itB.propertyPath)
                {
                    differentPath = itA.propertyPath;
                    return false;
                }

                if (itA.isArray && itA.arraySize != itB.arraySize)
                {
                    differentPath = itA.propertyPath + ".arraySize";
                    return false;
                }

                if (!PropertyValueEqual(itA, itB))
                {
                    differentPath = itA.propertyPath;
                    return false;
                }

                enterChildren = false;
            }

            return true;
        }

        private static bool PropertyValueEqual(SerializedProperty a, SerializedProperty b)
        {
            if (a.propertyType != b.propertyType)
                return false;

            switch (a.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return a.longValue == b.longValue;

                case SerializedPropertyType.Boolean:
                    return a.boolValue == b.boolValue;

                case SerializedPropertyType.Float:
                    return Mathf.Approximately(a.floatValue, b.floatValue);

                case SerializedPropertyType.String:
                    return a.stringValue == b.stringValue;

                case SerializedPropertyType.Color:
                    return a.colorValue == b.colorValue;

                case SerializedPropertyType.ObjectReference:
                    return a.objectReferenceInstanceIDValue == b.objectReferenceInstanceIDValue;

                case SerializedPropertyType.LayerMask:
                    return a.intValue == b.intValue;

                case SerializedPropertyType.Enum:
                    return a.enumValueIndex == b.enumValueIndex;

                case SerializedPropertyType.Vector2:
                    return a.vector2Value == b.vector2Value;

                case SerializedPropertyType.Vector3:
                    return a.vector3Value == b.vector3Value;

                case SerializedPropertyType.Vector4:
                    return a.vector4Value == b.vector4Value;

                case SerializedPropertyType.Rect:
                    return a.rectValue == b.rectValue;

                case SerializedPropertyType.Bounds:
                    return a.boundsValue == b.boundsValue;

                case SerializedPropertyType.Quaternion:
                    return a.quaternionValue == b.quaternionValue;

                case SerializedPropertyType.Vector2Int:
                    return a.vector2IntValue == b.vector2IntValue;

                case SerializedPropertyType.Vector3Int:
                    return a.vector3IntValue == b.vector3IntValue;

                case SerializedPropertyType.RectInt:
                    return a.rectIntValue.Equals(b.rectIntValue);

                case SerializedPropertyType.BoundsInt:
                    return a.boundsIntValue == b.boundsIntValue;

                case SerializedPropertyType.ManagedReference:
                    return a.managedReferenceId == b.managedReferenceId
                           && a.managedReferenceFullTypename == b.managedReferenceFullTypename;

                // 复杂类型 / 结构体 / 数组元素，子属性会在遍历时逐个比较
                case SerializedPropertyType.Generic:
                    return true;

                default:
                    return true;
            }
        }
    }
}
#endif