using BenchmarkDotNet.Attributes;
using BuildEngineFormulas;

namespace Benchmark.Benchmarks
{
    public class BuildEngineSquareRoot
    {
        private readonly (float, float)[] floatValues = new (float, float)[2048];

        public BuildEngineSquareRoot()
        {
            Random r = new();

            for (int i = 0; i < floatValues.Length; i++)
            {
                floatValues[i] = (r.NextSingle() * 1024f, r.NextSingle() * 1024f);
            }
        }

        [Benchmark(Baseline = true)]
        public uint SinglePrecisionSquareRoot()
        {
            (float a, float b) = floatValues[0];
            float mult = a * a + b * b;

            return float.ConvertToIntegerNative<uint>(MathF.Sqrt(mult));
        }


        [Benchmark]
        public uint BuildSquareRootLookup()
        {
            (float a, float b) = floatValues[0];
            float mult = a * a + b * b;
            uint mult_u = float.ConvertToIntegerNative<uint>(mult);

            return SquareRoot.Lookup(mult_u);
        }
    }
}
