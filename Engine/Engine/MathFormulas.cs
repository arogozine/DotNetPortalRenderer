using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal static class MathFormulas
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Span<T> AlignSpan<T>(Span<T> span)
            where T : unmanaged 
        {
            int alignment = Vector<byte>.Count;

            ref T r0 = ref MemoryMarshal.GetReference(span);
            byte* ptr = (byte*)Unsafe.AsPointer(ref r0);

            // Calculate misalignment
            nuint misalignment = (nuint)ptr & (nuint)(alignment - 1);

            if (misalignment == 0)
            {
                return span;
            }

            int offset = alignment - (int)misalignment;
            (int quotient, int rem) = Math.DivRem(offset, sizeof(T));

            if (rem != 0)
            {
                quotient ++;
            }

            // assume span is big enough

            return span[quotient..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (float rx1, float ry1) RotateVertex(
            float x, float y,
            float sin, float cos)
        {
            float rx1 = x * sin - y * cos;
            float ry1 = x * cos + y * sin;

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

            float rx1 = x * sin - y * cos;
            float ry1 = x * cos + y * sin;

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
            Vector<float> rx1 = y * pcos + x * psin;
            Vector<float> ry1 = y * psin - x * pcos;

            return (rx1 + px, ry1 + py);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<int> SetAllZerosToOne(Vector<int> input)
        {
            Vector<int> zeroMask = Vector.Equals(input, Vector<int>.Zero);
            Vector<int> onesVector = Vector<int>.One;
            return Vector.ConditionalSelect(zeroMask, onesVector, input);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Within(int value, int from, int to)
        {
            return value > from && value < to;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Within(float value, float from, float to)
        {
            return value > from && value < to;
        }

        public static bool IsPointInPolygon(scoped ReadOnlySpan<Wall> walls, Point point)
        {
            float x = point.X;
            float y = point.Y;
            bool inside = false;

            for (int i = 0; i < walls.Length; i++)
            {
                Wall wall = walls[i];

                float x1 = wall.R1.X;
                float y1 = wall.R1.Y;
                float x2 = wall.R2.X;
                float y2 = wall.R2.Y;

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
    }
}
