using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Benchmark.Benchmarks
{
    [DisassemblyDiagnoser]

    public class VectorRotate
    {
        const int N = 8_000;

        readonly Vector<float>[] x = new Vector<float>[N];
        readonly Vector<float>[] y = new Vector<float>[N];
        readonly Vector<float>[] sin = new Vector<float>[N];
        readonly Vector<float>[] cos = new Vector<float>[N];
        readonly Vector<float>[] px = new Vector<float>[N];
        readonly Vector<float>[] py = new Vector<float>[N];
        readonly Vector<float>[] rxA = new Vector<float>[N];
        readonly Vector<float>[] ryA = new Vector<float>[N];

        public VectorRotate()
        {
            FillRandom(x, y, sin, cos, px, py);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (Vector<float>, Vector<float>) RotateOriginal(
        Vector<float> x, Vector<float> y,
        Vector<float> psin, Vector<float> pcos,
        Vector<float> px, Vector<float> py)
        {
            Vector<float> rx1 = y * pcos + x * psin;
            Vector<float> ry1 = y * psin - x * pcos;
            return (rx1 + px, ry1 + py);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (Vector<float>, Vector<float>) RotateFused(
            Vector<float> x, Vector<float> y,
            Vector<float> psin, Vector<float> pcos,
            Vector<float> px, Vector<float> py)
        {
            return (
                y * pcos + x * psin + px,
                y * psin - x * pcos + py
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (Vector<float>, Vector<float>) RotateFMA(
            Vector<float> x, Vector<float> y,
            Vector<float> psin, Vector<float> pcos,
            Vector<float> px, Vector<float> py)
        {
            var rx = px + Vector.FusedMultiplyAdd(y, pcos, x * psin);
            var ry = py + Vector.FusedMultiplyAdd(y, psin, x * (-pcos));
            return (rx, ry);
        }

        [Benchmark(Baseline = true)]
        public void RotateOriginalB()
        {
            for (int i = 0; i < N; i++)
            {
                (rxA[i], ryA[i]) = RotateOriginal(x[i], y[i], sin[i], cos[i], px[i], py[i]);
            }
        }

        [Benchmark]
        public void RotateFusedB()
        {
            for (int i = 0; i < N; i++)
            {
                (rxA[i], ryA[i]) = RotateFused(x[i], y[i], sin[i], cos[i], px[i], py[i]);
            }
        }

        [Benchmark]
        public void RotateFMAB()
        {
            for (int i = 0; i < N; i++)
            {
                (rxA[i], ryA[i]) = RotateFMA(x[i], y[i], sin[i], cos[i], px[i], py[i]);
            }
        }

        static void FillRandom(Vector<float>[] x, Vector<float>[] y, Vector<float>[] s,
                          Vector<float>[] c, Vector<float>[] px, Vector<float>[] py)
        {
            var rnd = new Random(12345);
            for (int i = 0; i < x.Length; i++)
            {
                float angle = (float)(rnd.NextDouble() * Math.PI * 2);
                float scale = (float)(rnd.NextDouble() * 200 - 100);

                s[i] = new Vector<float>((float)Math.Sin(angle));
                c[i] = new Vector<float>((float)Math.Cos(angle));

                for (int j = 0; j < Vector<float>.Count; j++)
                {
                    Unsafe.Add(ref Unsafe.As<Vector<float>, float>(ref x[i]), j) = (float)(rnd.NextDouble() * 400 - 200) * scale;
                    Unsafe.Add(ref Unsafe.As<Vector<float>, float>(ref y[i]), j) = (float)(rnd.NextDouble() * 400 - 200) * scale;
                    Unsafe.Add(ref Unsafe.As<Vector<float>, float>(ref px[i]), j) = (float)(rnd.NextDouble() * 1000 - 500);
                    Unsafe.Add(ref Unsafe.As<Vector<float>, float>(ref py[i]), j) = (float)(rnd.NextDouble() * 1000 - 500);
                }
            }
        }
    }
}
