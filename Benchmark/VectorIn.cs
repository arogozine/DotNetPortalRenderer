using BenchmarkDotNet.Attributes;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Benchmark
{
    public class VectorIn
    {
        [Benchmark(Baseline = true)]
        public Vector<float> RotateBack1() {
            Vector<float> x = Vector.Create(7f);
            Vector<float> y = Vector.Create(3f);
            Vector<float> psin = Vector.Create(0.3f);
            Vector<float> pcos = Vector.Create(0.7f);
            Vector<float> px = Vector.Create(1f);
            Vector<float> py = Vector.Create(2f);

            Vector<float> sum = Vector.Create(0f);
            Vector<float> rx1, ry1;

            for (int i = 0; i < 5000; i++)
            {
                (rx1, ry1) = RotateVertexBack(x, y, psin, pcos, px, py);
                sum += rx1;
                sum += ry1;
            }

            return sum;
        }

        [Benchmark]
        public Vector<float> RotateBack2()
        {
            Vector<float> x = Vector.Create(7f);
            Vector<float> y = Vector.Create(3f);
            Vector<float> psin = Vector.Create(0.3f);
            Vector<float> pcos = Vector.Create(0.7f);
            Vector<float> px = Vector.Create(1f);
            Vector<float> py = Vector.Create(2f);

            Vector<float> sum = Vector.Create(0f);
            Vector<float> rx1, ry1;

            for (int i = 0; i < 5000; i++)
            {
                (rx1, ry1) = RotateVertexBack2(x, y, psin, pcos, px, py);
                sum += rx1;
                sum += ry1;
            }

            return sum;
        }

        [Benchmark]
        public Vector<float> RotateBack3()
        {
            Vector<float> x = Vector.Create(7f);
            Vector<float> y = Vector.Create(3f);
            Vector<float> psin = Vector.Create(0.3f);
            Vector<float> pcos = Vector.Create(0.7f);
            Vector<float> px = Vector.Create(1f);
            Vector<float> py = Vector.Create(2f);

            Vector<float> sum = Vector.Create(0f);
            Vector<float> rx1, ry1;

            for (int i = 0; i < 5000; i++)
            {
                (rx1, ry1) = RotateVertexBack3(in x, in y, in psin, in pcos, in px, in py);
                sum += rx1;
                sum += ry1;
            }

            return sum;
        }

        [Benchmark]
        public Vector<float> RotateBack4()
        {
            Vector<float> x = Vector.Create(7f);
            Vector<float> y = Vector.Create(3f);
            Vector<float> psin = Vector.Create(0.3f);
            Vector<float> pcos = Vector.Create(0.7f);
            Vector<float> px = Vector.Create(1f);
            Vector<float> py = Vector.Create(2f);

            Vector<float> sum = Vector.Create(0f);
            Vector<float> rx1, ry1;

            for (int i = 0; i < 5000; i++)
            {
                (rx1, ry1) = RotateVertexBack4(in x, in y, in psin, in pcos, in px, in py);
                sum += rx1;
                sum += ry1;
            }

            return sum;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector<float> rx1, Vector<float> ry1) RotateVertexBack(
            Vector<float> x, Vector<float> y,
            Vector<float> psin, Vector<float> pcos,
            Vector<float> px, Vector<float> py)
        {
            Vector<float> rx1 = y * pcos + x * psin;
            Vector<float> ry1 = y * psin - x * pcos;

            return (rx1 + px, ry1 + py);
        }

        private static (Vector<float> rx1, Vector<float> ry1) RotateVertexBack2(
            Vector<float> x, Vector<float> y,
            Vector<float> psin, Vector<float> pcos,
            Vector<float> px, Vector<float> py)
        {
            Vector<float> rx1 = y * pcos + x * psin;
            Vector<float> ry1 = y * psin - x * pcos;

            return (rx1 + px, ry1 + py);
        }

        private static (Vector<float> rx1, Vector<float> ry1) RotateVertexBack3(
            in Vector<float> x, in Vector<float> y,
            in Vector<float> psin, in Vector<float> pcos,
            in Vector<float> px, in Vector<float> py)
        {
            Vector<float> rx1 = y * pcos + x * psin;
            Vector<float> ry1 = y * psin - x * pcos;

            return (rx1 + px, ry1 + py);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector<float> rx1, Vector<float> ry1) RotateVertexBack4(
            in Vector<float> x, in Vector<float> y,
            in Vector<float> psin, in Vector<float> pcos,
            in Vector<float> px, in Vector<float> py)
        {
            Vector<float> rx1 = y * pcos + x * psin;
            Vector<float> ry1 = y * psin - x * pcos;

            return (rx1 + px, ry1 + py);
        }
    }
}
