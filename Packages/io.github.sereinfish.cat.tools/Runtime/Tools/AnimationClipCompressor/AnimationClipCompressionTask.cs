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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.Tools.AnimationClipCompressor
{
    /// <summary>压缩结果统计（成功 / 取消 / 失败均会填充）</summary>
    public class KeyframeCompressionResult
    {
        public bool Success;
        public bool Canceled;
        public string ErrorMessage;
        public string OutputPath;
        public int OldCurveCount;
        public int NewCurveCount;
        public int OldKeyCount;
        public int NewKeyCount;
        public float OldFileSizeKB;
        public float NewFileSizeKB;
        public float OldAnimSizeKB; // 动画体积（AnimationClip Inspector 显示的 BlobSize）；-1 表示获取失败
        public float NewAnimSizeKB;
    }

    /// <summary>
    /// 动画剪辑关键帧压缩任务。
    /// 以静态单例方式持有（Current），与窗口实例解耦：窗口关闭后任务继续运行，
    /// 重新打开窗口可恢复进度展示。同一时间只允许一个任务（单任务约束）。
    /// 曲线缩减在后台线程执行；资产写入（AssetDatabase）必须在主线程完成。
    /// </summary>
    public class KeyframeCompressionTask
    {
        // ===== 静态单例 =====

        private static KeyframeCompressionTask _current;

        /// <summary>当前任务（运行中或刚完成，用于展示进度与结果）</summary>
        public static KeyframeCompressionTask Current => _current;

        /// <summary>是否有任务正在运行</summary>
        public static bool IsRunning => _current != null && _current.IsActive;

        /// <summary>启动任务；已有任务运行中时返回 false</summary>
        public static bool Start(AnimationClip inputClip, AnimationClip outputClip, int qualityPercent, float dt,
            bool fullSampling, ReductionAlgorithm algorithm,
            bool preserveObjectReferenceCurves, bool preserveAnimationEvents)
        {
            if (IsRunning)
            {
                Debug.LogWarning("[动画剪辑压缩工具] 已有压缩任务正在进行，请等待其完成后重试。");
                return false;
            }
            _current = new KeyframeCompressionTask(inputClip, outputClip, qualityPercent, dt, fullSampling,
                algorithm, preserveObjectReferenceCurves, preserveAnimationEvents);
            _ = _current.RunAsync();
            return true;
        }

        /// <summary>请求取消当前任务</summary>
        public static void Cancel()
        {
            if (_current != null) _current.CancelRequested = true;
        }

        // ===== 实例状态 =====

        private readonly AnimationClip inputClip;
        private readonly AnimationClip outputClip;
        private readonly int qualityPercent;
        private readonly float dt;
        private readonly bool fullSampling; // 整段逐帧采样（原暴力模式）
        private readonly ReductionAlgorithm algorithm;
        private readonly bool preserveObjectReferenceCurves;
        private readonly bool preserveAnimationEvents;

        /// <summary>任务是否运行中（后台线程写入，主线程读取）</summary>
        public volatile bool IsActive;

        /// <summary>取消请求标志（后台线程轮询）</summary>
        public volatile bool CancelRequested;

        /// <summary>整体进度 0~1</summary>
        public volatile float OverallProgress;

        /// <summary>当前处理的文件名（进度展示用）</summary>
        public string CurrentFileName { get; private set; }

        /// <summary>进度详情文本（如"曲线 3/10"）</summary>
        public string ProgressText { get; private set; }

        /// <summary>任务完成后的结果</summary>
        public KeyframeCompressionResult Result { get; private set; }

        // 后台线程进度统计
        private int curveCount;
        private int doneCurveCount;
        private long reductionLoops;
        private long doneReductionLoops;
        private int progressId;
        private bool progressStarted;
        private bool progressFinished;

        private KeyframeCompressionTask(AnimationClip inputClip, AnimationClip outputClip, int qualityPercent, float dt,
            bool fullSampling, ReductionAlgorithm algorithm,
            bool preserveObjectReferenceCurves, bool preserveAnimationEvents)
        {
            this.inputClip = inputClip;
            this.outputClip = outputClip;
            this.qualityPercent = qualityPercent;
            this.dt = dt;
            this.fullSampling = fullSampling;
            this.algorithm = algorithm;
            this.preserveObjectReferenceCurves = preserveObjectReferenceCurves;
            this.preserveAnimationEvents = preserveAnimationEvents;
            CurrentFileName = inputClip != null ? inputClip.name : string.Empty;
        }

        private async Task RunAsync()
        {
            try
            {
                IsActive = true;
                Debug.Log("[动画剪辑压缩工具] ====== 开始执行 ======");
                string clipPath = inputClip != null ? AssetDatabase.GetAssetPath(inputClip) : "(null)";
                Debug.Log($"[动画剪辑压缩工具] 参数: inputClip={clipPath}, qualityPercent={qualityPercent}, dt={dt}, " +
                          $"fullSampling={fullSampling}, algorithm={algorithm}, " +
                          $"preserveObjectReferenceCurves={preserveObjectReferenceCurves}, preserveAnimationEvents={preserveAnimationEvents}");

                if (inputClip == null)
                {
                    Debug.LogError("[动画剪辑压缩工具] 输入 Animation Clip 为空，已中止执行！请在窗口中选择输入动画剪辑。");
                    Result = new KeyframeCompressionResult { ErrorMessage = "输入动画剪辑为空" };
                    return;
                }
                if (dt <= 0)
                {
                    Debug.LogError($"[动画剪辑压缩工具] 采样间隔必须大于 0（当前为 {dt}），已中止执行，否则会导致死循环！");
                    Result = new KeyframeCompressionResult { ErrorMessage = "采样间隔必须大于 0" };
                    return;
                }

                progressId = Progress.Start("动画剪辑压缩工具");
                progressStarted = true;
                Progress.RegisterCancelCallback(progressId, () =>
                {
                    CancelRequested = true;
                    return true;
                });
                Debug.Log($"[动画剪辑压缩工具] 已创建进度条 (progressId={progressId})");

                var allBindings = AnimationUtility.GetCurveBindings(inputClip);
                var curves = allBindings.Select(b => AnimationUtility.GetEditorCurve(inputClip, b)).ToArray();
                var reducedCurves = new AnimationCurve[allBindings.Length];

                curveCount = allBindings.Length;
                Debug.Log($"[动画剪辑压缩工具] 从剪辑读取到 {curveCount} 条动画曲线");

                int emptyCurveCount = 0;
                foreach (var c in curves)
                {
                    // 某些 binding 可能返回 0 个关键帧的曲线，直接跳过，否则 keys[^1] 会越界
                    if (c == null || c.keys.Length == 0)
                    {
                        emptyCurveCount++;
                        continue;
                    }
                    if (algorithm == ReductionAlgorithm.PolynomialStream)
                    {
                        if (!fullSampling)
                        {
                            reductionLoops += c.keys.Length;
                        }
                        else
                        {
                            var endTime = c.keys[c.keys.Length - 1].time;
                            // This value may differ slightly from the actual number of reduction loops
                            // due to floating point errors.
                            reductionLoops += Mathf.CeilToInt(endTime / dt);
                        }
                    }
                    else
                    {
                        // 非流式算法：以曲线条数为单位估算进度
                        reductionLoops += 1;
                    }
                }
                if (emptyCurveCount > 0)
                {
                    Debug.LogWarning($"[动画剪辑压缩工具] 有 {emptyCurveCount} 条曲线不含关键帧，已跳过（不参与缩减）");
                }
                Debug.Log($"[动画剪辑压缩工具] 预估总循环次数: {reductionLoops} (fullSampling={fullSampling})");
                if (reductionLoops <= 0)
                {
                    Debug.LogWarning("[动画剪辑压缩工具] 预估循环次数为 0，动画曲线可能为空，最终输出的剪辑可能不包含任何曲线。");
                }

                Debug.Log("[动画剪辑压缩工具] 开始后台并行缩减曲线...");
                await Task.Run(() =>
                {
                    Parallel.For(0, allBindings.Length, i =>
                    {
                        if (CancelRequested) return;

                        EditorCurveBinding binding = allBindings[i];
                        AnimationCurve curve = curves[i];

                        // 空曲线直接原样保留（不参与缩减），避免 ExecuteReduction 中 keys 越界
                        if (curve == null || curve.keys.Length == 0)
                        {
                            reducedCurves[i] = curve != null ? new AnimationCurve(curve.keys) : new AnimationCurve();
                            Interlocked.Increment(ref doneCurveCount);
                            UpdateProgress();
                            return;
                        }

                        ReductionCurve.CurveType type = ReductionCurve.CurveType.Smooth;
                        if (binding.propertyName == "m_IsActive") type = ReductionCurve.CurveType.Discrete;
                        else if (binding.propertyName.Contains("Rotation")) type = ReductionCurve.CurveType.Degree;
                        // Note: Add any binding type you want here, or send me a PR!

                        // 质量百分比 → 按该曲线值域归一化的绝对容差（Degree 类型先做 ±180° 回绕展开）
                        double valueRange = ComputeValueRange(curve, type);
                        double threshold = CurveReductionAlgorithms.QualityToTolerance(algorithm, qualityPercent, valueRange);
                        if (algorithm == ReductionAlgorithm.PolynomialStream)
                        {
                            // 流式算法阈值是残差平方和：取绝对偏差的平方
                            threshold = threshold * threshold;
                        }
                        reducedCurves[i] = ExecuteReduction(curve, type, algorithm, threshold);

                        Interlocked.Increment(ref doneCurveCount);
                        UpdateProgress();
                    });
                });
                Debug.Log("[动画剪辑压缩工具] 后台并行缩减完成");

                if (CancelRequested)
                {
                    Debug.LogWarning("[动画剪辑压缩工具] 任务已被取消，不写入结果。");
                    Result = new KeyframeCompressionResult
                    {
                        Canceled = true,
                        OutputPath = AssetDatabase.GetAssetPath(inputClip),
                    };
                    FinishProgress(Progress.Status.Canceled);
                    return;
                }

                // 临时剪辑必须在 await 之后（主线程续体）创建：创建→填充→写入资产须在同一个
                // 同步（无 await）代码块内完成，否则临时对象跨越 await 挂起点会被 Unity 销毁，
                // 导致续体中 SetCurve 访问已销毁的 AnimationClip 抛 MissingReferenceException。
                var ac = new AnimationClip();
                Debug.Log("[动画剪辑压缩工具] 开始将缩减后的曲线写入新的 AnimationClip...");
                for (int i = 0; i < allBindings.Length; i++)
                {
                    EditorCurveBinding binding = allBindings[i];
                    AnimationCurve reduced = reducedCurves[i];
                    ac.SetCurve(binding.path, binding.type, binding.propertyName, reduced);
                }

                // 附加数据保留（是否保留由窗口弹窗决定）
                if (preserveObjectReferenceCurves)
                {
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(inputClip))
                    {
                        var keys = AnimationUtility.GetObjectReferenceCurve(inputClip, binding);
                        AnimationUtility.SetObjectReferenceCurve(ac, binding, keys);
                    }
                }
                if (preserveAnimationEvents)
                {
                    AnimationUtility.SetAnimationEvents(ac, AnimationUtility.GetAnimationEvents(inputClip));
                }

                var settings = AnimationUtility.GetAnimationClipSettings(inputClip);
                AnimationUtility.SetAnimationClipSettings(ac, settings);
                Debug.Log("[动画剪辑压缩工具] 曲线写入完成");

                string outputPath;
                if (outputClip == null)
                {
                    // 输出位置为空：在输入剪辑同目录新建，不覆盖原文件
                    string path = AssetDatabase.GetAssetPath(inputClip);
                    string newPath = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_compressed.anim";
                    newPath = newPath.Replace("\\", "/");
                    Debug.Log($"[动画剪辑压缩工具] 输出位置为空，将生成新文件: {newPath}");
                    Write(newPath, ac, true);
                    Debug.Log($"[动画剪辑压缩工具] 新文件已保存: {newPath}");
                    outputPath = newPath;
                }
                else
                {
                    string path = AssetDatabase.GetAssetPath(outputClip);
                    Debug.Log($"[动画剪辑压缩工具] 将覆盖写入现有剪辑: {path}");
                    Write(path, ac, false);
                    Debug.Log($"[动画剪辑压缩工具] 剪辑已保存: {path}");
                    outputPath = path;
                }

                int originalKeys = 0;
                int oldCurveCount = 0;
                int newCurveCount = 0;
                int reducedKeys = 0;
                for (int i = 0; i < curves.Length; i++)
                {
                    if (curves[i] != null && curves[i].keys.Length > 0)
                    {
                        oldCurveCount++;
                        originalKeys += curves[i].keys.Length;
                    }
                    if (reducedCurves[i] != null && reducedCurves[i].keys.Length > 0)
                    {
                        newCurveCount++;
                        reducedKeys += reducedCurves[i].keys.Length;
                    }
                }
                Debug.Log($"[动画剪辑压缩工具] 关键帧数量: {originalKeys} -> {reducedKeys}");

                FileInfo src = new FileInfo(AssetDatabase.GetAssetPath(inputClip));
                FileInfo dst = new FileInfo(outputPath);
                long oldAnimBytes = GetClipInspectorSize(inputClip);
                long newAnimBytes = GetClipInspectorSize(ac);
                Result = new KeyframeCompressionResult
                {
                    Success = true,
                    OutputPath = outputPath,
                    OldCurveCount = oldCurveCount,
                    NewCurveCount = newCurveCount,
                    OldKeyCount = originalKeys,
                    NewKeyCount = reducedKeys,
                    OldFileSizeKB = src.Exists ? src.Length / 1024f : 0,
                    NewFileSizeKB = dst.Exists ? dst.Length / 1024f : 0,
                    OldAnimSizeKB = oldAnimBytes >= 0 ? oldAnimBytes / 1024f : -1f,
                    NewAnimSizeKB = newAnimBytes >= 0 ? newAnimBytes / 1024f : -1f,
                };

                FinishProgress(Progress.Status.Succeeded);
                Debug.Log("[动画剪辑压缩工具] ====== 执行成功 ======");
            }
            catch (Exception e)
            {
                Debug.LogError($"[动画剪辑压缩工具] 执行过程中发生异常: {e}");
                Result = new KeyframeCompressionResult { ErrorMessage = e.Message };
                FinishProgress(Progress.Status.Failed);
            }
            finally
            {
                IsActive = false;
            }
        }

        private AnimationCurve ExecuteReduction(AnimationCurve c, ReductionCurve.CurveType t, ReductionAlgorithm alg, double threshold)
        {
            // 非多项式流式算法：交给 CurveReductionAlgorithms，进度按曲线条数计
            if (alg != ReductionAlgorithm.PolynomialStream)
            {
                AnimationCurve reduced = CurveReductionAlgorithms.Reduce(c, alg, threshold, dt);
                Interlocked.Increment(ref doneReductionLoops);
                UpdateProgress();
                return NoMoreKeysThanOriginal(c, reduced);
            }

            ReductionCurve k = new ReductionCurve(threshold, t);
            if (!fullSampling)
            {
                float lastTime = 0;
                for (int i = 0; i < c.keys.Length; i++)
                {
                    Keyframe key = c.keys[i];

                    float tp2 = key.time - dt * 2;
                    if (lastTime < tp2) k.Tick(tp2, c.Evaluate(tp2));

                    float tp = key.time - dt;
                    if (lastTime < tp) k.Tick(tp, c.Evaluate(tp));

                    k.Tick(key.time, key.value);
                    if (i != c.keys.Length - 1)
                    {
                        float tf = key.time + dt;
                        k.Tick(tf, c.Evaluate(tf));
                        lastTime = tf;
                    }

                    Interlocked.Increment(ref doneReductionLoops);
                    UpdateProgress();
                    if (CancelRequested)
                    {
                        break;
                    }
                }
            }
            else
            {
                float endTime = c.keys[c.keys.Length - 1].time;
                float lt = 0.0f;
                while (lt < endTime)
                {
                    k.Tick(lt, c.Evaluate(lt));
                    lt += dt;

                    Interlocked.Increment(ref doneReductionLoops);
                    UpdateProgress();
                    if (CancelRequested)
                    {
                        break;
                    }
                }
                float eps = 0.0001f;
                if (lt + eps < endTime) k.Tick(endTime, c.Evaluate(endTime));
            }
            k.Done();
            return NoMoreKeysThanOriginal(c, k.curve);
        }

        /// <summary>计算曲线值域（max-min）。Degree 类型先按 ±180° 回绕展开再计算，
        /// 避免旋转跨越 ±180° 时值域虚高（与 ReductionCurve.PickValue 的展开方式一致）</summary>
        private double ComputeValueRange(AnimationCurve c, ReductionCurve.CurveType type)
        {
            if (c == null || c.keys.Length == 0) return 0;
            float min = float.MaxValue, max = float.MinValue;
            float last = 0f;
            bool first = true;
            for (int i = 0; i < c.keys.Length; i++)
            {
                float v = c.keys[i].value;
                if (type == ReductionCurve.CurveType.Degree)
                {
                    if (!first)
                    {
                        v -= last;
                        v = (((v + 180) % 360 + 360) % 360) - 180;
                        v += last;
                    }
                    last = v;
                    first = false;
                }
                if (v < min) min = v;
                if (v > max) max = v;
            }
            return max - min;
        }

        /// <summary>无损兜底：缩减结果的关键帧数不少于原曲线时，直接保留原曲线，
        /// 避免高保真档位（如 100% 质量）输出比原剪辑更多的采样点关键帧</summary>
        private static AnimationCurve NoMoreKeysThanOriginal(AnimationCurve original, AnimationCurve reduced)
        {
            return reduced.keys.Length >= original.keys.Length ? new AnimationCurve(original.keys) : reduced;
        }

        private void UpdateProgress()
        {
            if (reductionLoops <= 0) return; // 防止除 0 产生 NaN
            float p = (float)((double)doneReductionLoops / reductionLoops);
            OverallProgress = p;
            ProgressText = $"{doneCurveCount}/{curveCount} 曲线";
            Progress.Report(progressId, p, $"{CurrentFileName} ({doneCurveCount}/{curveCount} 曲线)");
        }

        /// <summary>结束进度条（可重入：重复调用仅生效一次，避免对同一 Progress 重复 Finish 抛异常）</summary>
        private void FinishProgress(Progress.Status status)
        {
            if (!progressStarted || progressFinished) return;
            progressFinished = true;
            Progress.Finish(progressId, status);
        }

        private void Write(string rawPath, AnimationClip clip, bool unique)
        {
            string path = unique
                ? AssetDatabase.GenerateUniqueAssetPath(rawPath)
                : rawPath;
            OverwriteAsset(clip, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void OverwriteAsset(UnityEngine.Object asset, string path)
        {
            if (File.Exists(path))
            {
                string tmpDirPath = Path.Combine(Path.GetDirectoryName(path), "tmpOverwrite");
                Directory.CreateDirectory(tmpDirPath);

                string tmpPath = Path.Combine(tmpDirPath, Path.GetFileName(path));
                AssetDatabase.CreateAsset(asset, tmpPath);

                FileUtil.ReplaceFile(tmpPath, path);
                AssetDatabase.DeleteAsset(tmpDirPath);
                AssetDatabase.ImportAsset(path);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
            }
        }

        // ===== 动画体积（Inspector 显示的 BlobSize）=====

        private static MethodInfo _getAnimationClipStatsMethod;
        private static FieldInfo _animationClipStatsSizeField;
        private static readonly object[] _statsInvokeParam = new object[1];

        /// <summary>
        /// 获取动画剪辑的「动画体积」：即 AnimationClip Inspector 中显示的 Size（反序列化后的二进制大小 BlobSize），
        /// 与文件体积不同。Unity 未公开该接口，通过反射调用非公开的 AnimationUtility.GetAnimationClipStats
        /// 读取 AnimationClipStats.size 字段（与 Inspector 显示的值一致）。
        /// </summary>
        /// <returns>体积（字节）；获取失败时返回 -1</returns>
        private static long GetClipInspectorSize(AnimationClip clip)
        {
            if (clip == null) return -1;
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
                if (_getAnimationClipStatsMethod == null || _animationClipStatsSizeField == null)
                {
                    Debug.LogWarning("[动画剪辑压缩工具] 无法反射到 AnimationClipStats，动画体积不可用。");
                    return -1;
                }
                _statsInvokeParam[0] = clip;
                object stats = _getAnimationClipStatsMethod.Invoke(null, _statsInvokeParam);
                if (stats == null) return -1;
                return Convert.ToInt64(_animationClipStatsSizeField.GetValue(stats));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[动画剪辑压缩工具] 获取动画体积失败（将以 \"-\" 显示）: {e.Message}");
                return -1;
            }
        }
    }
}
#endif
