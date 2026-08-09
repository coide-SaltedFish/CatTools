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
using UnityEngine;

namespace io.github.sereinfish.cat.tools.Tools.AnimationClipCompressor
{
    /// <summary>关键帧缩减算法枚举</summary>
    public enum ReductionAlgorithm
    {
        PolynomialStream, // 多项式流式（默认）
        RdpPolyline,      // RDP 折线简化
        TangentAware,     // 切线感知简化
        OptimalDp,        // DP 最优分段
    }

    /// <summary>算法展示信息（下拉框名字 + 特点描述）</summary>
    public static class ReductionAlgorithmInfo
    {
        public static readonly string[] Names =
        {
            "多项式流式（默认）",
            "RDP 折线简化",
            "切线感知简化",
            "DP 最优分段",
        };

        public static readonly string[] Descriptions =
        {
            "按采样间隔流式采样，用最小二乘多项式拟合，残差平方和超过误差阈值时保留关键帧。" +
            "速度最快、可在线处理，但误差为平方残差而非逐点保证。",
            "Ramer-Douglas-Peucker 折线简化。先稠密采样成折线，再按误差阈值递归删除偏差最小的点，全局最大偏差有界、实现简单稳健。" +
            "输出 Linear 关键帧，平滑度略降。",
            "切线感知简化。保留 Unity 贝塞尔切线结构，逐帧尝试删除中间关键帧并用相邻切线补偿，重建误差超过阈值则保留。" +
            "压缩质量好、平滑度损失小，耗时中等。",
            "动态规划最优分段。在稠密采样点上求满足误差预算的最少线性分段（全局最优），质量最高但耗时较长。" +
            "输出 Linear 关键帧。",
        };
    }

    /// <summary>流式多项式关键帧缩减器（零第三方依赖）</summary>
    public class ReductionCurve
    {
        public enum CurveType
        {
            Discrete, // 0 阶：适合开关量（如 m_IsActive）
            Linear,   // 1 阶：线性插值
            Smooth,   // 3 阶：贝塞尔平滑（默认）
            Degree,   // 3 阶 + 角度增量 ±180° 回绕（Unity 欧拉角为角度制）
            Radian,   // 3 阶（备用，暂未启用回绕）
        }

        private readonly CurveType curveType;
        private readonly int order;
        private readonly double threshold;

        public AnimationCurve curve { get; private set; }

        private bool first;
        private float lastValue;
        private double queueHeadTime;
        private double queueHeadValue;
        private Stack<double> queueTimes;
        private Stack<double> queueValues;
        private bool hasLastKeyframe;
        private Keyframe lastKeyframe;

        public ReductionCurve(double threshold, CurveType curveType = CurveType.Smooth)
        {
            this.threshold = threshold;
            this.curveType = curveType;
            switch (curveType)
            {
                case CurveType.Discrete:
                    order = 0;
                    break;
                case CurveType.Linear:
                    order = 1;
                    break;
                default:
                    order = 3;
                    break;
            }

            curve = new AnimationCurve();
            first = true;
            queueHeadTime = 0;
            queueHeadValue = 0;
            queueTimes = new Stack<double>();
            queueValues = new Stack<double>();
            hasLastKeyframe = false;
            lastKeyframe = new Keyframe(0, 0);
        }

        private float PickValue(float c)
        {
            if (first)
            {
                first = false;
                lastValue = c;
            }
            if (curveType == CurveType.Degree)
            {
                c -= lastValue;
                c = (((c + 180) % 360 + 360) % 360) - 180;
                c += lastValue;
                lastValue = c;
            }
            return c;
        }

        public void Tick(float t, float value)
        {
            float v = PickValue(value);
            if (queueTimes.Count == 0)
            {
                queueHeadTime = t;
                queueHeadValue = v;
            }
            queueTimes.Push(t - queueHeadTime);
            queueValues.Push(v - queueHeadValue);
            if (queueTimes.Count <= order + 1)
            {
                return;
            }

            double[] ts = queueTimes.ToArray();
            double[] vs = queueValues.ToArray();
            double[] coeffs = PolynomialFit.Fit(ts, vs, order);
            double res = 0;
            for (int i = 0; i < ts.Length; i++)
            {
                double ev = PolynomialFit.Evaluate(coeffs, ts[i]);
                double d = vs[i] - ev;
                res += d * d;
            }
            if (res > threshold)
            {
                Flush(false);
            }
        }

        public void Done()
        {
            Flush(true);
        }

        private Keyframe CreateKeyframe(double t, double[] coeffs)
        {
            double t0 = queueHeadTime;
            double t1 = queueHeadTime + t;
            double p0 = queueHeadValue + coeffs[0];
            double p1 = p0;
            for (int j = 1; j <= order; j++)
            {
                p1 += coeffs[j] * Math.Pow(t, j);
            }
            double v0 = 0;
            double v1 = 0;
            if (order == 1)
            {
                v0 = v1 = coeffs[1];
            }
            else if (order == 3)
            {
                v0 = coeffs[1];
                v1 = coeffs[1] + 2 * coeffs[2] * t + 3 * coeffs[3] * t * t;
            }

            if (!hasLastKeyframe)
            {
                hasLastKeyframe = true;
                lastKeyframe = new Keyframe((float)t0, (float)p0, 0, 0);
            }

            lastKeyframe.outTangent = (float)v0;
            Keyframe keyframe = new Keyframe((float)t1, (float)p1, (float)v1, 0);
            return keyframe;
        }

        private void Flush(bool final)
        {
            if (queueTimes.Count == 0) return;

            double qht = 0;
            double qhv = 0;
            Stack<double> qt = new Stack<double>();
            Stack<double> qv = new Stack<double>();
            if (!final)
            {
                if (order == 3)
                {
                    double t1 = queueTimes.Pop();
                    double t0 = queueTimes.Pop();
                    qht = queueHeadTime + t0;
                    qt.Push(0);
                    qt.Push(t1 - t0);
                    double v1 = queueValues.Pop();
                    double v0 = queueValues.Pop();
                    qhv = queueHeadValue + v0;
                    qv.Push(0);
                    qv.Push(v1 - v0);
                    queueTimes.Push(t0);
                    queueValues.Push(v0);
                }
                else
                {
                    double t0 = queueTimes.Pop();
                    qht = queueHeadTime + t0;
                    qt.Push(0);
                    double v0 = queueValues.Pop();
                    qhv = queueHeadValue + v0;
                    qv.Push(0);
                }
            }
            double[] ts = queueTimes.ToArray();
            double[] vs = queueValues.ToArray();
            double[] coeffs = PolynomialFit.Fit(ts, vs, order);
            Keyframe keyframe = CreateKeyframe(ts[0], coeffs);
            int res = curve.AddKey(lastKeyframe);
            if (res == -1)
            {
                Debug.LogError("Failed to add keyframe");
            }
            if (final || order != 3)
            {
                curve.AddKey(keyframe);
                hasLastKeyframe = false;
            }
            else
            {
                lastKeyframe = keyframe;
            }

            queueHeadTime = qht;
            queueHeadValue = qhv;
            queueTimes = qt;
            queueValues = qv;
        }
    }

    /// <summary>
    /// 最小二乘多项式拟合（零第三方依赖）。
    /// 通过正规方程 A^T A c = A^T y 求解，采用部分主元高斯消元。
    /// 返回升序系数数组（长度 = order + 1）；数据点不足时自动降低阶数，高位补 0；
    /// 矩阵奇异时回退为 0 阶（均值）拟合。
    /// </summary>
    internal static class PolynomialFit
    {
        public static double[] Fit(double[] xs, double[] ys, int order)
        {
            if (xs == null || ys == null || xs.Length == 0 || xs.Length != ys.Length)
            {
                return new double[order + 1];
            }

            int n = xs.Length;
            int m = Math.Min(order, n - 1) + 1; // 数据点不足 order+1 个时降低阶数
            if (m <= 0) m = 1;

            // 构造正规方程
            double[,] ata = new double[m, m];
            double[] aty = new double[m];
            for (int i = 0; i < n; i++)
            {
                double x = xs[i];
                double y = ys[i];
                double xp = 1.0;
                for (int j = 0; j < m; j++)
                {
                    aty[j] += xp * y;
                    double row = xp;
                    double xk = 1.0;
                    for (int k = j; k < m; k++)
                    {
                        ata[j, k] += row * xk;
                        xk *= x;
                    }
                    xp *= x;
                }
            }
            for (int j = 0; j < m; j++)
            {
                for (int k = 0; k < j; k++)
                {
                    ata[j, k] = ata[k, j];
                }
            }

            double[] c = SolveGaussian(ata, aty);
            if (c == null)
            {
                // 奇异矩阵：回退到 0 阶（均值）
                c = new double[m];
                double sum = 0;
                for (int i = 0; i < n; i++) sum += ys[i];
                c[0] = sum / n;
            }

            double[] coeffs = new double[order + 1];
            Array.Copy(c, coeffs, m);
            return coeffs;
        }

        /// <summary>多项式求值：Σ coeffs[i] * x^i（霍纳法则）</summary>
        public static double Evaluate(double[] coeffs, double x)
        {
            if (coeffs == null || coeffs.Length == 0) return 0;
            double result = 0;
            for (int i = coeffs.Length - 1; i >= 0; i--)
            {
                result = result * x + coeffs[i];
            }
            return result;
        }

        private static double[] SolveGaussian(double[,] a, double[] b)
        {
            int m = b.Length;
            double[,] aug = new double[m, m + 1];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++) aug[i, j] = a[i, j];
                aug[i, m] = b[i];
            }

            for (int col = 0; col < m; col++)
            {
                // 部分主元
                int pivot = col;
                double maxAbs = Math.Abs(aug[col, col]);
                for (int r = col + 1; r < m; r++)
                {
                    double v = Math.Abs(aug[r, col]);
                    if (v > maxAbs)
                    {
                        maxAbs = v;
                        pivot = r;
                    }
                }
                if (maxAbs < 1e-15) return null; // 奇异

                if (pivot != col)
                {
                    for (int j = col; j <= m; j++)
                    {
                        double t = aug[col, j];
                        aug[col, j] = aug[pivot, j];
                        aug[pivot, j] = t;
                    }
                }

                for (int r = col + 1; r < m; r++)
                {
                    double f = aug[r, col] / aug[col, col];
                    for (int j = col; j <= m; j++) aug[r, j] -= f * aug[col, j];
                }
            }

            double[] x = new double[m];
            for (int r = m - 1; r >= 0; r--)
            {
                double s = aug[r, m];
                for (int j = r + 1; j < m; j++) s -= aug[r, j] * x[j];
                x[r] = s / aug[r, r];
            }
            return x;
        }
    }
}
#endif
