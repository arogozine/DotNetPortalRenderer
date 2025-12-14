using System.Numerics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal static class MathFormulas
    {

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
        internal static int FastPositiveFloatToInt(float value)
        {
            if (Sse.IsSupported) // Ensure x86, x64
            {
                // https://www.cs.uaf.edu/2009/fall/cs301/lecture/12_09_float_to_int.html
                const float magic = 1 << 23;
                float resultF = value + magic;
                ref int result = ref Unsafe.As<float, int>(ref resultF);

                return result & 0x7FffFF;
            }

            return (int)value;
        }
    }
}
