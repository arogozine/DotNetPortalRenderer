using Benchmark.Benchmarks;
using BenchmarkDotNet.Running;
using RenderingEngine.Models;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Benchmark
{

    internal class Program
    {


        static void Main(string[] args)
        {
            // How to run benchmark:
            // BenchmarkRunner.Run<BenchmarkClassName>();

            BenchmarkRunner.Run<FloatConversion>();
        }
    }
}
