using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal static class SharedHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int EnsureOffsetIsPositive(int length, int offset)
        {
            offset %= length;

            if (offset < 0)
            {
                offset = length + offset;
            }

            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (float rx1, float ry1) RotateVertex(
            float x, float y,
            float sin, float cos)
        {
            float rx1 = MathF.FusedMultiplyAdd(x, sin, - y * cos);
            float ry1 = MathF.FusedMultiplyAdd(x, cos, + y * sin);

            return (rx1, ry1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (float rx1, float ry1) RotateVertex(
            Point p,
            float sin, float cos,
            float px, float py)
        {
            (float x, float y) = p;

            x -= px;
            y -= py;

            float rx1 = MathF.FusedMultiplyAdd(x, sin, - y * cos);
            float ry1 = MathF.FusedMultiplyAdd(x, cos,  y * sin);

            return (rx1, ry1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (float rx1, float ry1) RotateVertex(
            float x, float y,
            float sin, float cos,
            float px, float py)
        {
            x -= px;
            y -= py;

            float rx1 = MathF.FusedMultiplyAdd(x, sin, -y * cos);
            float ry1 = MathF.FusedMultiplyAdd(x, cos, +y * sin);

            return (rx1, ry1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<int> rx1, Vector<int> ry1) RotateVertexBack(
            Vector<int> x, Vector<int> y,
            Vector<int> psin, Vector<int> pcos,
            Vector<int> px, Vector<int> py)
        {
            Vector<int> rx1 = y * pcos + x * psin;
            Vector<int> ry1 = y * psin - x * pcos;

            return (rx1 + px, ry1 + py);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<float> rx1, Vector<float> ry1) RotateVertexBack(
            Vector<float> x, Vector<float> y,
            Vector<float> psin, Vector<float> pcos,
            Vector<float> px, Vector<float> py)
        {
            Vector<float> rx = px + Vector.FusedMultiplyAdd(y, pcos, x * psin);
            Vector<float> ry = py + Vector.FusedMultiplyAdd(y, psin, x * (-pcos));

            return (rx, ry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Within<T>(T value, T from, T to)
            where T : IComparisonOperators<T, T, bool>
        {
            return value > from && value < to;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WithinInclusive<T>(T value, T from, T to)
            where T : IComparisonOperators<T, T, bool>
        {
            return value >= from && value <= to;
        }

        public static bool IsPointInPolygon(scoped ReadOnlySpan<RenderableWall> walls, Point point)
        {
            float x = point.X;
            float y = point.Y;
            bool inside = false;

            for (int i = 0; i < walls.Length; i++)
            {
                RenderableWall wall = walls[i];

                float x1 = wall.PointA.X;
                float y1 = wall.PointA.Y;
                float x2 = wall.PointB.X;
                float y2 = wall.PointB.Y;

                if (MathF.Min(y1, y2) <= y && y < MathF.Max(y1, y2) && x <= MathF.Max(x1, x2))
                {
                    float xinters = default;

                    if (y1 != y2)
                    {
                        xinters = (y - y1) * (x2 - x1) / (y2 - y1) + x1;
                    }

                    if (x1 == x2 || x <= xinters)
                    {
                        inside = !inside;
                    }
                }
            }

            return inside;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<int> EnsureOffsetIsPositive(Vector<int> length, Vector<int> offset)
        {
            Vector<int> quotient = offset / length;
            Vector<int> rem = offset - quotient * length;

            Vector<int> mask = Vector.LessThan(rem, Vector<int>.Zero);
            Vector<int> adjusted = Vector.ConditionalSelect(mask, rem + length, rem);

            return adjusted;
        }
    }
}
