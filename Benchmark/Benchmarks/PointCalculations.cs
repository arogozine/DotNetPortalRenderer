using BenchmarkDotNet.Attributes;
using RenderingEngine.Models;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Benchmark.Benchmarks
{
    [DisassemblyDiagnoser]
    public class PointCalculations
    {
        //private readonly Point[] points;
        private readonly Point point;

        public PointCalculations()
        {
            Random random = new();
            point = new Point(random.NextSingle(), random.NextSingle());
            /*
            points = new Point[10_000];
            Random random = new();

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Point(random.NextSingle(), random.NextSingle());
            }
            */
        }

        [Benchmark]
        public float Distance()
        {
            Point p = point;
            return p.X * p.X + p.Y * p.Y;
        }

        [Benchmark]
        public float Distance_Vector64()
        {
            Point p1 = point;
            Vector64<float> p = Unsafe.As<Point, Vector64<float>>(ref p1);
            return Vector64.Sum(p * p);
        }

        /*
        [Benchmark]
        public float Distance()
        {
            float f = float.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                Point p = points[i];
                float dist = p.X * p.X + p.Y * p.Y;
                f = MathF.Max(f, dist);
            }

            return f;
        }

        [Benchmark]
        public float Distance_Vector64()
        {
            float f = float.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                Vector64<float> p = Unsafe.As<Point, Vector64<float>>(ref points[i]);
                float dist = Vector64.Sum(p * p);
                f = MathF.Max(f, dist);
            }

            return f;
        }
        */
    }
}
