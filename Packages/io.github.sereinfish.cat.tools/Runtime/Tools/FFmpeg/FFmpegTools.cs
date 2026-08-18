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

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace io.github.sereinfish.cat.tools.Tools.FFmpeg
{
    /// <summary>FFmpeg 转码工具窗口：调用 ffmpeg 处理音频/视频文件。</summary>
    public class FFmpegTools : EditorWindow
    {
        private const string InputPrefsKey = "CatTools.FFmpeg.Input";
        private const string OutputPrefsKey = "CatTools.FFmpeg.Output";

        /// <summary>输出留空时追加到文件名后的「已压缩」后缀。</summary>
        private const string CompressedSuffix = "_compressed";

        private FFmpegConfig _config;
        private string _inputPath = string.Empty;
        private string _outputPath = string.Empty;
        private int _commandIndex = 0;
        private bool _showConfig = false; // 配置面板默认折叠
        private Vector2 _configScroll;
        private Vector2 _logScroll;
        private bool _lastRunning;

        [MenuItem("CatTools/FFmpeg 转码工具")]
        private static void Open()
        {
            GetWindow<FFmpegTools>("FFmpeg 转码工具");
        }

        private void OnEnable()
        {
            _config = FFmpegConfigStore.Load();
            _inputPath = ToAbsoluteProjectPath(EditorPrefs.GetString(InputPrefsKey, string.Empty));
            _outputPath = EditorPrefs.GetString(OutputPrefsKey, string.Empty);

            if (string.IsNullOrEmpty(_config.ffmpegPath))
                _config.ffmpegPath = FFmpegLocator.Find(string.Empty);

            _commandIndex = Mathf.Clamp(_commandIndex, 0, Mathf.Max(0, _config.commands.Count - 1));
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            FFmpegConfigStore.Save(_config);
            EditorPrefs.SetString(InputPrefsKey, _inputPath);
            EditorPrefs.SetString(OutputPrefsKey, _outputPath);
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            bool running = FFmpegProcess.IsRunning;

            // 捕获「运行结束」的下降沿：_lastRunning 为 true 且当前为 false 时表示本次 ffmpeg 刚结束。
            bool justFinished = _lastRunning && !running;

            if (running != _lastRunning || running)
            {
                _lastRunning = running;
                Repaint();
            }

            if (justFinished)
                RefreshAssetDatabase();
        }

        /// <summary>ffmpeg 运行结束后刷新资源数据库，让 Project 窗口立即显示新生成的输出文件。</summary>
        private void RefreshAssetDatabase()
        {
            if (FFmpegProcess.ExitCode != 0)
                return;

            try
            {
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"刷新资源数据库失败：{ex.Message}");
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("FFmpeg 转码工具", EditorStyles.boldLabel);
            GUILayout.Label("调用 ffmpeg 对音频/视频文件进行转码。", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            bool running = FFmpegProcess.IsRunning;

            using (new EditorGUI.DisabledScope(running))
            {
                DrawFFmpegPathField();
                EditorGUILayout.Space(5);
                DrawInputOutputFields();
                EditorGUILayout.Space(5);
                DrawCommandField();
                EditorGUILayout.Space(10);
            }

            DrawRunPanel();

            EditorGUILayout.Space(10);

            DrawLogPanel();

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(running))
            {
                DrawConfigFoldout();
            }

            FFmpegConfigStore.Save(_config);
        }

        private void DrawFFmpegPathField()
        {
            EditorGUILayout.BeginHorizontal();
            _config.ffmpegPath = EditorGUILayout.TextField(
                new GUIContent("FFmpeg 路径",
                    "ffmpeg 可执行文件的完整路径。可手动填写，也可点击「浏览」选择或「自动发现」查找。"),
                _config.ffmpegPath);
            if (GUILayout.Button(new GUIContent("浏览", "手动选择 ffmpeg 可执行文件。"), GUILayout.Width(50)))
            {
                string ext = Application.platform == RuntimePlatform.WindowsEditor ? "exe" : string.Empty;
                string selected = EditorUtility.OpenFilePanel("选择 ffmpeg", GetDirectory(_config.ffmpegPath), ext);
                if (!string.IsNullOrEmpty(selected))
                    _config.ffmpegPath = selected;
            }
            if (GUILayout.Button(new GUIContent("自动发现", "在系统 PATH 与常见安装位置中查找 ffmpeg。"), GUILayout.Width(70)))
            {
                string found = FFmpegLocator.Find(_config.ffmpegPath);
                if (!string.IsNullOrEmpty(found))
                {
                    _config.ffmpegPath = found;
                    Debug.Log($"已在 {found} 找到 ffmpeg。");
                }
                else
                {
                    EditorUtility.DisplayDialog("FFmpeg 转码工具", "未自动发现 ffmpeg，请手动指定路径。", "确定");
                }
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrWhiteSpace(_config.ffmpegPath))
                EditorGUILayout.HelpBox("未设置 FFmpeg 路径。请手动填写或点击「自动发现」。", MessageType.Warning);
            else if (!File.Exists(_config.ffmpegPath))
                EditorGUILayout.HelpBox("FFmpeg 路径无效：找不到可执行文件。", MessageType.Error);
        }

        private void DrawInputOutputFields()
        {
            EditorGUILayout.LabelField(
                new GUIContent("输入文件", "需要处理的音频/视频文件。将文件直接拖入下方选择框即可，无需手动填写路径。"),
                EditorStyles.label);

            // 文件拖放选择框
            Rect dropRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.helpBox, GUILayout.Height(40));
            HandleInputFileDrop(dropRect);
            DrawInputDropZone(dropRect);

            // 显示选中文件的绝对路径，并提供「浏览 / 清除」
            EditorGUILayout.BeginHorizontal();
            string inputHint = string.IsNullOrEmpty(_inputPath) ? "（未选择文件）" : _inputPath;
            EditorGUILayout.SelectableLabel(inputHint, EditorStyles.miniLabel, GUILayout.Height(18));
            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                GUI.tooltip = "当前输入文件的绝对路径，可选中复制。";
            if (GUILayout.Button(new GUIContent("浏览", "选择输入文件。"), GUILayout.Width(50)))
            {
                string selected = EditorUtility.OpenFilePanel(
                    "选择输入文件",
                    GetDirectory(_inputPath),
                    "wav,mp3,ogg,flac,aac,m4a,mp4,avi,mov,mkv,webm");
                if (!string.IsNullOrEmpty(selected))
                    _inputPath = ToAbsoluteProjectPath(selected);
            }
            if (GUILayout.Button(new GUIContent("清除", "清除当前选择的输入文件。"), GUILayout.Width(50)))
                _inputPath = string.Empty;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            _outputPath = EditorGUILayout.TextField(
                new GUIContent("输出路径", "输出文件路径。留空则默认输出到输入文件同目录，文件名为「输入文件名_compressed + 后缀」，若已存在则自动追加递增数字（如 _compressed1、_compressed2）。填相对路径时以项目根目录为基准；生成命令时统一解析为绝对路径。"),
                _outputPath);
            if (GUILayout.Button(new GUIContent("浏览", "选择输出路径。"), GUILayout.Width(50)))
            {
                var cmd = SelectedCommand();
                string ext = cmd != null ? BakedExtension(cmd.command).TrimStart('.') : "ogg";
                if (string.IsNullOrEmpty(ext))
                    ext = "ogg";
                string dir = GetDirectory(string.IsNullOrEmpty(_outputPath) ? _inputPath : _outputPath);
                string defaultName = Path.GetFileNameWithoutExtension(_inputPath);
                string selected = EditorUtility.SaveFilePanel("选择输出路径", dir, defaultName, ext);
                if (!string.IsNullOrEmpty(selected))
                    _outputPath = selected;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                new GUIContent("留空输出路径 = 输入文件同目录 + 文件名_compressed + 后缀（重名自动追加递增数字）；相对路径以项目根目录为基准",
                    "后缀来自指令中的 {output}.后缀（例如 {output}.ogg）。留空时默认文件名为「输入文件名_compressed + 后缀」，若已存在则自动追加递增数字（_compressed1、_compressed2…）。若手动填写的输出路径已包含后缀，则忽略指令中的后缀。相对路径（如 Assets/xx.ogg）会按项目根目录解析为绝对路径。"),
                EditorStyles.miniLabel);
        }

        private void DrawCommandField()
        {
            if (_config.commands.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未配置任何指令。请在下方「指令配置」面板中添加。", MessageType.Info);
                return;
            }

            _commandIndex = Mathf.Clamp(_commandIndex, 0, _config.commands.Count - 1);
            string[] names = new string[_config.commands.Count];
            for (int i = 0; i < _config.commands.Count; i++)
            {
                string n = _config.commands[i].name;
                names[i] = string.IsNullOrEmpty(n) ? $"指令 {i + 1}" : n;
            }

            _commandIndex = EditorGUILayout.Popup(
                new GUIContent("指令", "选择要运行的指令。指令在下方「指令配置」面板中维护。"),
                _commandIndex,
                names);

            var cmd = SelectedCommand();
            if (cmd != null && !string.IsNullOrEmpty(cmd.command))
            {
                EditorGUILayout.LabelField(
                    new GUIContent("指令预览", "即将执行的完整命令（输入/输出均为绝对路径），会随输入/输出与所选指令实时更新。"));
                EditorGUILayout.SelectableLabel(BuildPreviewCommand(cmd), EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawRunPanel()
        {
            if (FFmpegProcess.IsRunning)
            {
                EditorGUILayout.LabelField("处理中…", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("ffmpeg 正在运行，请稍候。", EditorStyles.miniLabel);
                if (GUILayout.Button(new GUIContent("取消", "终止当前 ffmpeg 进程。"), GUILayout.Height(30)))
                    FFmpegProcess.Cancel();
                return;
            }

            bool valid = ValidToRun(out string reason);
            using (new EditorGUI.DisabledScope(!valid))
            {
                if (GUILayout.Button(
                        new GUIContent("运行", "执行所选指令。需先设置 FFmpeg 路径、选择输入文件并选中有效指令。"),
                        GUILayout.Height(35)))
                {
                    RunSelectedCommand();
                }
            }

            if (!valid && !string.IsNullOrEmpty(reason))
                EditorGUILayout.HelpBox(reason, MessageType.Warning);
        }

        private void DrawLogPanel()
        {
            if (!FFmpegProcess.IsRunning && FFmpegProcess.HasResult)
            {
                if (FFmpegProcess.ExitCode == 0)
                    EditorGUILayout.HelpBox("处理完成（退出码 0）。", MessageType.Info);
                else
                    EditorGUILayout.HelpBox($"处理失败（退出码 {FFmpegProcess.ExitCode}）。", MessageType.Error);
            }

            string output = FFmpegProcess.Output;
            if (string.IsNullOrEmpty(output))
                return;

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(140));
            EditorGUILayout.SelectableLabel(output, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigFoldout()
        {
            _showConfig = EditorGUILayout.Foldout(
                _showConfig,
                new GUIContent("指令配置", "配置 ffmpeg 指令模板（支持占位符），支持 JSON 导入/导出。"),
                true);
            if (!_showConfig)
                return;

            EditorGUI.indentLevel++;

            _configScroll = EditorGUILayout.BeginScrollView(_configScroll, GUILayout.Height(180));
            for (int i = 0; i < _config.commands.Count; i++)
            {
                var cmd = _config.commands[i];
                if (cmd == null)
                {
                    cmd = new FFmpegCommand();
                    _config.commands[i] = cmd;
                }

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                cmd.name = EditorGUILayout.TextField(new GUIContent("代称", "指令的代称，用于下拉框显示。"), cmd.name);
                bool remove = GUILayout.Button(new GUIContent("删除", "删除该指令。"), GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();

                cmd.command = EditorGUILayout.TextArea(cmd.command, GUILayout.Height(40));

                EditorGUILayout.EndVertical();

                if (remove)
                {
                    _config.commands.RemoveAt(i);
                    i--;
                }
            }

            if (_config.commands.Count == 0)
                EditorGUILayout.HelpBox("暂无指令，点击「添加指令」新建。", MessageType.Info);

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button(new GUIContent("添加指令", "新增一条空白指令（带示例模板）。")))
            {
                _config.commands.Add(new FFmpegCommand(string.Empty, "-y -i {input} {output}.ogg"));
                _commandIndex = _config.commands.Count - 1;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("导出配置(JSON)", "把当前配置导出为 JSON 文件。")))
                ExportConfig();
            if (GUILayout.Button(new GUIContent("导入配置(JSON)", "从 JSON 文件导入配置，并与当前配置合并共存。")))
                ImportConfig();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                new GUIContent(
                    "占位符：{input} 输入文件 / {output} 输出路径。可用 {output}.ogg 预先指定输出后缀。",
                    "输入文件选择框选中的文件会填充 {input}；输入与输出在生成命令时都会解析为绝对路径。"),
                EditorStyles.wordWrappedLabel);

            EditorGUI.indentLevel--;
        }

        private bool ValidToRun(out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(_config.ffmpegPath))
            {
                reason = "请先设置 FFmpeg 路径。";
                return false;
            }
            if (!File.Exists(_config.ffmpegPath))
            {
                reason = "FFmpeg 路径无效：找不到可执行文件。";
                return false;
            }
            if (string.IsNullOrWhiteSpace(_inputPath))
            {
                reason = "请先选择输入文件。";
                return false;
            }
            if (!File.Exists(_inputPath))
            {
                reason = "输入文件不存在。";
                return false;
            }
            var cmd = SelectedCommand();
            if (cmd == null || string.IsNullOrWhiteSpace(cmd.command))
            {
                reason = "请选择有效指令。";
                return false;
            }
            return true;
        }

        private void RunSelectedCommand()
        {
            var cmd = SelectedCommand();
            if (cmd == null)
                return;

            string args;
            try
            {
                args = BuildArguments(cmd.command, _inputPath, _outputPath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("FFmpeg 转码工具", "生成命令失败：" + ex.Message, "确定");
                return;
            }

            Debug.Log($"运行 ffmpeg：{_config.ffmpegPath} {args}");
            FFmpegProcess.Start(_config.ffmpegPath, args);
            _lastRunning = true;
        }

        /// <summary>根据当前输入/输出路径实时生成将执行的完整命令行（用于预览）。</summary>
        private string BuildPreviewCommand(FFmpegCommand cmd)
        {
            if (cmd == null || string.IsNullOrEmpty(cmd.command))
                return string.Empty;

            try
            {
                string args = BuildArguments(cmd.command, _inputPath, _outputPath);
                string exe = string.IsNullOrWhiteSpace(_config.ffmpegPath) ? "ffmpeg" : _config.ffmpegPath;
                return exe + " " + args;
            }
            catch (Exception ex)
            {
                return "无法解析指令：" + ex.Message;
            }
        }

        private void ExportConfig()
        {
            string path = EditorUtility.SaveFilePanel("导出配置", string.Empty, "FFmpegConfig", "json");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                File.WriteAllText(path, FFmpegConfigStore.ToJson(_config));
                Debug.Log($"已导出 FFmpeg 配置到 {path}");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("FFmpeg 转码工具", "导出失败：" + ex.Message, "确定");
            }
        }

        private void ImportConfig()
        {
            string path = EditorUtility.OpenFilePanel("导入配置", string.Empty, "json");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                string json = File.ReadAllText(path);
                var imported = FFmpegConfigStore.FromJson(json);
                if (imported == null)
                {
                    EditorUtility.DisplayDialog("FFmpeg 转码工具", "导入失败：无法解析 JSON 文件。", "确定");
                    return;
                }

                int before = _config.commands.Count;
                FFmpegConfigStore.Merge(_config, imported);
                int added = _config.commands.Count - before;
                EditorUtility.DisplayDialog("FFmpeg 转码工具", $"导入完成，新增 {added} 条指令（共 {_config.commands.Count} 条）。", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("FFmpeg 转码工具", "导入失败：" + ex.Message, "确定");
            }
        }

        private FFmpegCommand SelectedCommand()
        {
            if (_config == null || _config.commands.Count == 0)
                return null;
            _commandIndex = Mathf.Clamp(_commandIndex, 0, _config.commands.Count - 1);
            return _config.commands[_commandIndex];
        }

        /// <summary>把指令模板解析成最终命令行参数。</summary>
        internal static string BuildArguments(string template, string inputPath, string outputField)
        {
            if (string.IsNullOrEmpty(template))
                throw new ArgumentException("指令模板为空。");

            // 可执行文件已由「FFmpeg 路径」字段指定，剥离模板开头误写的 ffmpeg / ffmpeg.exe。
            string command = StripFfmpegPrefix(template);

            // 识别 {output} 后紧跟的后缀（如 .ogg），作为「预先指定输出格式」。
            var outputMatch = Regex.Match(command, @"\{output\}(\.[A-Za-z0-9]+)?");
            string bakedExt = outputMatch.Success && outputMatch.Groups[1].Success
                ? outputMatch.Groups[1].Value
                : string.Empty;
            string outputToken = outputMatch.Success ? outputMatch.Value : "{output}";

            string finalOutput = ResolveOutputPath(inputPath, outputField, bakedExt);

            string finalInput = ToAbsoluteProjectPath(inputPath);

            string args = command;
            args = args.Replace(outputToken, Quote(finalOutput));
            args = args.Replace("{output}", Quote(finalOutput));
            args = args.Replace("{input}", Quote(finalInput));
            return args;
        }

        /// <summary>剥离指令模板开头误写的 ffmpeg / ffmpeg.exe（可执行文件由「FFmpeg 路径」字段单独指定）。</summary>
        private static string StripFfmpegPrefix(string template)
        {
            string command = template.TrimStart();
            if (command.StartsWith("ffmpeg.exe ", StringComparison.OrdinalIgnoreCase))
                return command.Substring("ffmpeg.exe ".Length).TrimStart();
            if (command.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            if (command.StartsWith("ffmpeg ", StringComparison.OrdinalIgnoreCase))
                return command.Substring("ffmpeg ".Length).TrimStart();
            if (command.Equals("ffmpeg", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return command;
        }

        /// <summary>
        /// 计算最终输出路径：
        /// 1. 输出留空 → 输入同目录 + 输入文件名 + 压缩后缀 + 指令后缀，重名时自动追加递增数字；
        /// 2. 输出已含后缀 → 忽略指令中的后缀；
        /// 3. 输出不含后缀 → 追加指令中的后缀。
        /// 手动填写的相对路径按项目根目录解析为绝对路径。
        /// </summary>
        private static string ResolveOutputPath(string inputPath, string outputField, string bakedExt)
        {
            string userOutput = string.IsNullOrWhiteSpace(outputField) ? string.Empty : outputField.Trim();
            string inputDir = GetDirectory(inputPath);
            string inputName = Path.GetFileNameWithoutExtension(inputPath);

            if (string.IsNullOrEmpty(userOutput))
                return ResolveDefaultOutputPath(inputDir, inputName, bakedExt);

            if (Path.HasExtension(userOutput))
                return ToAbsoluteProjectPath(userOutput);

            return ToAbsoluteProjectPath(userOutput + bakedExt);
        }

        /// <summary>
        /// 输出留空时的默认输出路径：输入同目录 + 输入文件名 + 压缩后缀 + 指令后缀；
        /// 若目标已存在，则在文件名后追加递增数字（1、2、3…）直到不冲突。
        /// </summary>
        private static string ResolveDefaultOutputPath(string inputDir, string inputName, string bakedExt)
        {
            string baseName = inputName + CompressedSuffix;
            string candidate = baseName + bakedExt;
            string fullPath = string.IsNullOrEmpty(inputDir) ? candidate : Path.Combine(inputDir, candidate);

            int n = 1;
            while (File.Exists(fullPath))
            {
                candidate = $"{baseName}{n}{bakedExt}";
                fullPath = string.IsNullOrEmpty(inputDir) ? candidate : Path.Combine(inputDir, candidate);
                n++;
            }

            return fullPath;
        }

        private static string BakedExtension(string template)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;
            var m = Regex.Match(template, @"\{output\}(\.[A-Za-z0-9]+)?");
            return m.Success && m.Groups[1].Success ? m.Groups[1].Value : string.Empty;
        }

        private static string GetDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            try
            {
                return Path.GetDirectoryName(path) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>Unity 项目根目录（Assets 的上一级目录）。</summary>
        private static string ProjectRoot
        {
            get
            {
                string root = Path.GetDirectoryName(Application.dataPath);
                return string.IsNullOrEmpty(root) ? Directory.GetCurrentDirectory() : root;
            }
        }

        /// <summary>相对路径按项目根目录解析为绝对路径；已是绝对路径则原样返回。</summary>
        private static string ToAbsoluteProjectPath(string path)
        {
            if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
                return path;
            return Path.GetFullPath(Path.Combine(ProjectRoot, path));
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        /// <summary>让指定区域支持拖入文件，把第一个拖入的文件绝对路径写入 _inputPath。</summary>
        private void HandleInputFileDrop(Rect rect)
        {
            if (Event.current == null || !rect.Contains(Event.current.mousePosition))
                return;

            if (Event.current.type != EventType.DragUpdated && Event.current.type != EventType.DragPerform)
                return;

            string dropped = FirstFilePath(DragAndDrop.paths);
            bool accepted = !string.IsNullOrEmpty(dropped);
            DragAndDrop.visualMode = accepted ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

            if (Event.current.type == EventType.DragUpdated)
            {
                Event.current.Use();
                return;
            }

            if (accepted)
            {
                DragAndDrop.AcceptDrag();
                _inputPath = ToAbsoluteProjectPath(dropped);
                Event.current.Use();
                Repaint();
            }
        }

        /// <summary>绘制输入文件拖放框：显示已选文件名或拖放提示，并在拖拽悬停时高亮。</summary>
        private void DrawInputDropZone(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            bool dragOver = Event.current != null && rect.Contains(Event.current.mousePosition)
                && (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform);
            if (dragOver)
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.6f, 1f, 0.2f));

            string text = string.IsNullOrEmpty(_inputPath) ? "将音频/视频文件拖入此处" : Path.GetFileName(_inputPath);
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
            };
            EditorGUI.LabelField(rect, new GUIContent(text, _inputPath), style);
        }

        /// <summary>取拖入列表里的第一个文件路径（跳过目录）。</summary>
        private static string FirstFilePath(string[] paths)
        {
            if (paths == null)
                return null;
            foreach (var p in paths)
            {
                if (File.Exists(p))
                    return p;
            }
            return null;
        }

        /// <summary>后台运行 ffmpeg 的进程封装（单任务，静态状态与窗口解耦）。</summary>
        private static class FFmpegProcess
        {
            private static readonly StringBuilder OutputBuilder = new StringBuilder();
            private static readonly object Lock = new object();

            private static Process _process;
            private static Thread _thread;
            private static volatile bool _running;
            private static volatile bool _hasResult;
            private static volatile int _exitCode;

            public static bool IsRunning => _running;
            public static bool HasResult => _hasResult;
            public static int ExitCode => _exitCode;

            public static string Output
            {
                get
                {
                    lock (Lock)
                    {
                        return OutputBuilder.ToString();
                    }
                }
            }

            public static void Start(string ffmpegPath, string arguments)
            {
                if (_running)
                    return;

                lock (Lock)
                {
                    OutputBuilder.Length = 0;
                }
                _hasResult = false;
                _exitCode = -1;
                _running = true;

                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = ProjectRoot,
                };

                _process = new Process { StartInfo = psi };
                _process.OutputDataReceived += OnOutputLine;
                _process.ErrorDataReceived += OnOutputLine;

                _thread = new Thread(RunThread)
                {
                    IsBackground = true,
                    Name = "CatTools.FFmpeg.Process"
                };
                _thread.Start();
            }

            public static void Cancel()
            {
                try
                {
                    if (_process != null && !_process.HasExited)
                        _process.Kill();
                }
                catch
                {
                    // 进程可能尚未启动或已退出，忽略
                }
            }

            private static void RunThread()
            {
                try
                {
                    _process.Start();
                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();
                    _process.WaitForExit();
                    _exitCode = _process.ExitCode;
                }
                catch (Exception ex)
                {
                    AppendLine("运行 ffmpeg 出错：" + ex.Message);
                    _exitCode = -1;
                }
                finally
                {
                    try
                    {
                        _process?.Dispose();
                    }
                    catch
                    {
                        // 忽略释放异常
                    }
                    _process = null;
                    _hasResult = true;
                    _running = false;
                }
            }

            private static void OnOutputLine(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                    AppendLine(e.Data);
            }

            private static void AppendLine(string line)
            {
                lock (Lock)
                {
                    OutputBuilder.AppendLine(line);
                }
            }
        }
    }
}
#endif
