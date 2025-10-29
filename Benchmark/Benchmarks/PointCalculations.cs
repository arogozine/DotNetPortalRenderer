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
    }

    [DisassemblyDiagnoser]
    public class PointCalculations2
    {
        //private readonly Point[] points;
        private readonly Point pointA;
        private readonly Point pointB;

        public PointCalculations2()
        {
            Random random = new();
            pointA = new Point(random.NextSingle(), random.NextSingle());
            pointB = new Point(random.NextSingle(), random.NextSingle());
            /*
            points = new Point[10_000];
            Random random = new();

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Point(random.NextSingle(), random.NextSingle());
            }
            */
        }

        /*
        [Benchmark]
        public int Compare()
        {
            Point x = pointA;
            Point y = pointB;

            float xcx = x.X;
            float xcy = x.Y;
            float ycx = y.X;
            float ycy = y.Y;

            float xd = xcx * xcx + xcy * xcy;
            float yd = ycx * ycx + ycy * ycy;

            if (xd == yd)
            {
                return 0;
            }

            return xd < yd ? -1 : 1;
        }

        [Benchmark]
        public float Compare_Vector64()
        {
            Point x = pointA;
            Point y = pointB;

            var xVector = Unsafe.As<Point, Vector64<float>>(ref x);
            var yVector = Unsafe.As<Point, Vector64<float>>(ref y);

            var xd = Vector64.Sum(xVector * xVector);
            var yd = Vector64.Sum(yVector * yVector);

            if (xd == yd)
            {
                return 0;
            }

            return xd < yd ? -1 : 1;
        }

        [Benchmark]
        [SkipLocalsInit]
        public int Compare2()
        {
            Point x = pointA;
            Point y = pointB;

            float xcx = x.X;
            float xcy = x.Y;
            float ycx = y.X;
            float ycy = y.Y;

            float xd = xcx * xcx + xcy * xcy;
            float yd = ycx * ycx + ycy * ycy;

            if (xd == yd)
            {
                return 0;
            }

            return xd < yd ? -1 : 1;
        }

        [Benchmark]
        [SkipLocalsInit]
        public float Compare2_Vector64()
        {
            Point x = pointA;
            Point y = pointB;

            var xVector = Unsafe.As<Point, Vector64<float>>(ref x);
            var yVector = Unsafe.As<Point, Vector64<float>>(ref y);

            var xd = Vector64.Sum(xVector * xVector);
            var yd = Vector64.Sum(yVector * yVector);

            if (xd == yd)
            {
                return 0;
            }

            return xd < yd ? -1 : 1;
        }
        */


        [Benchmark]
        [SkipLocalsInit]
        public int Compare3_Vector64_1()
        {
            Point x = pointA;
            Point y = pointB;

            // Vector64.LoadUnsafe(ref x)

            var xVector = Unsafe.As<Point, Vector64<float>>(ref x);
            var yVector = Unsafe.As<Point, Vector64<float>>(ref y);

            var xd = Vector64.Sum(xVector * xVector);
            var yd = Vector64.Sum(yVector * yVector);

            if (xd == yd)
            {
                return 0;
            }

            return xd < yd ? -1 : 1;
        }

        [Benchmark]
        [SkipLocalsInit]
        public float Compare3_Vector64_2()
        {
            Point x = pointA;
            Point y = pointB;

            var xVector = Vector64.LoadUnsafe(ref Unsafe.As<Point, float>( ref x));
            var yVector = Vector64.LoadUnsafe(ref Unsafe.As<Point, float>(ref y));

            var xd = Vector64.Sum(xVector * xVector);
            var yd = Vector64.Sum(yVector * yVector);

            if (xd == yd)
            {
                return 0;
            }

            return xd < yd ? -1 : 1;
        }
    }
}
