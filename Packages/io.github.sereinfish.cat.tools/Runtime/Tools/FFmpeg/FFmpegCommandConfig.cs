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
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.Tools.FFmpeg
{
    /// <summary>单条 FFmpeg 指令模板：代称 + 命令模板。</summary>
    [Serializable]
    public class FFmpegCommand
    {
        /// <summary>指令代称（用于下拉框显示）。</summary>
        public string name;

        /// <summary>命令模板，支持占位符 {input} / {output}[.后缀]；不含 ffmpeg 可执行文件名。</summary>
        public string command;

        public FFmpegCommand() { }

        public FFmpegCommand(string name, string command)
        {
            this.name = name;
            this.command = command;
        }
    }

    /// <summary>FFmpeg 工具配置：ffmpeg 路径 + 指令列表。</summary>
    [Serializable]
    public class FFmpegConfig
    {
        public string ffmpegPath = string.Empty;
        public List<FFmpegCommand> commands = new List<FFmpegCommand>();
    }

    /// <summary>配置持久化（EditorPrefs）与 JSON 导入/导出。</summary>
    public static class FFmpegConfigStore
    {
        private const string PrefsKey = "CatTools.FFmpeg.Config";

        public static FFmpegConfig Load()
        {
            string json = EditorPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return CreateDefault();

            try
            {
                var cfg = JsonUtility.FromJson<FFmpegConfig>(json);
                if (cfg == null)
                    return CreateDefault();
                if (cfg.commands == null)
                    cfg.commands = new List<FFmpegCommand>();
                foreach (var c in cfg.commands)
                {
                    if (c != null)
                        c.command = NormalizeLegacyPlaceholders(c.command);
                }
                return cfg;
            }
            catch
            {
                return CreateDefault();
            }
        }

        public static void Save(FFmpegConfig cfg)
        {
            if (cfg == null) return;
            EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(cfg));
        }

        public static string ToJson(FFmpegConfig cfg)
        {
            return cfg == null ? "{}" : JsonUtility.ToJson(cfg, true);
        }

        public static FFmpegConfig FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonUtility.FromJson<FFmpegConfig>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 把导入的配置合并进当前配置：指令以「共存」方式追加；
        /// 代称冲突时自动追加序号，避免覆盖；ffmpeg 路径仅在当前为空时采用。
        /// </summary>
        public static void Merge(FFmpegConfig current, FFmpegConfig imported)
        {
            if (current == null || imported == null) return;

            if (string.IsNullOrEmpty(current.ffmpegPath) && !string.IsNullOrEmpty(imported.ffmpegPath))
                current.ffmpegPath = imported.ffmpegPath;

            if (imported.commands == null) return;

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in current.commands)
            {
                if (c != null && !string.IsNullOrEmpty(c.name))
                    names.Add(c.name);
            }

            foreach (var c in imported.commands)
            {
                if (c == null) continue;
                var candidate = new FFmpegCommand(
                    string.IsNullOrEmpty(c.name) ? "未命名指令" : c.name,
                    NormalizeLegacyPlaceholders(c.command ?? string.Empty));

                if (!names.Add(candidate.name))
                {
                    string baseName = candidate.name;
                    int n = 2;
                    while (!names.Add($"{baseName} ({n})"))
                        n++;
                    candidate.name = $"{baseName} ({n})";
                }

                current.commands.Add(candidate);
            }
        }

        /// <summary>把旧的 {file.audio} / {file.video} 占位符迁移为 {input}。</summary>
        private static string NormalizeLegacyPlaceholders(string command)
        {
            if (string.IsNullOrEmpty(command))
                return command;
            return command
                .Replace("{file.audio}", "{input}")
                .Replace("{file.video}", "{input}");
        }

        /// <summary>首次使用（无已保存配置）时提供的示例指令。</summary>
        private static FFmpegConfig CreateDefault()
        {
            var cfg = new FFmpegConfig();
            cfg.commands.Add(new FFmpegCommand(
                "音频转OGG(VRChat)",
                "-y -i {input} -c:a libvorbis -q:a 4 {output}.ogg"));
            cfg.commands.Add(new FFmpegCommand(
                "视频转MP4",
                "-y -i {input} -c:v libx264 -pix_fmt yuv420p -c:a aac {output}.mp4"));
            return cfg;
        }
    }

    /// <summary>ffmpeg 自动发现：优先使用手动路径，其次搜索 PATH 与常见安装位置。</summary>
    public static class FFmpegLocator
    {
        public static string Find(string manualPath)
        {
            if (!string.IsNullOrWhiteSpace(manualPath) && File.Exists(manualPath))
                return manualPath;

            foreach (var dir in PathDirectories())
            {
                foreach (var name in ExecutableNames)
                {
                    var candidate = Path.Combine(dir, name);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }

            foreach (var candidate in CommonLocations())
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return string.Empty;
        }

        private static IEnumerable<string> PathDirectories()
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                yield break;

            foreach (var dir in path.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir))
                    continue;
                yield return dir.Trim();
            }
        }

        private static IEnumerable<string> CommonLocations()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                yield return Path.Combine(localAppData, "Microsoft", "WinGet", "Links", "ffmpeg.exe");
                yield return Path.Combine(home, "scoop", "shims", "ffmpeg.exe");
                yield return Path.Combine(home, "chocolatey", "bin", "ffmpeg.exe");
                yield return @"C:\ffmpeg\bin\ffmpeg.exe";
                yield return @"C:\Program Files\ffmpeg\bin\ffmpeg.exe";
                yield return @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe";
            }
            else
            {
                yield return "/usr/local/bin/ffmpeg";
                yield return "/opt/homebrew/bin/ffmpeg";
                yield return "/usr/bin/ffmpeg";
            }
        }

        private static string[] ExecutableNames =>
            Application.platform == RuntimePlatform.WindowsEditor
                ? new[] { "ffmpeg.exe" }
                : new[] { "ffmpeg" };
    }
}
#endif
