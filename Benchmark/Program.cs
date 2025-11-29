using Benchmark.Benchmarks;
using BenchmarkDotNet.Running;

namespace Benchmark
{


    internal class Program
    {
        static void Main(string[] args)
        {
            // How to run benchmark:
            // BenchmarkRunner.Run<BenchmarkClassName>();

            BenchmarkRunner.Run<PointCalculations2>();
        }
    }
}
