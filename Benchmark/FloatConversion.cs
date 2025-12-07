using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;

namespace Benchmark
{
    [DisassemblyDiagnoser(printSource: true, exportCombinedDisassemblyReport: true)]
    public class FloatConversion
    {
        private readonly float[] Meh;

        public FloatConversion()
        {
            Meh = new float[5000];
            var rand = new Random();

            for (int i = 0; i < Meh.Length; i++)
            {
                Meh[i] = rand.NextSingle();
            }
        }

        [Benchmark]
        public int BaseLine() {
            int ans = 0;
            for (int i = 0; i < Meh.Length; i++)
            {
                ans = (int)Meh[i];
            }
            return ans;
        }

        [Benchmark]
        public int FloatingPointTrick()
        {
            int ans = 0;
            for (int i = 0; i < Meh.Length; i++)
            {
                ans = FastToInt(Meh[i]);
            }
            return ans;
        }

        [Benchmark]
        public int FloatingPointTrick2()
        {
            int ans = 0;
            for (int i = 0; i < Meh.Length; i++)
            {
                ans = FastToInt(Meh[i]);
            }
            return ans;
        }

        [Benchmark]
        public int FloatingPointTrickInlined()
        {
            const float magic = 12582912.0f;

            int ans = 0;
            for (int i = 0; i < Meh.Length; i++)
            {
                float biased = Meh[i] + magic;
                int result = Unsafe.As<float, int>(ref biased);
                ans = (result - 0x4B400000);
            }
            return ans;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastToInt(float f)
        {
            // Formula AI assisted
            // Found from https://www.cs.uaf.edu/2009/fall/cs301/lecture/12_09_float_to_int.html

            // Bias constant: 2^23 + 2^22 (to handle rounding)
            const float magic = 12582912.0f; // 1.5 * 2^23

            // Add bias
            float biased = f + magic;

            // Reinterpret as int
            int result = Unsafe.As<float, int>(ref biased);

            return result - 0x4B400000; // subtract bias offset
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastToInt2(float f)
        {
            // Formula AI assisted
            // Found from https://www.cs.uaf.edu/2009/fall/cs301/lecture/12_09_float_to_int.html

            // Bias constant: 2^23 + 2^22 (to handle rounding)
            const float magic = 12582912.0f; // 1.5 * 2^23

            // Add bias
            float biased = f + magic;

            // Reinterpret as int
            ref int result = ref Unsafe.As<float, int>(ref biased);

            return result - 0x4B400000; // subtract bias offset
        }
    }
}
