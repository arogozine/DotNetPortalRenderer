using BenchmarkDotNet.Attributes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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

        [Benchmark(Baseline = true)]
        public void RotateFused()
        {
            for (int i = 0; i < N; i++)
            {
                (rxA[i], ryA[i]) = RotateFused(x[i], y[i], sin[i], cos[i], px[i], py[i]);
            }
        }

        [Benchmark]
        public void RotateFMA()
        {
            for (int i = 0; i < N; i++)
            {
                (rxA[i], ryA[i]) = RotateFMA(x[i], y[i], sin[i], cos[i], px[i], py[i]);
            }
        }

        [Benchmark]
        public void RotateFMA2()
        {
            for (int i = 0; i < N; i++)
            {
                (rxA[i], ryA[i]) = RotateFMA2(x[i], y[i], sin[i], cos[i], px[i], py[i]);
            }
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (Vector<float>, Vector<float>) RotateFMA2(
            Vector<float> x, Vector<float> y,
            Vector<float> psin, Vector<float> pcos,
            Vector<float> px, Vector<float> py)
        {
            if (Fma.IsSupported && Vector<float>.Count == Vector256<float>.Count)
            {
                var rx = px.AsVector256() + Fma.MultiplyAdd(y.AsVector256(), pcos.AsVector256(), x.AsVector256() * psin.AsVector256());
                var ry = py.AsVector256() + Fma.MultiplyAddSubtract(y.AsVector256(), psin.AsVector256(), x.AsVector256() * pcos.AsVector256());
                return (rx.AsVector(), ry.AsVector());
            }

            throw new NotSupportedException();
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
