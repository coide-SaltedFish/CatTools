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

using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.Tools.AnimationClipCompressor
{
    /// <summary>动画剪辑压缩工具窗口</summary>
    public class AnimationClipCompressorWindow : EditorWindow
    {
        private AnimationClip inputClip;  // 输入动画剪辑
        private AnimationClip outputClip; // 输出位置（留空 = 同目录新建，不覆盖）

        private int density = BalancedDensity; // 采样密度百分比 1~100
        private int dtDensity = BalancedDtDensity; // 采样间隔密度百分比 1~300（100% = 剪辑原生帧率）
        private bool fullSampling = DefaultFullSampling; // 整段逐帧采样（原暴力模式）
        private ReductionAlgorithm algorithm = ReductionAlgorithm.PolynomialStream;

        // 预设参数：同时设置「采样密度(%)」与「采样间隔密度(%)」；采样间隔按算法特性取值；整段逐帧采样为独立的高级选项
        private enum Preset { MaxQuality = 0, Balanced = 1, MaxCompression = 2 }

        private const int MaxQualityDensity = 100; // 最大采样密度：几乎不压缩
        private const int BalancedDensity = 50;    // 平衡
        private const int MaxCompressionDensity = 25; // 激进压缩

        // 采样间隔密度百分比：100% = 剪辑原生帧率（1/fps）。除 DP 外各算法统一取值；
        // DP 最优分段为 O(n³)，对采样点数极为敏感，各档均使用更稀疏的间隔保证可用性。
        private const int MaxDtDensity = 300;
        private const int MaxQualityDtDensity = 200;        // 2× 帧率（60fps 时约 1/120s）
        private const int BalancedDtDensity = 100;          // 原生帧率（60fps 时约 1/60s）
        private const int MaxCompressionDtDensity = 50;     // 1/2 帧率（60fps 时约 1/30s）
        private const int MaxQualityDpDtDensity = 100;      // DP：原生帧率（更密会被采样上限降采样，收益有限）
        private const int BalancedDpDtDensity = 50;         // DP：1/2 帧率（60fps 时约 1/30s）
        private const int MaxCompressionDpDtDensity = 25;   // DP：1/4 帧率（60fps 时约 1/15s）

        private const bool DefaultFullSampling = true;

        // 预设下拉框选项（下标 0~2 为预设，3 为自定义，仅显示用，不可选择）
        private static readonly string[] PresetNames =
        {
            "最大质量预设", "平衡预设", "最大压缩预设", "自定义预设",
        };

        // 进度轮询（后台线程只写静态状态，此处仅主线程读取）
        private bool lastShownRunning;
        private float lastShownProgress = -1f;

        [MenuItem("CatTools/动画剪辑压缩工具")]
        private static void Open()
        {
            GetWindow<AnimationClipCompressorWindow>("动画剪辑压缩工具");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        /// <summary>Ctrl+Z 撤销 / Ctrl+Shift+Z 重做后刷新窗口，让参数立即恢复显示</summary>
        private void OnUndoRedoPerformed()
        {
            Repaint();
        }

        private void OnEditorUpdate()
        {
            bool running = KeyframeCompressionTask.IsRunning;
            float progress = KeyframeCompressionTask.Current != null ? KeyframeCompressionTask.Current.OverallProgress : 0f;
            if (running != lastShownRunning || (running && Mathf.Abs(progress - lastShownProgress) > 0.0001f))
            {
                lastShownRunning = running;
                lastShownProgress = progress;
                Repaint();
            }
        }

        private void OnGUI()
        {
            // Ctrl+Z 撤销支持：鼠标按下或键盘输入之前记录窗口状态，
            // 使「采样密度(%)」「采样间隔」「算法」「输入/输出剪辑」等界面参数均可通过 Ctrl+Z 撤回。
            // 同一组内（一次拖拽 / 一次点击）的全部修改合并为一个撤销步骤。
            // 排除修饰键与 Z/Y 键，避免拦截 Unity 编辑器自带的撤销/重做快捷键。
            Event evt = Event.current;
            if (evt != null)
            {
                bool isModifierKey = evt.keyCode == KeyCode.LeftControl || evt.keyCode == KeyCode.RightControl
                    || evt.keyCode == KeyCode.LeftCommand || evt.keyCode == KeyCode.RightCommand
                    || evt.keyCode == KeyCode.LeftAlt || evt.keyCode == KeyCode.RightAlt
                    || evt.keyCode == KeyCode.LeftShift || evt.keyCode == KeyCode.RightShift;
                bool isUndoRedoKey = evt.keyCode == KeyCode.Z || evt.keyCode == KeyCode.Y;
                if (evt.type == EventType.MouseDown
                    || (evt.type == EventType.KeyDown && !isModifierKey && !isUndoRedoKey))
                {
                    Undo.IncrementCurrentGroup();
                    Undo.RecordObject(this, "修改压缩参数");
                }
            }

            GUILayout.Label("动画剪辑压缩工具", EditorStyles.boldLabel);
            GUILayout.Label("对 AnimationClip 的关键帧进行缩减压缩，以减小动画文件体积。", EditorStyles.miniLabel);

            var ackContent = new GUIContent(
                "本工具参考 ImKeyframeReduction 项目实现，感谢原作者的分享：https://github.com/phi16/ImKeyframeReduction",
                "点击打开 ImKeyframeReduction 项目主页");
            if (GUILayout.Button(ackContent, EditorStyles.miniLabel))
            {
                Application.OpenURL("https://github.com/phi16/ImKeyframeReduction");
            }
            EditorGUILayout.Space(5);

            bool running = KeyframeCompressionTask.IsRunning;

            using (new EditorGUI.DisabledScope(running))
            {
                DrawPresetField();
                DrawAlgorithmField();
                EditorGUILayout.Space(5);

                inputClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("输入动画剪辑", "需要压缩的原始动画剪辑（必填）。"),
                    inputClip, typeof(AnimationClip), false);

                density = EditorGUILayout.IntSlider(
                    new GUIContent("采样密度",
                        "采样密度百分比。100% 为最大采样密度（保留全部关键帧、几乎不压缩），数值越低压缩越激进、曲线变形越大。\n" +
                        "同一百分比下，不同算法会按各自特性换算成对应的内部容差参数（DP 最优分段对容差最敏感，切线感知最温和）。\n" +
                        "预设：最大质量 100% / 平衡 50% / 最大压缩 25%。"),
                    density, 1, 100);
                EditorGUILayout.LabelField(
                    new GUIContent("100% = 最大采样密度（几乎不压缩），1% = 最大压缩。建议从 50% 起步调整。",
                        "同一百分比下不同算法的内部参数不同：DP 最优分段对容差最敏感，切线感知最温和。"),
                    EditorStyles.miniLabel);

                // 采样间隔：百分比条（相对剪辑原生帧率的密度）+ 右侧输入框可直接设置实际秒数
                int dtPct = dtDensity;
                int newDtPct = dtPct;
                float dtSec = DensityToDt(dtPct);
                float newDtSec = dtSec;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(new GUIContent("采样间隔",
                    "采样间隔密度百分比：100% = 剪辑原生帧率的采样间隔（1/fps 秒，如 60fps 时约 1/60 秒），" +
                    "百分比越高采样越密集、结果越精细但耗时越长，越低越稀疏、速度越快。\n" +
                    "右侧输入框可手动设置实际秒数，设置后百分比自动换算。"));
                newDtPct = Mathf.RoundToInt(EditorGUILayout.Slider(dtPct, 1f, MaxDtDensity));
                newDtSec = EditorGUILayout.FloatField(dtSec, GUILayout.Width(70f));
                EditorGUILayout.EndHorizontal();
                if (newDtPct != dtPct)
                {
                    // 拖动百分比条
                    dtDensity = newDtPct;
                }
                else if (newDtSec != dtSec)
                {
                    // 直接输入实际秒数
                    dtDensity = DtToDensity(newDtSec);
                }

                fullSampling = EditorGUILayout.Toggle(
                    new GUIContent("整段逐帧采样",
                        "开启：对整段动画时长按采样间隔逐帧采样评估，结果更精确但耗时更长。\n" +
                        "关闭：仅在关键帧附近采样评估，速度更快。"),
                    fullSampling);

                outputClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("输出位置",
                        "留空：在输入剪辑同目录下新建压缩后的动画剪辑（不覆盖原文件）。\n" +
                        "指定已有剪辑：压缩结果将覆盖该剪辑。"),
                    outputClip, typeof(AnimationClip), false);
            }

            EditorGUILayout.Space(10);

            if (running)
            {
                DrawProgressPanel();
            }
            else
            {
                if (GUILayout.Button(new GUIContent("执行压缩", "开始压缩当前输入动画剪辑。点击后按钮将禁用，处理期间仅可取消。")))
                {
                    StartCompression();
                }
            }

            // 结果统计（任务完成但尚未开始新任务时展示）
            var task = KeyframeCompressionTask.Current;
            if (task != null && !task.IsActive && task.Result != null)
            {
                DrawResult(task.Result);
            }
        }

        private void DrawPresetField()
        {
            int currentIndex = GetCurrentPresetIndex();
            int newIndex = EditorGUILayout.Popup(
                new GUIContent("预设",
                    "快速套用常用压缩参数组合（同时设置「采样密度」与「采样间隔」，采样间隔按算法特性取值，如 DP 最优分段使用更稀疏的间隔）。" +
                    "手动修改「采样密度」或「采样间隔」后自动切换为「自定义预设」。"),
                currentIndex, PresetNames);
            if (newIndex != currentIndex && newIndex < 3)
            {
                ApplyPreset((Preset)newIndex);
            }
        }

        private void DrawAlgorithmField()
        {
            int algIndex = (int)algorithm;
            int newAlgIndex = EditorGUILayout.Popup(
                new GUIContent("缩减算法", "选择关键帧缩减算法，不同算法在压缩质量与耗时上各有取舍。"),
                algIndex, ReductionAlgorithmInfo.Names);
            algorithm = (ReductionAlgorithm)newAlgIndex;
            EditorGUILayout.HelpBox(ReductionAlgorithmInfo.Descriptions[(int)algorithm], MessageType.Info);
            EditorGUILayout.Space(5);
        }

        private void DrawProgressPanel()
        {
            var task = KeyframeCompressionTask.Current;
            float progress = task != null ? task.OverallProgress : 0f;
            string fileName = task != null ? task.CurrentFileName : string.Empty;

            EditorGUILayout.LabelField("处理中…", EditorStyles.boldLabel);
            Rect rect = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.ProgressBar(rect, progress, fileName);
            if (task != null && !string.IsNullOrEmpty(task.ProgressText))
            {
                EditorGUILayout.LabelField(task.ProgressText, EditorStyles.miniLabel);
            }
            if (GUILayout.Button(new GUIContent("取消", "取消当前压缩任务。")))
            {
                KeyframeCompressionTask.Cancel();
            }
        }

        private void DrawResult(KeyframeCompressionResult r)
        {
            EditorGUILayout.Space(8);
            if (r.Success)
            {
                EditorGUILayout.HelpBox(
                    $"压缩完成\n\n曲线：{r.OldCurveCount} → {r.NewCurveCount}\n" +
                    $"关键帧：{r.OldKeyCount} → {r.NewKeyCount}（压缩率 {FormatRatio(r.OldKeyCount, r.NewKeyCount)}）\n" +
                    $"文件体积：{FormatFileSize(r.OldFileSizeKB)} → {FormatFileSize(r.NewFileSizeKB)}（压缩率 {FormatRatio(r.OldFileSizeKB, r.NewFileSizeKB)}）\n" +
                    $"动画体积：{FormatAnimSize(r.OldAnimSizeKB)} → {FormatAnimSize(r.NewAnimSizeKB)}（压缩率 {FormatRatio(r.OldAnimSizeKB, r.NewAnimSizeKB)}）\n" +
                    $"输出：{r.OutputPath}",
                    MessageType.Info);
            }
            else if (r.Canceled)
            {
                EditorGUILayout.HelpBox("任务已取消，未写入任何结果。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"执行失败：{r.ErrorMessage}", MessageType.Error);
            }
        }

        /// <summary>文件大小格式化：不足 1MB 显示 KB，否则自动转换为 MB</summary>
        private static string FormatFileSize(float kb)
        {
            return kb >= 1024f ? $"{kb / 1024f:F2} MB" : $"{kb:F1} KB";
        }

        /// <summary>动画体积格式化：获取失败（-1）时显示 "-"</summary>
        private static string FormatAnimSize(float kb)
        {
            return kb < 0f ? "-" : FormatFileSize(kb);
        }

        /// <summary>压缩率：new 比 old 小时为负值（缩减了 x%），反之正值（变大了 x%）</summary>
        private static string FormatRatio(float oldV, float newV)
        {
            if (oldV <= 0f) return "-";
            double p = (1.0 - newV / oldV) * 100.0;
            return p >= 0 ? $"-{p:F1}%" : $"+{-p:F1}%";
        }

        /// <summary>当前输入剪辑的原生帧率；未选择剪辑或帧率异常时回退到 60fps</summary>
        private float ClipFps()
        {
            if (inputClip == null) return 60f;
            float fps = inputClip.frameRate;
            if (fps <= 0f || float.IsNaN(fps) || float.IsInfinity(fps)) return 60f;
            return fps;
        }

        /// <summary>采样间隔密度百分比 → 实际间隔秒数：100% = 剪辑原生帧率间隔 1/fps</summary>
        private float DensityToDt(int densityPct)
        {
            return 100f / (ClipFps() * Mathf.Max(densityPct, 1));
        }

        /// <summary>实际间隔秒数 → 采样间隔密度百分比（1~300，越界自动截断）</summary>
        private int DtToDensity(float dtSeconds)
        {
            if (dtSeconds <= 0f) return MaxDtDensity;
            return Mathf.Clamp(Mathf.RoundToInt(100f / (ClipFps() * dtSeconds)), 1, MaxDtDensity);
        }

        /// <summary>根据当前参数判断所属预设；不匹配任何预设时返回 3（自定义）</summary>
        private int GetCurrentPresetIndex()
        {
            for (int i = 0; i <= (int)Preset.MaxCompression; i++)
            {
                Preset p = (Preset)i;
                if (density == PresetDensity(p) && dtDensity == PresetDtDensity(p))
                {
                    return i;
                }
            }
            return 3;
        }

        /// <summary>预设对应的「采样密度(%)」</summary>
        private int PresetDensity(Preset preset)
        {
            switch (preset)
            {
                case Preset.MaxQuality: return MaxQualityDensity;
                case Preset.Balanced: return BalancedDensity;
                default: return MaxCompressionDensity;
            }
        }

        /// <summary>预设对应的「采样间隔密度(%)」：按算法特性取值（DP 最优分段使用更稀疏的间隔）</summary>
        private int PresetDtDensity(Preset preset)
        {
            bool isDp = algorithm == ReductionAlgorithm.OptimalDp;
            switch (preset)
            {
                case Preset.MaxQuality: return isDp ? MaxQualityDpDtDensity : MaxQualityDtDensity;
                case Preset.Balanced: return isDp ? BalancedDpDtDensity : BalancedDtDensity;
                default: return isDp ? MaxCompressionDpDtDensity : MaxCompressionDtDensity;
            }
        }

        private void ApplyPreset(Preset preset)
        {
            density = PresetDensity(preset);
            dtDensity = PresetDtDensity(preset);
        }

        private void StartCompression()
        {
            if (KeyframeCompressionTask.IsRunning) return;

            if (inputClip == null)
            {
                EditorUtility.DisplayDialog("动画剪辑压缩工具", "请先选择「输入动画剪辑」。", "确定");
                return;
            }

            // 采样间隔实际秒数由「采样间隔密度百分比」× 剪辑原生帧率换算得到（密度 ≥ 1 恒有 dt > 0）
            float dt = DensityToDt(dtDensity);

            // 检测压缩过程中会丢失的附加数据，弹窗由用户决定是否保留
            bool hasObjectRefCurves = AnimationUtility.GetObjectReferenceCurveBindings(inputClip).Length > 0;
            bool hasAnimationEvents = AnimationUtility.GetAnimationEvents(inputClip).Length > 0;
            bool preserveObjRef = false, preserveEvents = false;
            if (hasObjectRefCurves || hasAnimationEvents)
            {
                var msg = "检测到输入剪辑包含压缩过程中会丢失的附加数据：";
                if (hasObjectRefCurves) msg += "\n- ObjectReference 曲线（如材质引用等）";
                if (hasAnimationEvents) msg += "\n- AnimationEvent 动画事件";
                msg += "\n\n是否在压缩结果中保留这些数据？";
                int choice = EditorUtility.DisplayDialogComplex("检测到附加数据", msg, "保留", "不保留", "取消");
                if (choice == 2) return; // 取消本次压缩
                preserveObjRef = hasObjectRefCurves && choice == 0;
                preserveEvents = hasAnimationEvents && choice == 0;
            }

            KeyframeCompressionTask.Start(inputClip, outputClip, density, dt, fullSampling, algorithm,
                preserveObjRef, preserveEvents);
            lastShownRunning = true;
            lastShownProgress = 0f;
        }
    }
}
#endif
