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
using System.Linq;
using UnityEngine;

namespace io.github.sereinfish.cat.tools.Tools.AnimationClipCompressor
{
    /// <summary>除多项式流式外的缩减算法实现</summary>
    public static class CurveReductionAlgorithms
    {
        /// <summary>按算法分发缩减（多项式流式不在本类实现，由 KeyframeCompressionTask 走流式逻辑）</summary>
        public static AnimationCurve Reduce(AnimationCurve c, ReductionAlgorithm alg, double threshold, float dt)
        {
            switch (alg)
            {
                case ReductionAlgorithm.RdpPolyline:
                    return RdpReduce(c, threshold, dt);
                case ReductionAlgorithm.TangentAware:
                    return TangentAwareReduce(c, threshold, dt);
                case ReductionAlgorithm.OptimalDp:
                    return OptimalDpReduce(c, threshold, dt);
                default:
                    // 多项式流式：原样返回（调用方保证不会走到这里）
                    return new AnimationCurve(c.keys);
            }
        }

        /// <summary>
        /// 压缩质量百分比 → 绝对容差（按曲线值域归一化）。
        /// 质量 100% → 容差 0（零压缩，保留全部关键帧）；质量越低容差越大。
        /// 不同算法对同一百分比映射不同的容差增长曲线，符合各自对容差的敏感特性：
        /// DP 直接决定可合并分段长度、最敏感（平方增长并限制上限）；RDP 次之（线性+立方）；
        /// 切线感知逐个试探原始关键帧、最平缓；多项式流式残差为平方量纲，由调用方再取平方。
        /// </summary>
        public static double QualityToTolerance(ReductionAlgorithm alg, float qualityPercent, double valueRange)
        {
            double q = Mathf.Clamp01(qualityPercent / 100f);
            double s = 1.0 - q; // 压缩强度：0 = 100%质量，1 = 0%质量
            double k; // 归一化容差（相对值域的比例 0~1）
            switch (alg)
            {
                case ReductionAlgorithm.OptimalDp:
                    k = 0.25 * s * s;
                    break;
                case ReductionAlgorithm.RdpPolyline:
                    k = 0.02 * s + 0.15 * s * s * s;
                    break;
                case ReductionAlgorithm.TangentAware:
                    k = 0.005 * s + 0.08 * s * s * s;
                    break;
                default: // PolynomialStream
                    k = 0.05 * s * s;
                    break;
            }
            if (valueRange <= 1e-9) return 0;
            return k * valueRange;
        }

        // ============ RDP 折线简化 ============

        public static AnimationCurve RdpReduce(AnimationCurve c, double threshold, float dt)
        {
            var pts = SamplePoints(c, dt);
            if (pts.Count <= 2) return BuildLinearCurve(pts);
            var keep = new HashSet<int> { 0, pts.Count - 1 };
            RdpRecursive(pts, 0, pts.Count - 1, threshold, keep);
            var kept = keep.OrderBy(i => i).Select(i => pts[i]).ToList();
            return BuildLinearCurve(kept);
        }

        private static void RdpRecursive(List<Vector2> pts, int start, int end, double threshold, HashSet<int> keep)
        {
            if (end - start < 2) return;
            Vector2 a = pts[start];
            Vector2 b = pts[end];
            float aLen = (b - a).magnitude;
            int idx = -1;
            float maxD = 0f;
            for (int i = start + 1; i < end; i++)
            {
                float d = PointLineDist(pts[i], a, b, aLen);
                if (d > maxD)
                {
                    maxD = d;
                    idx = i;
                }
            }
            if (maxD > threshold)
            {
                keep.Add(idx);
                RdpRecursive(pts, start, idx, threshold, keep);
                RdpRecursive(pts, idx, end, threshold, keep);
            }
        }

        /// <summary>点到直线 |ab| 的垂直距离（aLen 为 |ab|，避免重复计算）</summary>
        private static float PointLineDist(Vector2 p, Vector2 a, Vector2 b, float aLen)
        {
            if (aLen <= 1e-6f) return Vector2.Distance(p, a);
            // 叉积绝对值 / |ab|
            return Mathf.Abs((b.x - a.x) * (a.y - p.y) - (a.x - p.x) * (b.y - a.y)) / aLen;
        }

        // ============ 切线感知简化 ============

        public static AnimationCurve TangentAwareReduce(AnimationCurve c, double threshold, float dt)
        {
            List<Keyframe> keys = c.keys.ToList();
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 1; i < keys.Count - 1; i++)
                {
                    Keyframe prev = keys[i - 1];
                    Keyframe next = keys[i + 1];
                    // 用指向被删帧的直线斜率临时替换相邻切线，模拟"两段直线替代一段曲线"
                    float s1 = (keys[i].value - prev.value) / (keys[i].time - prev.time);
                    float s2 = (next.value - keys[i].value) / (next.time - keys[i].time);

                    float prevOut = prev.outTangent;
                    float nextIn = next.inTangent;
                    prev.outTangent = s1;
                    next.inTangent = s2;
                    keys[i - 1] = prev;
                    keys[i + 1] = next;

                    // 重建曲线并与原始曲线比较最大误差
                    var probe = new AnimationCurve(keys.ToArray());
                    float maxErr = 0f;
                    float from = prev.time;
                    float to = next.time;
                    for (float t = from; t <= to + 1e-6f; t += dt)
                    {
                        float err = Mathf.Abs(probe.Evaluate(t) - c.Evaluate(t));
                        if (err > maxErr) maxErr = err;
                    }

                    if (maxErr <= threshold)
                    {
                        keys.RemoveAt(i);
                        changed = true;
                    }
                    else
                    {
                        prev.outTangent = prevOut;
                        next.inTangent = nextIn;
                        keys[i - 1] = prev;
                        keys[i + 1] = next;
                    }
                }
            }
            return new AnimationCurve(keys.ToArray());
        }

        // ============ DP 最优分段 ============

        /// <summary>DP 最优分段采样点数上限（算法为 O(n³)，必须限制避免卡死编辑器）</summary>
        private const int DpMaxPoints = 800;

        public static AnimationCurve OptimalDpReduce(AnimationCurve c, double threshold, float dt)
        {
            var pts = SamplePoints(c, dt);
            // O(n³) 保护：超限时均匀降采样（保留首尾端点）
            if (pts.Count > DpMaxPoints)
            {
                int step = Mathf.CeilToInt(pts.Count / (float)DpMaxPoints);
                var sampled = new List<Vector2>(DpMaxPoints + 1);
                for (int i = 0; i < pts.Count; i += step)
                {
                    sampled.Add(pts[i]);
                }
                if (sampled[sampled.Count - 1].x < pts[pts.Count - 1].x)
                {
                    sampled.Add(pts[pts.Count - 1]);
                }
                pts = sampled;
            }

            int n = pts.Count;
            if (n <= 2) return BuildLinearCurve(pts);

            // dp[i] = 覆盖 pts[0..i] 的最少段数；prev[i] = 第 i 点所在段的起点
            const int INF = int.MaxValue / 2;
            int[] dp = new int[n];
            int[] prev = new int[n];
            for (int i = 0; i < n; i++)
            {
                dp[i] = INF;
                prev[i] = -1;
            }
            dp[0] = 0;
            for (int i = 1; i < n; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (dp[j] == INF) continue;
                    if (LineMaxError(pts, j, i) <= threshold && dp[j] + 1 < dp[i])
                    {
                        dp[i] = dp[j] + 1;
                        prev[i] = j;
                    }
                }
            }

            // 回溯段端点
            int cur = n - 1;
            if (dp[cur] >= INF)
            {
                // 没有任何单段满足误差预算：保留原始关键帧，避免结果失真
                return new AnimationCurve(c.keys);
            }
            var kept = new List<Vector2>();
            while (cur >= 0)
            {
                kept.Add(pts[cur]);
                cur = prev[cur];
            }
            kept.Reverse();
            return BuildLinearCurve(kept);
        }

        /// <summary>区间 [j,i] 内各采样点到端点连线的最大偏差</summary>
        private static double LineMaxError(List<Vector2> pts, int j, int i)
        {
            Vector2 a = pts[j];
            Vector2 b = pts[i];
            double span = b.x - a.x;
            double maxErr = 0;
            for (int k = j; k <= i; k++)
            {
                Vector2 p = pts[k];
                double t = span > 1e-9 ? (p.x - a.x) / span : 0;
                double interp = a.y + t * (b.y - a.y);
                double err = Math.Abs(p.y - interp);
                if (err > maxErr) maxErr = err;
            }
            return maxErr;
        }

        // ============ 通用工具 ============

        /// <summary>按 dt 稠密采样原始曲线（限制最大点数，避免算法退化到不可接受的速度）</summary>
        private static List<Vector2> SamplePoints(AnimationCurve c, float dt)
        {
            float endTime = c.keys[c.keys.Length - 1].time;
            // 限制最大采样点数，超出则放大采样间隔
            const int maxPoints = 1500;
            if (endTime / dt > maxPoints) dt = endTime / maxPoints;

            var pts = new List<Vector2>();
            for (float t = 0; t <= endTime + 1e-6f; t += dt)
            {
                pts.Add(new Vector2(t, c.Evaluate(t)));
            }
            // 确保终点精确
            Vector2 last = pts[pts.Count - 1];
            if (Mathf.Abs(last.x - endTime) > 1e-6f)
            {
                pts.Add(new Vector2(endTime, c.Evaluate(endTime)));
            }
            return pts;
        }

        /// <summary>用逐段线性切线构建关键帧曲线（每段内 in/out 切线相等，即为线性插值）</summary>
        private static AnimationCurve BuildLinearCurve(List<Vector2> pts)
        {
            var keys = new Keyframe[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                float inS = 0, outS = 0;
                if (i > 0)
                {
                    inS = (pts[i].y - pts[i - 1].y) / (pts[i].x - pts[i - 1].x);
                }
                if (i < pts.Count - 1)
                {
                    outS = (pts[i + 1].y - pts[i].y) / (pts[i + 1].x - pts[i].x);
                }
                keys[i] = new Keyframe(pts[i].x, pts[i].y, inS, outS);
            }
            return new AnimationCurve(keys);
        }
    }
}
#endif
