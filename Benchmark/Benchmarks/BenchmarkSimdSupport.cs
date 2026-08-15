using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using System.Runtime.Intrinsics.X86;

namespace Benchmark.Benchmarks;

public sealed class BenchmarkSimdSupport : ManualConfig
{
    public BenchmarkSimdSupport()
    {
        AddJob("No SIMD", ("EnableHWIntrinsic", false));

        if (Sse42.IsSupported)
        {
            AddJob("SSE42", ("EnableAVX512F", false), ("EnableAVX2", false), ("EnableAVX", false));
        }

        if (Avx.IsSupported)
        {
            AddJob("AVX", ("EnableAVX2", false));
        }

        if (Avx512F.IsSupported)
        {
            AddJob("AVX512F");
        }
    }

    public void AddJob(string name, params (string Name, bool Enabled)[] envVars)
    {
        var job = Job.Default.WithEnvironmentVariables([.. envVars.SelectMany(AsVariable)])
            .WithId(name);

        _ = AddJob(job);

        static IEnumerable<EnvironmentVariable> AsVariable((string Name, bool Enabled) param)
        {
            yield return new EnvironmentVariable($"COMPlus_{param.Name}", param.Enabled ? "1" : "0");
            yield return new EnvironmentVariable($"DOTNET_{param.Name}", param.Enabled ? "1" : "0");
        }
    }
}