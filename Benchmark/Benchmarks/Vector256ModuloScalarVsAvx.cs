// AI Assisted

using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Tooling;

namespace Benchmark.Benchmarks;

[DisassemblyDiagnoser]
public class Vector256ModuloScalarVsAvx
{
    private const int RightInt = 7;
    private const uint RightUInt = 7u;

    private Vector256<int> _leftInt;
    private Vector256<uint> _leftUInt;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);

        _leftInt = Vector256.Create(
            rand.Next(int.MinValue, int.MaxValue),
            rand.Next(int.MinValue, int.MaxValue),
            rand.Next(int.MinValue, int.MaxValue),
            rand.Next(int.MinValue, int.MaxValue),
            rand.Next(int.MinValue, int.MaxValue),
            rand.Next(int.MinValue, int.MaxValue),
            rand.Next(int.MinValue, int.MaxValue),
            rand.Next(int.MinValue, int.MaxValue)
        );

        _leftUInt = Vector256.Create(
            (uint)rand.Next(),
            (uint)rand.Next(),
            (uint)rand.Next(),
            (uint)rand.Next(),
            (uint)rand.Next(),
            (uint)rand.Next(),
            (uint)rand.Next(),
            (uint)rand.Next()
        );
    }

    // Pre-AVX behavior: the operator used to always take this unrolled scalar-lane path.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<int> ScalarModulo(Vector256<int> left, int right)
    {
        return Vector256.Create(
            left[0] % right,
            left[1] % right,
            left[2] % right,
            left[3] % right,
            left[4] % right,
            left[5] % right,
            left[6] % right,
            left[7] % right
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<uint> ScalarModulo(Vector256<uint> left, uint right)
    {
        return Vector256.Create(
            left[0] % right,
            left[1] % right,
            left[2] % right,
            left[3] % right,
            left[4] % right,
            left[5] % right,
            left[6] % right,
            left[7] % right
        );
    }

    [Benchmark(Baseline = true)]
    public Vector256<int> Int_Scalar() => ScalarModulo(_leftInt, RightInt);

    [Benchmark]
    public Vector256<int> Int_Avx() => _leftInt % RightInt;

    [Benchmark]
    public Vector256<uint> UInt_Scalar() => ScalarModulo(_leftUInt, RightUInt);

    [Benchmark]
    public Vector256<uint> UInt_Avx() => _leftUInt % RightUInt;
}
