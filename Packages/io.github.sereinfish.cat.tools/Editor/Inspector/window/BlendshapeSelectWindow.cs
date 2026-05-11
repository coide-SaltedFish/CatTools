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
using System.Collections.Generic;
using System.Linq;
using io.github.sereinfish.cat.tools.Components;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector.window
{
    internal sealed class ShapeChangeInfoPickerWindow : EditorWindow
    {
        private GameObject _avatarRoot;

        private ConditionalBlendShapeSetter.ShapeChangeType _defaultChangeType;
        private float _defaultValue;
        private Action _onClosed;

        private Action<ConditionalBlendShapeSetter.ShapeChangeInfo> _onSelected;
        private SearchField _searchField;
        private Mesh _singleMesh;
        private BlendshapeTree _tree;

        private void OnDestroy()
        {
            _tree = null;
            _searchField = null;
            _onSelected = null;

            _onClosed?.Invoke();
            _onClosed = null;
        }

        private void OnGUI()
        {
            if (_tree == null)
            {
                _searchField = new SearchField();

                if (_singleMesh != null)
                {
                    _tree = new BlendshapeTree(_singleMesh, new TreeViewState(), _defaultChangeType, _defaultValue);
                }
                else if (_avatarRoot != null)
                {
                    _tree = new BlendshapeTree(_avatarRoot, new TreeViewState(), _defaultChangeType, _defaultValue);
                }
                else
                {
                    Close();
                    return;
                }

                _tree.OnSingleClick = info => { _onSelected?.Invoke(info); };

                _tree.OnDoubleClick = info =>
                {
                    _onSelected?.Invoke(info);
                    // Close();
                };

                _tree.Reload();
                _tree.SetExpanded(0, true);
            }

            var sfRect = GUILayoutUtility.GetRect(
                1, 99999,
                EditorGUIUtility.singleLineHeight,
                EditorGUIUtility.singleLineHeight,
                GUILayout.ExpandWidth(true));

            _tree.searchString = _searchField.OnGUI(sfRect, _tree.searchString);

            var remaining = GUILayoutUtility.GetRect(
                1, 99999,
                EditorGUIUtility.singleLineHeight * 2,
                99999999,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            _tree.OnGUI(remaining);
        }

        public static ShapeChangeInfoPickerWindow Open(
            GameObject avatarRoot,
            Action<ConditionalBlendShapeSetter.ShapeChangeInfo> onSelected,
            Action onClosed = null,
            ConditionalBlendShapeSetter.ShapeChangeType defaultChangeType = ConditionalBlendShapeSetter.ShapeChangeType.Set,
            float defaultValue = 100f)
        {
            var wnd = CreateInstance<ShapeChangeInfoPickerWindow>();
            wnd.Initialize(avatarRoot, null, onSelected, onClosed, defaultChangeType, defaultValue);
            wnd.ShowUtility();
            return wnd;
        }

        public static ShapeChangeInfoPickerWindow Open(
            Mesh mesh,
            Action<ConditionalBlendShapeSetter.ShapeChangeInfo> onSelected,
            Action onClosed = null,
            ConditionalBlendShapeSetter.ShapeChangeType defaultChangeType = ConditionalBlendShapeSetter.ShapeChangeType.Set,
            float defaultValue = 100f)
        {
            var wnd = CreateInstance<ShapeChangeInfoPickerWindow>();
            wnd.Initialize(null, mesh, onSelected, onClosed, defaultChangeType, defaultValue);
            wnd.ShowUtility();
            return wnd;
        }

        private void Initialize(
            GameObject avatarRoot,
            Mesh singleMesh,
            Action<ConditionalBlendShapeSetter.ShapeChangeInfo> onSelected,
            Action onClosed,
            ConditionalBlendShapeSetter.ShapeChangeType defaultChangeType,
            float defaultValue)
        {
            _avatarRoot = avatarRoot;
            _singleMesh = singleMesh;
            _onSelected = onSelected;
            _onClosed = onClosed;
            _defaultChangeType = defaultChangeType;
            _defaultValue = defaultValue;

            titleContent = new GUIContent("Select blendshapes");
        }

        public void ClosePicker()
        {
            Close();
        }
    }

    internal sealed class BlendshapeTree : TreeView
    {
        private readonly GameObject _avatarRoot;
        private readonly ConditionalBlendShapeSetter.ShapeChangeType _defaultChangeType;
        private readonly float _defaultValue;

        private readonly Mesh _singleMesh;

        private List<ConditionalBlendShapeSetter.ShapeChangeInfo?> _candidateInfos;
        internal Action<ConditionalBlendShapeSetter.ShapeChangeInfo> OnDoubleClick;

        internal Action<ConditionalBlendShapeSetter.ShapeChangeInfo> OnSingleClick;

        public BlendshapeTree(GameObject avatarRoot, TreeViewState state, ConditionalBlendShapeSetter.ShapeChangeType defaultChangeType,
            float defaultValue)
            : base(state)
        {
            _avatarRoot = avatarRoot;
            _defaultChangeType = defaultChangeType;
            _defaultValue = defaultValue;
        }

        public BlendshapeTree(Mesh mesh, TreeViewState state, ConditionalBlendShapeSetter.ShapeChangeType defaultChangeType, float defaultValue)
            : base(state)
        {
            _singleMesh = mesh;
            _defaultChangeType = defaultChangeType;
            _defaultValue = defaultValue;
        }

        // protected override void SingleClickedItem(int id)
        // {
        //     if (id < 0 || id >= _candidateInfos.Count)
        //         return;
        //
        //     var binding = _candidateInfos[id];
        //     if (binding.HasValue)
        //         OnSingleClick?.Invoke(binding.Value);
        // }

        protected override void DoubleClickedItem(int id)
        {
            if (id < 0 || id >= _candidateInfos.Count)
                return;

            var binding = _candidateInfos[id];
            if (binding.HasValue)
                OnDoubleClick?.Invoke(binding.Value);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            _candidateInfos = new List<ConditionalBlendShapeSetter.ShapeChangeInfo?>();
            _candidateInfos.Add(null);

            var allItems = new List<TreeViewItem>();
            var createdDepth = 0;
            var objectDisplayNames = new List<string>();

            if (_avatarRoot != null)
            {
                WalkTree(_avatarRoot, allItems, objectDisplayNames, ref createdDepth);
            }
            else if (_singleMesh != null)
            {
                CreateBlendshapes(allItems, _avatarRoot, _singleMesh, null, 0);
            }

            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        private void WalkTree(GameObject node, List<TreeViewItem> items, List<string> objectDisplayNames,
            ref int createdDepth)
        {
            objectDisplayNames.Add(node.name);

            var smr = node.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null && smr.sharedMesh.blendShapeCount > 0)
            {
                while (createdDepth < objectDisplayNames.Count)
                {
                    items.Add(new TreeViewItem
                    {
                        id = _candidateInfos.Count,
                        depth = createdDepth,
                        displayName = objectDisplayNames[createdDepth]
                    });
                    _candidateInfos.Add(null);
                    createdDepth++;
                }

                CreateBlendshapes(smr, items, ref createdDepth);
            }

            foreach (Transform child in node.transform)
                WalkTree(child.gameObject, items, objectDisplayNames, ref createdDepth);

            objectDisplayNames.RemoveAt(objectDisplayNames.Count - 1);
            createdDepth = Math.Min(createdDepth, objectDisplayNames.Count);
        }

        private void CreateBlendshapes(SkinnedMeshRenderer smr, List<TreeViewItem> items, ref int createdDepth)
        {
            items.Add(new TreeViewItem
            {
                id = _candidateInfos.Count,
                depth = createdDepth,
                displayName = "BlendShapes"
            });
            _candidateInfos.Add(null);
            createdDepth++;

            var path = GetRelativePath(_avatarRoot != null ? _avatarRoot.transform : null, smr.transform);
            var mesh = smr.sharedMesh;

            CreateBlendshapes(items, smr.gameObject, mesh, path, createdDepth);

            createdDepth--;
        }

        private void CreateBlendshapes(List<TreeViewItem> items, GameObject target, Mesh mesh, string path, int createdDepth)
        {
            var infos = Enumerable.Range(0, mesh.blendShapeCount)
                .Select(n => new ConditionalBlendShapeSetter.ShapeChangeInfo
                {
                    target = target.transform,
                    shapeName = mesh.GetBlendShapeName(n),
                    changeType = _defaultChangeType,
                    value = _defaultValue
                });

            foreach (var info in infos)
            {
                items.Add(new OfferItem
                {
                    id = _candidateInfos.Count,
                    depth = createdDepth,
                    displayName = info.shapeName,
                    info = info,
                    referencePath = path
                });
                _candidateInfos.Add(info);
            }
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (target == null)
                return string.Empty;

            if (root == null)
                return target.name;

            if (root == target)
                return root.name;

            var stack = new Stack<string>();
            var current = target;

            while (current != null && current != root)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            if (current != root)
                return target.name;

            return string.Join("/", stack.ToArray());
        }

        internal class OfferItem : TreeViewItem
        {
            public ConditionalBlendShapeSetter.ShapeChangeInfo info;
            public string referencePath;
        }
    }
}