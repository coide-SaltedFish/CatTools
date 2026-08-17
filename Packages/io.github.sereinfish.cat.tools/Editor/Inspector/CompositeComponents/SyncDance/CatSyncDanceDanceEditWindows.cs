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
using System.Reflection;
using io.github.sereinfish.cat.tools.Components;
using io.github.sereinfish.cat.tools.editor.utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.editor.inspector
{
    public class CatSyncDanceDanceEditWindows : EditorWindow
    {
        private GameObject _gameObject;
        private SerializedObject _target;
        private SerializedProperty _dances;
        private SerializedProperty _syncParameterProp;
        private SerializedProperty _danceCategoriesProp;
        private Vector2 _scrollPos;
        private ReorderableList _list;

        /// <summary>显示下标（指向 _dances 数组）投影，用于视觉排序/筛选/搜索，不改变底层数组顺序</summary>
        private readonly List<int> _displayIndices = new();
        private readonly Dictionary<string, ItemList> _itemLists = new();
        private readonly Dictionary<int, DanceSize> _sizeCache = new();
        private bool _dirty;

        // 搜索 / 排序 / 筛选状态
        private string _search = "";
        private SortMode _sortMode = SortMode.None;
        private bool _sortDescending;
        private LoopFilter _loopFilter = LoopFilter.All;
        private int _categoryFilterIndex = -1; // -1 表示全部

        private string[] _syncParameterNames = Array.Empty<string>();
        private string[] _danceCategories = Array.Empty<string>();

        /// <summary>各标签文案复用对象，避免每行每帧重复分配 GUIContent</summary>
        private static readonly GUIContent LoopLabel = new("循环");
        private static readonly GUIContent SpeedLabel = new("速度:");
        private static readonly GUIContent MusicClipLabel = new("音乐剪辑");
        private static readonly GUIContent SyncParameterLabel = new("同步参数");
        private static readonly GUIContent ValueLabel = new("值:");
        private static readonly GUIContent AnimClipLabel = new("动画剪辑");
        private static readonly GUIContent DanceNameLabel = new("舞蹈名称");
        private static readonly GUIContent PathTypeLabel = new("动画路径类型");
        private static readonly GUIContent LocalIndexLabel = new("控制参数分配");
        private static readonly GUIContent CategoryLabel = new("类别");

        private static readonly string[] SortModeNames = { "不排序", "按舞蹈名", "按总大小", "按歌曲大小", "按动作大小" };
        private static readonly string[] LoopFilterNames = { "全部", "循环", "不循环" };

        // 分割线样式：2px 粗，上下各 4px 间隔
        private const float DividerThickness = 2f;
        private const float DividerGap = 4f;
        private const float DividerTotalHeight = DividerGap * 2f + DividerThickness; // 10f

        private enum SortMode
        {
            None = 0,
            Name = 1,
            TotalSize = 2,
            SongSize = 3,
            ActionSize = 4
        }

        private enum LoopFilter
        {
            All = 0,
            Loop = 1,
            NoLoop = 2
        }

        private CatSyncDanceDanceEditWindows()
        {
        }

        private void OnGUI()
        {
            DrawObjectSelector();
            if (_target == null)
            {
                if (_gameObject != null)
                {
                    EditorGUILayout.HelpBox("所选对象上没有 CatSyncDance 组件", MessageType.Warning);
                }
                return;
            }

            _target.Update();

            RefreshCaches();

            if (_dirty)
            {
                _sizeCache.Clear();
                _dirty = false;
            }

            DrawOperationArea();

            RebuildDisplayIndices();
            if (_list != null) _list.draggable = IsDefaultOrder();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _list?.DoLayoutList();
            EditorGUILayout.EndScrollView();

            _target.ApplyModifiedProperties();
        }

        private void DrawObjectSelector()
        {
            var newGo = (GameObject)EditorGUILayout.ObjectField("对象", _gameObject, typeof(GameObject), true);
            if (newGo != _gameObject)
            {
                SetGameObject(newGo);
            }
        }

        private void RefreshCaches()
        {
            _syncParameterNames = GetSyncParameterNames();
            _danceCategories = GetDanceCategories();
            if (_categoryFilterIndex >= _danceCategories.Length) _categoryFilterIndex = -1;
        }

        private void DrawOperationArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 第一层：操作控件
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新"))
            {
                _sizeCache.Clear();
                _dirty = false;
                Repaint();
            }
            if (GUILayout.Button("清除筛选"))
            {
                _search = "";
                _sortMode = SortMode.None;
                _sortDescending = false;
                _loopFilter = LoopFilter.All;
                _categoryFilterIndex = -1;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _search = EditorGUILayout.TextField("搜索", _search);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _sortMode = (SortMode)EditorGUILayout.Popup("排序", (int)_sortMode, SortModeNames);
            if (GUILayout.Button(_sortDescending ? "降序 ↓" : "升序 ↑", GUILayout.Width(64f)))
            {
                _sortDescending = !_sortDescending;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _loopFilter = (LoopFilter)EditorGUILayout.Popup("循环", (int)_loopFilter, LoopFilterNames);
            var categoryOptions = new string[_danceCategories.Length + 1];
            categoryOptions[0] = "全部";
            for (var i = 0; i < _danceCategories.Length; i++)
            {
                categoryOptions[i + 1] = _danceCategories[i];
            }
            var categoryIndex = _categoryFilterIndex + 1;
            var newCategoryIndex = EditorGUILayout.Popup("类别", categoryIndex, categoryOptions);
            if (newCategoryIndex != categoryIndex)
            {
                _categoryFilterIndex = newCategoryIndex - 1;
            }
            EditorGUILayout.EndHorizontal();

            // 第二层：信息（统计）
            DrawStatistics();

            EditorGUILayout.EndVertical();
        }

        private void DrawStatistics()
        {
            // 按文件对象去重：同一个音频 / 动画只加一次
            var countedAudio = new HashSet<AudioClip>();
            var countedAnim = new HashSet<AnimationClip>();
            long songOriginal = 0, songImported = 0, action = 0;
            var songCount = 0;
            var songImportedMissing = false;
            var actionMissing = false;

            for (var i = 0; i < _dances.arraySize; i++)
            {
                var prop = _dances.GetArrayElementAtIndex(i);

                // 歌曲
                var music = prop.FindPropertyRelative("musicClip").objectReferenceValue as AudioClip;
                if (music != null)
                {
                    songCount++;
                    if (countedAudio.Add(music))
                    {
                        var size = ComputeDanceSize(i);
                        if (size.SongOriginal > 0) songOriginal += size.SongOriginal;
                        if (size.SongImported >= 0) songImported += size.SongImported;
                        else songImportedMissing = true;
                    }
                }

                // 动作
                var clips = prop.FindPropertyRelative("clip");
                for (var j = 0; j < clips.arraySize; j++)
                {
                    var clip = clips.GetArrayElementAtIndex(j).objectReferenceValue as AnimationClip;
                    if (clip == null || !countedAnim.Add(clip)) continue;
                    var clipSize = GetClipInspectorSize(clip);
                    if (clipSize >= 0) action += clipSize;
                    else actionMissing = true;
                }
            }
            var total = songImported + action;

            EditorGUILayout.LabelField("统计信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("歌曲数量", $"{songCount}（舞蹈共 {_dances.arraySize} 个）");
            EditorGUILayout.LabelField("总大小", FormatBytes(total));
            EditorGUILayout.LabelField("歌曲大小", $"Original Size: {FormatBytes(songOriginal)}   Imported Size: {(songImportedMissing && songImported == 0 ? "-" : FormatBytes(songImported))}");
            EditorGUILayout.LabelField("动作大小", FormatBytes(action));
            if (songImportedMissing || actionMissing)
            {
                EditorGUILayout.LabelField("", "（部分资源大小无法获取，已按 0 计入）", EditorStyles.miniLabel);
            }
        }

        private void RebuildDisplayIndices()
        {
            var selectedArrayIndex = GetSelectedArrayIndex();

            _displayIndices.Clear();
            var count = _dances == null ? 0 : _dances.arraySize;
            for (var i = 0; i < count; i++)
            {
                if (MatchesFilter(i)) _displayIndices.Add(i);
            }

            if (_sortMode != SortMode.None || !string.IsNullOrEmpty(_search))
            {
                if (_sortMode == SortMode.TotalSize || _sortMode == SortMode.SongSize || _sortMode == SortMode.ActionSize)
                {
                    for (var i = 0; i < _displayIndices.Count; i++)
                    {
                        ComputeDanceSize(_displayIndices[i]);
                    }
                }
                _displayIndices.Sort(CompareDisplayIndices);
            }

            if (selectedArrayIndex >= 0 && _list != null)
            {
                var idx = _displayIndices.IndexOf(selectedArrayIndex);
                if (idx >= 0) _list.index = idx;
            }
            if (_list != null && _list.index >= _displayIndices.Count)
            {
                _list.index = _displayIndices.Count - 1;
            }
        }

        private int CompareDisplayIndices(int a, int b)
        {
            // 搜索时优先按匹配等级排序：danceName > 歌曲名 > 动作名
            if (!string.IsNullOrEmpty(_search))
            {
                var rankA = GetSearchMatchRank(_dances.GetArrayElementAtIndex(a));
                var rankB = GetSearchMatchRank(_dances.GetArrayElementAtIndex(b));
                var rankCmp = rankA.CompareTo(rankB);
                if (rankCmp != 0) return rankCmp;
            }

            int cmp;
            switch (_sortMode)
            {
                case SortMode.Name:
                    cmp = string.Compare(GetDanceName(a), GetDanceName(b), StringComparison.OrdinalIgnoreCase);
                    break;
                case SortMode.TotalSize:
                    cmp = ComputeDanceSize(a).Total.CompareTo(ComputeDanceSize(b).Total);
                    break;
                case SortMode.SongSize:
                    cmp = ComputeDanceSize(a).SongImported.CompareTo(ComputeDanceSize(b).SongImported);
                    break;
                case SortMode.ActionSize:
                    cmp = ComputeDanceSize(a).Action.CompareTo(ComputeDanceSize(b).Action);
                    break;
                default:
                    cmp = a.CompareTo(b);
                    break;
            }
            return _sortDescending ? -cmp : cmp;
        }

        private bool MatchesFilter(int index)
        {
            var prop = _dances.GetArrayElementAtIndex(index);

            if (_loopFilter != LoopFilter.All)
            {
                var loop = prop.FindPropertyRelative("loop").boolValue;
                if (_loopFilter == LoopFilter.Loop && !loop) return false;
                if (_loopFilter == LoopFilter.NoLoop && loop) return false;
            }

            if (_categoryFilterIndex >= 0 && _categoryFilterIndex < _danceCategories.Length)
            {
                if (!ContainsCategory(prop.FindPropertyRelative("categories"), _danceCategories[_categoryFilterIndex])) return false;
            }

            if (!string.IsNullOrEmpty(_search) && GetSearchMatchRank(prop) < 0) return false;

            return true;
        }

        private int GetSearchMatchRank(SerializedProperty prop)
        {
            var q = _search;
            if (string.IsNullOrEmpty(q)) return -1;

            var danceName = prop.FindPropertyRelative("danceName").stringValue;
            if (!string.IsNullOrEmpty(danceName) && danceName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return 0;

            var music = prop.FindPropertyRelative("musicClip").objectReferenceValue as AudioClip;
            if (music != null && music.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return 1;

            var clips = prop.FindPropertyRelative("clip");
            for (var i = 0; i < clips.arraySize; i++)
            {
                var clip = clips.GetArrayElementAtIndex(i).objectReferenceValue as AnimationClip;
                if (clip != null && clip.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            }
            return -1;
        }

        private float ElementHeightCallback(int displayIndex)
        {
            var arrayIndex = GetArrayIndex(displayIndex);
            if (arrayIndex < 0) return EditorGUIUtility.singleLineHeight + 4f;

            var lineH = EditorGUIUtility.singleLineHeight + 2f;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var prop = _dances.GetArrayElementAtIndex(arrayIndex);
            var itemList = GetOrCreateItemList(arrayIndex, prop);

            // 舞蹈名称、路径类型+localIndex、音乐/循环/速度、类别 共 4 行，加 2 个内嵌列表与 1 条分割线
            return lineH * 4 + spacing * 5 + itemList.ClipHeight + itemList.ParameterHeight + DividerTotalHeight;
        }

        private void DrawElement(Rect rect, int displayIndex, bool isActive, bool isFocused)
        {
            var arrayIndex = GetArrayIndex(displayIndex);
            if (arrayIndex < 0) return;

            // 视口剔除：完全落在滚动可视区域之外的条目跳过绘制
            if (position.height > 0f)
            {
                var visibleBottom = _scrollPos.y + position.height;
                if (rect.y + rect.height <= _scrollPos.y || rect.y >= visibleBottom) return;
            }

            var lineH = EditorGUIUtility.singleLineHeight + 2f;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var x = rect.x;
            var y = rect.y;

            var prop = _dances.GetArrayElementAtIndex(arrayIndex);
            var danceName = prop.FindPropertyRelative("danceName");
            var pathType = prop.FindPropertyRelative("pathType");
            var musicClip = prop.FindPropertyRelative("musicClip");
            var loop = prop.FindPropertyRelative("loop");
            var speed = prop.FindPropertyRelative("speed");
            var localIndexProp = prop.FindPropertyRelative("localIndex");
            var categoriesProp = prop.FindPropertyRelative("categories");

            var itemList = GetOrCreateItemList(arrayIndex, prop);

            // 1. 舞蹈名称
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(x, y, rect.width, lineH), danceName, DanceNameLabel);
            if (EditorGUI.EndChangeCheck())
            {
                if (DanceNameCheck(arrayIndex, danceName.stringValue).Not())
                {
                    danceName.stringValue = GetUniqueDanceName(arrayIndex, danceName.stringValue);
                }
            }
            y += lineH + spacing;

            // 2. 路径类型 + localIndex
            var pathTypeWidth = rect.width * 0.5f;
            EditorGUI.PropertyField(new Rect(x, y, pathTypeWidth, lineH), pathType, PathTypeLabel);
            DrawLocalIndex(new Rect(x + pathTypeWidth + 5f, y, rect.width - pathTypeWidth - 5f, lineH), arrayIndex, localIndexProp);
            y += lineH + spacing;

            // 3. 动画剪辑列表
            EditorGUI.BeginChangeCheck();
            itemList.ClipList.DoList(new Rect(x, y, rect.width, lineH));
            if (EditorGUI.EndChangeCheck()) _dirty = true;
            y += itemList.ClipHeight + spacing;

            // 4. 音乐剪辑、循环、速度
            var loopLabelWidth = EditorStyles.label.CalcSize(LoopLabel).x;
            var speedLabelWidth = EditorStyles.label.CalcSize(SpeedLabel).x;
            var toggleWidth = EditorGUIUtility.singleLineHeight;
            const float speedInputWidth = 40f;
            var musicClipLabelWidth = EditorStyles.label.CalcSize(MusicClipLabel).x;
            var sizeTextWidth = rect.width * 0.2f;
            var loopSpeedWidth = loopLabelWidth + 2f + toggleWidth + 5f + speedLabelWidth + 2f + speedInputWidth;
            var musicClipFieldWidth = Mathf.Max(0f, rect.width - musicClipLabelWidth - 2f - sizeTextWidth - 4f - loopSpeedWidth);
            var fieldX = x;
            EditorGUI.LabelField(new Rect(fieldX, y, musicClipLabelWidth, lineH), MusicClipLabel);
            fieldX += musicClipLabelWidth + 2f;
            EditorGUI.BeginChangeCheck();
            EditorGUI.ObjectField(new Rect(fieldX, y, musicClipFieldWidth, lineH), musicClip, GUIContent.none);
            if (EditorGUI.EndChangeCheck()) _dirty = true;
            fieldX += musicClipFieldWidth + 4f;
            var musicClipObject = musicClip.objectReferenceValue as AudioClip;
            var musicSizeText = musicClipObject != null ? FormatBytes(ComputeDanceSize(arrayIndex).SongImported) : "-";
            EditorGUI.LabelField(new Rect(fieldX, y, sizeTextWidth, lineH), musicSizeText, EditorStyles.miniLabel);
            fieldX += sizeTextWidth;
            EditorGUI.LabelField(new Rect(fieldX, y, loopLabelWidth, lineH), LoopLabel);
            fieldX += loopLabelWidth + 2f;
            EditorGUI.BeginChangeCheck();
            var loopValue = EditorGUI.Toggle(new Rect(fieldX, y, toggleWidth, lineH), loop.boolValue);
            if (EditorGUI.EndChangeCheck()) loop.boolValue = loopValue;
            fieldX += toggleWidth + 5f;
            EditorGUI.LabelField(new Rect(fieldX, y, speedLabelWidth, lineH), SpeedLabel);
            fieldX += speedLabelWidth + 2f;
            EditorGUI.PropertyField(new Rect(fieldX, y, speedInputWidth, lineH), speed, GUIContent.none);
            y += lineH + spacing;

            // 5. 同步参数列表
            itemList.ParameterList.DoList(new Rect(x, y, rect.width, lineH));
            y += itemList.ParameterHeight + spacing;

            // 6. 类别
            DrawCategories(new Rect(x, y, rect.width, lineH), categoriesProp);

            // 分割线（上下各 4px 间隔，2px 粗）
            var dividerRect = new Rect(x, rect.yMax - DividerGap - DividerThickness, rect.width, DividerThickness);
            EditorGUI.DrawRect(dividerRect, new Color(0f, 0f, 0f, 0.22f));
        }

        private void DrawLocalIndex(Rect rect, int arrayIndex, SerializedProperty localIndexProp)
        {
            const float toggleWidth = 100f;
            var manual = localIndexProp.intValue != 0;

            EditorGUI.BeginChangeCheck();
            var newManual = EditorGUI.Toggle(new Rect(rect.x, rect.y, toggleWidth, rect.height), LocalIndexLabel, manual);
            if (EditorGUI.EndChangeCheck())
            {
                if (newManual) localIndexProp.intValue = GetUniqueLocalIndex(arrayIndex, 1);
                else localIndexProp.intValue = 0;
                manual = newManual;
            }

            var intRect = new Rect(rect.x + toggleWidth + 5f, rect.y, rect.width - toggleWidth - 5f, rect.height);
            if (manual)
            {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUI.IntField(intRect, localIndexProp.intValue);
                if (EditorGUI.EndChangeCheck())
                {
                    if (value <= 0)
                    {
                        Debug.LogWarning("localIndex 不能为 0 或负数，已恢复为自动");
                        localIndexProp.intValue = 0;
                    }
                    else
                    {
                        localIndexProp.intValue = GetUniqueLocalIndex(arrayIndex, value);
                    }
                }
            }
            else
            {
                EditorGUI.LabelField(intRect, "(自动)");
            }
        }

        private int GetUniqueLocalIndex(int arrayIndex, int desired)
        {
            var value = Mathf.Max(1, desired);
            while (LocalIndexUsedByOther(arrayIndex, value)) value++;
            if (value != desired)
            {
                Debug.LogWarning($"localIndex {desired} 已被占用，已改为 {value}");
            }
            return value;
        }

        private bool LocalIndexUsedByOther(int arrayIndex, int value)
        {
            if (value <= 0) return false;
            for (var i = 0; i < _dances.arraySize; i++)
            {
                if (i == arrayIndex) continue;
                if (_dances.GetArrayElementAtIndex(i).FindPropertyRelative("localIndex").intValue == value) return true;
            }
            return false;
        }

        private void DrawCategories(Rect rect, SerializedProperty categoriesProp)
        {
            var options = _danceCategories ?? Array.Empty<string>();
            if (options.Length == 0)
            {
                // 未配置类别时显示一个默认类别下拉框
                EditorGUI.Popup(rect, CategoryLabel.text, 0, new[] { "默认" });
                return;
            }
            if (options.Length > 32)
            {
                // 类别超过 32 个时降级为单选下拉框
                var currentIndex = GetCurrentCategoryIndex(categoriesProp, options);
                var newIndex = EditorGUI.Popup(rect, CategoryLabel.text, currentIndex, options);
                if (newIndex != currentIndex)
                {
                    categoriesProp.arraySize = 1;
                    categoriesProp.GetArrayElementAtIndex(0).stringValue = options[newIndex];
                }
                return;
            }

            var mask = 0;
            for (var i = 0; i < options.Length; i++)
            {
                if (ContainsCategory(categoriesProp, options[i])) mask |= 1 << i;
            }

            EditorGUI.BeginChangeCheck();
            var newMask = EditorGUI.MaskField(rect, CategoryLabel, mask, options);
            if (EditorGUI.EndChangeCheck())
            {
                var selected = new List<string>();
                for (var i = 0; i < options.Length; i++)
                {
                    if ((newMask & (1 << i)) != 0) selected.Add(options[i]);
                }
                categoriesProp.arraySize = selected.Count;
                for (var i = 0; i < selected.Count; i++)
                {
                    categoriesProp.GetArrayElementAtIndex(i).stringValue = selected[i];
                }
            }
        }

        private static int GetCurrentCategoryIndex(SerializedProperty categoriesProp, string[] options)
        {
            if (categoriesProp.arraySize > 0)
            {
                var name = categoriesProp.GetArrayElementAtIndex(0).stringValue;
                var index = Array.IndexOf(options, name);
                if (index >= 0) return index;
            }
            return 0;
        }

        private static bool ContainsCategory(SerializedProperty categoriesProp, string name)
        {
            for (var i = 0; i < categoriesProp.arraySize; i++)
            {
                if (categoriesProp.GetArrayElementAtIndex(i).stringValue == name) return true;
            }
            return false;
        }

        private string[] GetSyncParameterNames()
        {
            if (_syncParameterProp == null) return Array.Empty<string>();
            var count = _syncParameterProp.arraySize;
            var names = new string[count];
            for (var i = 0; i < count; i++)
            {
                names[i] = _syncParameterProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
            }
            return names;
        }

        private string[] GetDanceCategories()
        {
            if (_danceCategoriesProp == null) return Array.Empty<string>();
            var count = _danceCategoriesProp.arraySize;
            var names = new string[count];
            for (var i = 0; i < count; i++)
            {
                names[i] = _danceCategoriesProp.GetArrayElementAtIndex(i).stringValue;
            }
            return names;
        }

        private string GetSyncDanceKey(int index, SerializedProperty dance)
        {
            var name = dance.FindPropertyRelative("danceName").stringValue;
            return string.IsNullOrEmpty(name) ? $"__empty_{index}" : name;
        }

        private bool DanceNameCheck(int index, string dName)
        {
            for (var i = 0; i < _dances.arraySize; i++)
            {
                if (i == index) continue;
                if (_dances.GetArrayElementAtIndex(i).FindPropertyRelative("danceName").stringValue == dName)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 生成不与其它舞蹈重复的舞蹈名称。
        /// 重复时剥除末尾数字后缀，按 “名称 0 / 名称 1 / 名称 2” 递增。
        /// </summary>
        private string GetUniqueDanceName(int index, string name)
        {
            var baseName = StripTrailingNumber(name);
            if (DanceNameCheck(index, name)) return name;

            var ci = 0;
            string cName;
            do
            {
                cName = $"{baseName} {ci}";
                ci++;
            } while (DanceNameCheck(index, cName).Not());

            return cName;
        }

        private static string StripTrailingNumber(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Dance";
            var idx = name.Length - 1;
            while (idx >= 0 && char.IsDigit(name[idx])) idx--;
            // 末尾数字前必须紧跟一个空格，才是我们生成的 “名称 N” 形式
            if (idx >= 0 && idx < name.Length - 1 && name[idx] == ' ')
            {
                return name.Substring(0, idx);
            }
            return name;
        }

        private void OnAddElement(ReorderableList list)
        {
            var index = _dances.arraySize;
            _dances.arraySize++;
            var newProp = _dances.GetArrayElementAtIndex(index);
            newProp.FindPropertyRelative("danceName").stringValue = GetUniqueDanceName(index, "Dance");
            newProp.FindPropertyRelative("speed").floatValue = 3f;
            newProp.FindPropertyRelative("loop").boolValue = true;

            _itemLists.Clear();
            _dirty = true;
            RebuildDisplayIndices();

            var displayIndex = _displayIndices.IndexOf(index);
            list.index = displayIndex >= 0 ? displayIndex : _displayIndices.Count - 1;
        }

        private void OnRemoveElement(ReorderableList list)
        {
            var displayIndex = list.index;
            if (displayIndex < 0 || displayIndex >= _displayIndices.Count) return;
            var arrayIndex = _displayIndices[displayIndex];
            _dances.DeleteArrayElementAtIndex(arrayIndex);

            _itemLists.Clear();
            _dirty = true;
            RebuildDisplayIndices();

            list.index = Mathf.Clamp(displayIndex, 0, _displayIndices.Count - 1);
        }

        private void OnReorder(ReorderableList list, int oldIndex, int newIndex)
        {
            if (!IsDefaultOrder()) return;
            _dances.MoveArrayElement(oldIndex, newIndex);

            _itemLists.Clear();
            _dirty = true;
            RebuildDisplayIndices();

            list.index = newIndex;
        }

        private bool IsDefaultOrder()
        {
            return _sortMode == SortMode.None
                   && string.IsNullOrEmpty(_search)
                   && _loopFilter == LoopFilter.All
                   && _categoryFilterIndex < 0;
        }

        private int GetArrayIndex(int displayIndex)
        {
            if (displayIndex < 0 || displayIndex >= _displayIndices.Count) return -1;
            return _displayIndices[displayIndex];
        }

        private int GetSelectedArrayIndex()
        {
            if (_list == null || _list.index < 0 || _list.index >= _displayIndices.Count) return -1;
            return _displayIndices[_list.index];
        }

        private string GetDanceName(int index)
        {
            if (index < 0 || index >= _dances.arraySize) return "";
            return _dances.GetArrayElementAtIndex(index).FindPropertyRelative("danceName").stringValue ?? "";
        }

        /// <summary>
        /// 获取舞蹈对应的列表项缓存，元素增删/撤销/拖拽导致下标失效时自动重建
        /// </summary>
        private ItemList GetOrCreateItemList(int index, SerializedProperty prop)
        {
            var key = GetSyncDanceKey(index, prop);
            if (_itemLists.TryGetValue(key, out var itemList))
            {
                if (itemList.BoundIndex == index) return itemList;
                _itemLists.Remove(key);
            }
            itemList = new ItemList(_dances, index, () => _syncParameterNames);
            _itemLists[key] = itemList;
            return itemList;
        }

        // ===== 统计大小 =====

        private struct DanceSize
        {
            public long SongOriginal;
            public long SongImported;
            public long Action;

            public long Total => Norm(SongImported) + Norm(Action);

            private static long Norm(long v) => v < 0 ? 0 : v;
        }

        private DanceSize ComputeDanceSize(int index)
        {
            if (_sizeCache.TryGetValue(index, out var cached)) return cached;
            var size = ComputeDanceSizeUncached(index);
            _sizeCache[index] = size;
            return size;
        }

        private DanceSize ComputeDanceSizeUncached(int index)
        {
            var result = new DanceSize();
            if (_dances == null || index < 0 || index >= _dances.arraySize) return result;

            var prop = _dances.GetArrayElementAtIndex(index);
            var music = prop.FindPropertyRelative("musicClip").objectReferenceValue as AudioClip;
            if (music != null)
            {
                result.SongOriginal = GetAudioClipOriginalSize(music);
                result.SongImported = GetAudioClipImportedSize(music);
            }

            var clips = prop.FindPropertyRelative("clip");
            var action = 0L;
            var missing = false;
            for (var i = 0; i < clips.arraySize; i++)
            {
                var clip = clips.GetArrayElementAtIndex(i).objectReferenceValue as AnimationClip;
                if (clip == null) continue;
                var s = GetClipInspectorSize(clip);
                if (s < 0) missing = true;
                else action += s;
            }
            result.Action = missing ? -1 : action;
            return result;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 0) return "-";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024f * 1024f):F2} MB";
            return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
        }

        private static Type _audioUtilType;
        private static MethodInfo _getAnimationClipStatsMethod;
        private static FieldInfo _animationClipStatsSizeField;
        private static readonly object[] _statsInvokeParam = new object[1];
        private static MethodInfo _getImporterFromClipMethod;
        private static PropertyInfo _audioImporterOrigSizeProp;
        private static PropertyInfo _audioImporterCompSizeProp;

        /// <summary>
        /// 音频原始大小（未压缩 PCM），对应 AudioClip Inspector 中显示的 Original Size。
        /// 优先读取 AudioImporter 的内部属性 origSize（Unity 自身 Inspector 即取自该字段），
        /// 取不到 importer 时按 samples * channels * 2 估算。
        /// </summary>
        private static long GetAudioClipOriginalSize(AudioClip clip)
        {
            if (clip == null) return 0;
            var size = GetAudioImporterSize(clip, "origSize");
            if (size >= 0) return size;
            return (long)clip.samples * clip.channels * 2;
        }

        /// <summary>
        /// 音频导入大小，对应 AudioClip Inspector 中显示的 Imported Size（压缩后大小）。
        /// Unity 自身 Inspector 即读取 AudioImporter 的内部属性 compSize，这里保持一致；
        /// 取不到 importer（如运行时生成的剪辑）时按加载类型/比特率估算，失败返回 -1。
        /// </summary>
        private static long GetAudioClipImportedSize(AudioClip clip)
        {
            if (clip == null) return 0;

            var size = GetAudioImporterSize(clip, "compSize");
            if (size >= 0) return size;

            // 回退估算
            return EstimateImportedSize(clip);
        }

        private static long EstimateImportedSize(AudioClip clip)
        {
            // DecompressOnLoad：导入后即为未压缩 PCM，等于原始大小
            if (clip.loadType == AudioClipLoadType.DecompressOnLoad)
            {
                return GetAudioClipOriginalSize(clip);
            }
            // 压缩在内存 / 流式：按比特率估算压缩后大小
            var bitRate = InvokeAudioUtil("GetBitRate", clip);
            if (bitRate > 0)
            {
                return (long)(bitRate / 8.0 * clip.length);
            }
            return -1;
        }

        /// <summary>
        /// 读取 AudioImporter 的内部大小属性。
        /// origSize = 原始未压缩大小，compSize = 导入压缩后大小，
        /// 二者正是 AudioClip Inspector 中 “Original Size / Imported Size” 的数据来源。
        /// 属性不存在或取不到 importer 时返回 -1。
        /// </summary>
        private static long GetAudioImporterSize(AudioClip clip, string propertyName)
        {
            var importer = GetAudioImporter(clip);
            if (importer == null) return -1;
            try
            {
                var prop = propertyName == "origSize"
                    ? GetAudioImporterOrigSizeProp()
                    : GetAudioImporterCompSizeProp();
                if (prop == null) return -1;
                return Convert.ToInt64(prop.GetValue(importer, null));
            }
            catch
            {
                return -1;
            }
        }

        private static PropertyInfo GetAudioImporterOrigSizeProp()
        {
            if (_audioImporterOrigSizeProp == null)
            {
                _audioImporterOrigSizeProp = typeof(AudioImporter)
                    .GetProperty("origSize", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return _audioImporterOrigSizeProp;
        }

        private static PropertyInfo GetAudioImporterCompSizeProp()
        {
            if (_audioImporterCompSizeProp == null)
            {
                _audioImporterCompSizeProp = typeof(AudioImporter)
                    .GetProperty("compSize", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return _audioImporterCompSizeProp;
        }

        private static AudioImporter GetAudioImporter(AudioClip clip)
        {
            if (clip == null) return null;

            // 首选公开 API：AssetImporter.GetAtPath
            var path = AssetDatabase.GetAssetPath(clip);
            if (!string.IsNullOrEmpty(path))
            {
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null) return importer;
            }

            // 回退：UnityEditor.AudioUtil.GetImporterFromClip(clip)（内部类型，需反射）
            if (_getImporterFromClipMethod == null)
            {
                var audioUtil = GetAudioUtilType();
                if (audioUtil != null)
                {
                    _getImporterFromClipMethod = audioUtil.GetMethod(
                        "GetImporterFromClip",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
            }
            if (_getImporterFromClipMethod == null) return null;
            try { return _getImporterFromClipMethod.Invoke(null, new object[] { clip }) as AudioImporter; }
            catch { return null; }
        }

        private static long InvokeAudioUtil(string methodName, AudioClip clip)
        {
            var type = GetAudioUtilType();
            if (type == null) return -1;
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) return -1;
            try
            {
                return Convert.ToInt64(method.Invoke(null, new object[] { clip }));
            }
            catch
            {
                return -1;
            }
        }

        private static Type GetAudioUtilType()
        {
            if (_audioUtilType == null)
            {
                _audioUtilType = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil");
                if (_audioUtilType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        _audioUtilType = assembly.GetType("UnityEditor.AudioUtil");
                        if (_audioUtilType != null) break;
                    }
                }
            }
            return _audioUtilType;
        }

        /// <summary>
        /// 动画剪辑体积（AnimationClip Inspector 显示的 Size），与文件体积不同。
        /// 反射读取 AnimationUtility.GetAnimationClipStats 的 AnimationClipStats.size。
        /// </summary>
        private static long GetClipInspectorSize(AnimationClip clip)
        {
            if (clip == null) return 0;
            try
            {
                if (_getAnimationClipStatsMethod == null)
                {
                    _getAnimationClipStatsMethod = typeof(AnimationUtility)
                        .GetMethod("GetAnimationClipStats", BindingFlags.Static | BindingFlags.NonPublic);
                    _animationClipStatsSizeField = typeof(Editor).Assembly
                        .GetType("UnityEditor.AnimationClipStats")?
                        .GetField("size", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
                if (_getAnimationClipStatsMethod == null || _animationClipStatsSizeField == null) return -1;
                _statsInvokeParam[0] = clip;
                var stats = _getAnimationClipStatsMethod.Invoke(null, _statsInvokeParam);
                if (stats == null) return -1;
                return Convert.ToInt64(_animationClipStatsSizeField.GetValue(stats));
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 校验同步参数值：必须大于 0，且同一 parameterName 下不可重复，重复时向上递增。
        /// </summary>
        private static void ValidateParameterValue(SerializedProperty danceParameters, int index, SerializedProperty valueProp)
        {
            var v = valueProp.floatValue;
            if (v < 1f)
            {
                Debug.LogWarning("同步参数的值不能小于 1，已自动设置为 1");
                valueProp.floatValue = 1f;
                v = 1f;
            }

            var name = danceParameters.GetArrayElementAtIndex(index).FindPropertyRelative("parameterName").stringValue;
            var start = v;
            while (ParameterValueDuplicate(danceParameters, index, name, v))
            {
                v += 1f;
            }
            if (!Mathf.Approximately(v, start))
            {
                Debug.LogWarning($"同步参数 {name} 的值 {start} 与其它同参数项重复，已递增为 {v}");
                valueProp.floatValue = v;
            }
        }

        private static bool ParameterValueDuplicate(SerializedProperty danceParameters, int index, string name, float value)
        {
            for (var i = 0; i < danceParameters.arraySize; i++)
            {
                if (i == index) continue;
                var p = danceParameters.GetArrayElementAtIndex(i);
                if (p.FindPropertyRelative("parameterName").stringValue == name &&
                    Mathf.Approximately(p.FindPropertyRelative("value").floatValue, value))
                {
                    return true;
                }
            }
            return false;
        }

        private class ItemList
        {
            public readonly ReorderableList ClipList;
            public readonly ReorderableList ParameterList;
            /// <summary>该列表项绑定的舞蹈元素下标，用于在数组变化时检测缓存是否失效</summary>
            public readonly int BoundIndex;

            public float ClipHeight => GetHeight(ref _clipCount, ref _clipHeight, ClipList);
            public float ParameterHeight => GetHeight(ref _paramCount, ref _paramHeight, ParameterList);

            private int _clipCount = -1;
            private float _clipHeight;
            private int _paramCount = -1;
            private float _paramHeight;

            private static float GetHeight(ref int cachedCount, ref float cachedHeight, ReorderableList list)
            {
                if (cachedCount != list.count)
                {
                    cachedHeight = list.GetHeight();
                    cachedCount = list.count;
                }
                return cachedHeight;
            }

            public ItemList(SerializedProperty dances, int index, Func<string[]> getSyncParameterNames)
            {
                BoundIndex = index;
                var lineH = EditorGUIUtility.singleLineHeight + 2f;

                var prop = dances.GetArrayElementAtIndex(index);
                var clips = prop.FindPropertyRelative("clip");
                var danceParameters = prop.FindPropertyRelative("danceParameters");

                ParameterList = new ReorderableList(danceParameters.serializedObject, danceParameters, true, true, true, true)
                {
                    drawHeaderCallback = r => EditorGUI.LabelField(r, SyncParameterLabel),
                    elementHeightCallback = _ => lineH,
                    drawElementCallback = (r, i, a, f) =>
                    {
                        if (i < 0 || i >= danceParameters.arraySize) return;
                        var pProp = danceParameters.GetArrayElementAtIndex(i);
                        var parameterName = pProp.FindPropertyRelative("parameterName");
                        var pValue = pProp.FindPropertyRelative("value");
                        var options = getSyncParameterNames();
                        var nowIndex = Array.IndexOf(options, parameterName.stringValue);
                        if (nowIndex == -1)
                        {
                            parameterName.stringValue = options.TryGet(0);
                            nowIndex = 0;
                        }
                        var labelWidth = EditorStyles.label.CalcSize(SyncParameterLabel).x;
                        var valueLabelWidth = EditorStyles.label.CalcSize(ValueLabel).x;
                        var popupWidth = r.width * 0.3f;
                        var valueWidth = r.width - labelWidth - 5f - popupWidth - 5f - valueLabelWidth - 2f;
                        var x = r.x;
                        EditorGUI.LabelField(new Rect(x, r.y, labelWidth, lineH), SyncParameterLabel);
                        x += labelWidth + 5f;
                        var newIndex = EditorGUI.Popup(new Rect(x, r.y, popupWidth, lineH), nowIndex, options);
                        if (newIndex != nowIndex)
                        {
                            parameterName.stringValue = options[newIndex];
                        }
                        x += popupWidth + 5f;
                        EditorGUI.LabelField(new Rect(x, r.y, valueLabelWidth, lineH), ValueLabel);
                        x += valueLabelWidth + 2f;
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(new Rect(x, r.y, valueWidth, lineH), pValue, GUIContent.none);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ValidateParameterValue(danceParameters, i, pValue);
                        }
                    }
                };

                ClipList = new ReorderableList(clips.serializedObject, clips, true, true, true, true)
                {
                    drawHeaderCallback = r => EditorGUI.LabelField(r, AnimClipLabel),
                    elementHeightCallback = _ => lineH,
                    drawElementCallback = (r, i, a, f) =>
                    {
                        if (i < 0 || i >= clips.arraySize) return;
                        var clipProp = clips.GetArrayElementAtIndex(i);
                        var objectWidth = r.width * 0.8f;
                        EditorGUI.PropertyField(new Rect(r.x, r.y, objectWidth, lineH), clipProp, GUIContent.none);
                        var clip = clipProp.objectReferenceValue as AnimationClip;
                        var sizeText = clip != null ? FormatBytes(GetClipInspectorSize(clip)) : "-";
                        EditorGUI.LabelField(new Rect(r.x + objectWidth + 4f, r.y, r.width - objectWidth - 4f, lineH), sizeText, EditorStyles.miniLabel);
                    }
                };
            }
        }

        public static void ShowWindow(GameObject gameObject)
        {
            var wnd = GetWindow<CatSyncDanceDanceEditWindows>(true, "同步舞蹈");
            wnd.minSize = new Vector2(560, 480);
            wnd.SetGameObject(gameObject);
            wnd.Show();
        }

        private void SetGameObject(GameObject go)
        {
            _gameObject = go;
            var component = go != null ? go.GetComponent<CatSyncDance>() : null;

            if (component != null)
            {
                _target = new SerializedObject(component);
                _dances = _target.FindProperty("dances");
                _syncParameterProp = _target.FindProperty("syncDanceConfig").FindPropertyRelative("syncParameterNames");
                _danceCategoriesProp = _target.FindProperty("danceCategories");
            }
            else
            {
                _target = null;
                _dances = null;
                _syncParameterProp = null;
                _danceCategoriesProp = null;
                _list = null;
            }

            _itemLists.Clear();
            _sizeCache.Clear();
            _displayIndices.Clear();

            if (_target != null)
            {
                _list = CreateList();
                RebuildDisplayIndices();
            }
        }

        private ReorderableList CreateList()
        {
            return new ReorderableList(_displayIndices, typeof(int), true, true, true, true)
            {
                drawHeaderCallback = r => EditorGUI.LabelField(r, "舞蹈列表"),
                drawElementCallback = DrawElement,
                elementHeightCallback = ElementHeightCallback,
                onAddCallback = OnAddElement,
                onRemoveCallback = OnRemoveElement,
                onReorderCallbackWithDetails = OnReorder
            };
        }
    }
}
