using System;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal static class MathFormulas
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastPositiveFloatToInt(float value)
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
