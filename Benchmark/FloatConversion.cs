using BenchmarkDotNet.Attributes;
using RenderingEngine.Engine;

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

        [Benchmark(Baseline = true)]
        public int StandardCastFloatAsInt() {
            int ans = 0;
            for (int i = 0; i < Meh.Length; i++)
            {
                ans = (int)Meh[i];
            }
            return ans;
        }

        [Benchmark()]
        public int StandardCastFloatAsIntNative()
        {
            int ans = 0;
            for (int i = 0; i < Meh.Length; i++)
            {
                ans = float.ConvertToIntegerNative<int>(Meh[i]);
            }
            return ans;
        }
    }
}
