using SoftwareRendererModels;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal static class SharedHelpers
    {
        internal static bool RefineRepeatedValues<T>(
            scoped Span<T> repeatedValues,
            scoped Span<T> repeatedValues2)
            where T : unmanaged, IBinaryInteger<T>
        {
            bool repeated = false;

            for (int i = 0; i < repeatedValues.Length; i++)
            {
                T repeat_a = repeatedValues[i];
                T repeat_b = repeatedValues2[i];
                T repeat_min = T.Min(repeat_b, repeat_a);

                repeatedValues[i] = repeat_min;

                repeated = repeated || repeat_min > T.One;
            }

            // true if there are still repeated values after filtering
            return repeated;
        }

        internal static unsafe bool PopulateRepeatedValuesInPlace<T>(
            T* values, int length)
            where T : unmanaged, IBinaryInteger<T>
        {
            bool repeated = false;

            for (int i = 0; i < length;)
            {
                T count = T.One;
                T l = values[i];

                if (l == T.Zero)
                {
                    i++;
                    continue;
                }

                for (int j = i + 1; j < length && values[j] == l; j++)
                {
                    count++;
                }

                repeated = repeated || count > T.One;

                for (; count > T.Zero; count--, i++)
                {
                    values[i] = count;
                }
            }

            return repeated;
        }

        internal static bool PopulateRepeatedValuesInPlace<T>(
            scoped Span<T> values)
            where T : unmanaged, IBinaryInteger<T>
        {
            bool repeated = false;

            for (int i = 0; i < values.Length;)
            {
                T count = T.One;
                T l = values[i];

                if (l == T.Zero)
                {
                    i++;
                    continue;
                }

                for (int j = i + 1; j < values.Length && values[j] == l; j++)
                {
                    count++;
                }

                repeated = repeated || count > T.One;

                for (; count > T.Zero; count--, i++)
                {
                    values[i] = count;
                }
            }

            return repeated;
        }

        internal static bool PopulateRepeatedValues<T>(
            scoped Span<ushort> repeatedCount,
            scoped ReadOnlySpan<T> values)
            where T : unmanaged, IBinaryInteger<T>
        {
            bool repeated = false;

            for (int i = 0; i < values.Length;)
            {
                T l = values[i];

                int j = i + 1;

                while (j < values.Length && values[j] == l)
                    j++;

                ushort count = (ushort)(j - i);
                repeated = repeated || count > 1;

                for (; count > 0; count--, i++)
                {
                    repeatedCount[i] = count;
                }
            }

            return repeated;
        }


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
        internal static Vector2 RotateVertexAroundPoint(
            float x, float y,
            float sin, float cos,
            float px, float py)
        {
            x -= px;
            y -= py;

            float rx1 = MathF.FusedMultiplyAdd(x, sin, -y * cos);
            float ry1 = MathF.FusedMultiplyAdd(x, cos, +y * sin);

            return new Vector2(rx1 + px, ry1 + py);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 RotateVertex(
            Vector2 p,
            float sin, float cos,
            float px, float py)
        {
            (float x, float y) = p;

            x -= px;
            y -= py;

            float rx1 = MathF.FusedMultiplyAdd(x, sin, -y * cos);
            float ry1 = MathF.FusedMultiplyAdd(x, cos, y * sin);

            return new(rx1, ry1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 RotateVertex(
            float x, float y,
            float sin, float cos,
            float px, float py)
        {
            x -= px;
            y -= py;

            float rx1 = MathF.FusedMultiplyAdd(x, sin, -y * cos);
            float ry1 = MathF.FusedMultiplyAdd(x, cos, +y * sin);

            return new(rx1, ry1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (float rx1, float ry1) RotateVertexBack(
            float x, float y,
            float sin, float cos,
            float px, float py)
        {
            float rx1 = px + MathF.FusedMultiplyAdd(y, cos, x * sin);
            float ry1 = py + MathF.FusedMultiplyAdd(y, sin, x * (-cos));

            return (rx1, ry1);
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
        public static T Clamp<T>(T value, T min, T max)
            where T : INumber<T>
        {
            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
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

        public static bool IsPointInPolygon(scoped ReadOnlySpan<RenderableWall> walls, Vector2 point)
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
