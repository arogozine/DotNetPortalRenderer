// AI Assisted

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Benchmark.Benchmarks;

[SkipLocalsInit]
[DisassemblyDiagnoser]
public unsafe class Vector256UIntCreate
{
    private readonly uint[] values = new uint[Vector256<uint>.Count];

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = (uint)rand.Next();
        }
    }

    [Benchmark(Baseline = true)]
    public Vector256<uint> StackallocLoad()
    {
        uint* stackValues = stackalloc uint[Vector256<uint>.Count];

        for (int i = 0; i < Vector256<uint>.Count; i++)
        {
            stackValues[i] = values[i];
        }

        return Vector256.Load(stackValues);
    }


    [Benchmark]
    public Vector256<uint> ZeroWithElement2()
    {
        Vector128<uint> resultA = Vector128<uint>.Zero;
        Vector128<uint> resultB = Vector128<uint>.Zero;

        resultA = Sse41.Insert(resultA, values[0], 0);
        resultA = Sse41.Insert(resultA, values[1], 1);
        resultA = Sse41.Insert(resultA, values[2], 2);
        resultA = Sse41.Insert(resultA, values[3], 3);

        resultB = Sse41.Insert(resultB, values[4], 0);
        resultB = Sse41.Insert(resultB, values[5], 1);
        resultB = Sse41.Insert(resultB, values[6], 2);
        resultB = Sse41.Insert(resultB, values[7], 3);

        return Vector256.Create(resultA, resultB);
    }

    [Benchmark]
    public Vector256<uint> ZeroWithElement()
    {
        Vector256<uint> result = Vector256<uint>.Zero;

        result = result.WithElement(0, values[0]);
        result = result.WithElement(1, values[1]);
        result = result.WithElement(2, values[2]);
        result = result.WithElement(3, values[3]);
        result = result.WithElement(4, values[4]);
        result = result.WithElement(5, values[5]);
        result = result.WithElement(6, values[6]);
        result = result.WithElement(7, values[7]);

        /*
        for (int i = 0; i < Vector256<uint>.Count; i++)
        {
            result = result.WithElement(i, values[i]);
        }
        */

        return result;
    }

    [Benchmark]
    public Vector256<uint> UnsafeAppr()
    {
        Vector256<uint> gatherV = default;
        uint* gather = (uint*) Unsafe.AsPointer(ref gatherV);

        for (int i = 0; i < Vector256<int>.Count; i++)
        {
            gather[i] = values[i];
        }

        return gatherV;
    }
}
