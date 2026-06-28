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

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.physBones
{
    public class ComponentTransferTools : EditorWindow
    {
        private readonly List<TransferInfo> transferList = new();

        private GameObject referenceRoot;

        private Vector2 scroll;
        private GameObject targetRoot;

        private void OnGUI()
        {
            GUILayout.Space(5);

            referenceRoot = (GameObject)EditorGUILayout.ObjectField(
                "参考对象",
                referenceRoot,
                typeof(GameObject),
                true);

            targetRoot = (GameObject)EditorGUILayout.ObjectField(
                "迁移对象",
                targetRoot,
                typeof(GameObject),
                true);

            GUILayout.Space(10);

            GUI.enabled = referenceRoot != null && targetRoot != null;

            if (GUILayout.Button("检查", GUILayout.Height(30))) CheckComponents();

            GUI.enabled = true;

            GUILayout.Space(10);

            EditorGUILayout.LabelField($"待迁移组件：{transferList.Count}");

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var item in transferList)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField("路径", item.path);
                EditorGUILayout.LabelField("目标", item.dstGO.name);
                EditorGUILayout.LabelField("组件", item.component.GetType().Name);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            GUI.enabled = transferList.Count > 0;

            if (GUILayout.Button("执行迁移", GUILayout.Height(35))) ExecuteTransfer();

            GUI.enabled = true;
        }

        [MenuItem("CatTools/组件迁移工具")]
        public static void Open()
        {
            GetWindow<ComponentTransferTools>("组件迁移工具");
        }

        private void CheckComponents()
        {
            transferList.Clear();

            foreach (var child in referenceRoot.GetComponentsInChildren<Transform>(true))
            {
                var path = GetRelativePath(referenceRoot.transform, child);

                var dst = FindChildByPath(targetRoot.transform, path);

                if (dst == null)
                    continue;

                var components = child.GetComponents<Component>();

                foreach (var comp in components)
                {
                    if (comp == null)
                        continue;

                    if (comp is Transform)
                        continue;

                    if (dst.GetComponent(comp.GetType()) != null)
                        continue;

                    transferList.Add(new TransferInfo
                    {
                        srcGO = child.gameObject,
                        dstGO = dst.gameObject,
                        component = comp,
                        path = path
                    });
                }
            }

            Debug.Log($"检查完成，共 {transferList.Count} 个组件待迁移");
        }

        private void ExecuteTransfer()
        {
            Undo.IncrementCurrentGroup();

            foreach (var info in transferList)
            {
                ComponentUtility.CopyComponent(info.component);
                ComponentUtility.PasteComponentAsNew(info.dstGO);
            }

            Debug.Log($"迁移完成，共复制 {transferList.Count} 个组件");

            transferList.Clear();
        }

        private static string GetRelativePath(Transform root, Transform current)
        {
            if (current == root)
                return "";

            List<string> list = new();

            while (current != root)
            {
                list.Add(current.name);
                current = current.parent;
            }

            list.Reverse();

            return string.Join("/", list);
        }

        private static Transform FindChildByPath(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;

            return root.Find(path);
        }

        private class TransferInfo
        {
            public Component component;
            public GameObject dstGO;
            public string path;
            public GameObject srcGO;
        }
    }
}
#endif