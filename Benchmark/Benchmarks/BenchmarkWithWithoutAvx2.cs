using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using System.Runtime.Intrinsics.X86;

namespace Benchmark.Benchmarks;

public sealed class BenchmarkWithWithoutAvx2 : ManualConfig
{
    public BenchmarkWithWithoutAvx2()
    {
        if (!Avx2.IsSupported)
        {
            throw new Exception("AVX2 not supported");
        }

        _ = AddJob(Job.Default.WithEnvironmentVariables([
                    new EnvironmentVariable("COMPlus_EnableAVX2", "0")
                // new EnvironmentVariable("DOTNET_EnableAVX", "0"),
                ])
                .WithId("No AVX2")
        );

        _ = AddJob(Job.Default
            .WithId("AVX2"));
    }
}