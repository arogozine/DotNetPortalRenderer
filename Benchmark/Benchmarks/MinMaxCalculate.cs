using BenchmarkDotNet.Attributes;
using BuildAssetLoader.Map;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Benchmark.Benchmarks;

[DisassemblyDiagnoser]
public class MinMaxCalculate
{

    private readonly uint[] _data;
    public MinMaxCalculate()
    {
        _data = new uint[1024];
        var rand = new Random();

        for (int i = 0; i < 1024; i++)
        {
            _data[i] = (uint)rand.Next(0, 255);
        }
    }

    /*
    [Benchmark(Baseline = true)]
    public (uint min_t, uint min_b, uint max_t, uint max_b) Scalar()
    {
        var startYV = Vector256.LoadUnsafe(ref _data[0]);
        var endYV = Vector256.LoadUnsafe(ref _data[Vector256<int>.Count]);

        uint min_t = uint.MaxValue, max_t = 0;
        uint min_b = uint.MaxValue, max_b = 0;

        for (int i = 0; i < Vector256<uint>.Count; i++)
        {
            uint top = startYV[i];
            min_t = Math.Min(min_t, top);
            max_t = Math.Max(max_t, top);

            uint bottom = endYV[i];
            min_b = Math.Min(min_b, bottom);
            max_b = Math.Max(max_b, bottom);
        }

        return (min_t, min_b, max_t, max_b);
    }

    [Benchmark]
    public (uint min_t, uint min_b, uint max_t, uint max_b) ScalarNoMath()
    {
        var startYV = Vector256.LoadUnsafe(ref _data[0]);
        var endYV = Vector256.LoadUnsafe(ref _data[Vector256<int>.Count]);

        uint min_t = uint.MaxValue, max_t = 0;
        uint min_b = uint.MaxValue, max_b = 0;

        for (int i = 0; i < Vector256<uint>.Count; i++)
        {
            uint top = startYV[i];
            min_t = Min(min_t, top);
            max_t = Max(max_t, top);

            uint bottom = endYV[i];
            min_b = Min(min_b, bottom);
            max_b = Max(max_b, bottom);
        }

        return (min_t, min_b, max_t, max_b);
    }


    */

    /*
    [Benchmark]
    public (uint min_t, uint min_b, uint max_t, uint max_b) Span()
    {
        ReadOnlySpan<uint> startYV = _data.AsSpan()[..Vector256<uint>.Count];
        ReadOnlySpan<uint> endYV = _data.AsSpan().Slice(128, Vector256<uint>.Count);

        uint min_t = uint.MaxValue, max_t = 0;
        uint min_b = uint.MaxValue, max_b = 0;

        for (int i = 0; i < Vector256<uint>.Count; i++)
        {
            uint top = startYV[i];
            min_t = Math.Min(min_t, top);
            max_t = Math.Max(max_t, top);

            uint bottom = endYV[i];
            min_b = Math.Min(min_b, bottom);
            max_b = Math.Max(max_b, bottom);
        }

        return (min_t, min_b, max_t, max_b);
    }

    [Benchmark]
    public (uint min_t, uint min_b, uint max_t, uint max_b) SpanNoMath()
    {
        ReadOnlySpan<uint> startYV = _data.AsSpan()[..Vector256<uint>.Count];
        ReadOnlySpan<uint> endYV = _data.AsSpan().Slice(128, Vector256<uint>.Count);

        uint min_t = uint.MaxValue, max_t = 0;
        uint min_b = uint.MaxValue, max_b = 0;

        for (int i = 0; i < Vector256<uint>.Count; i++)
        {
            uint top = startYV[i];
            min_t = Min(min_t, top);
            max_t = Max(max_t, top);

            uint bottom = endYV[i];
            min_b = Min(min_b, bottom);
            max_b = Max(max_b, bottom);
        }

        return (min_t, min_b, max_t, max_b);
    }
    */
    [Benchmark]
    public (uint min_t, uint min_b, uint max_t, uint max_b) Vector()
    {
        var startYV = Vector256.LoadUnsafe(ref _data[0]);
        var endYV = Vector256.LoadUnsafe(ref _data[Vector256<int>.Count]);

        (uint min_t, uint max_t) = GetMinMaxValue(startYV);
        (uint min_b, uint max_b) = GetMinMaxValue(endYV);


        return (min_t, min_b, max_t, max_b);
    }

    [Benchmark]
    public (uint min_t, uint min_b, uint max_t, uint max_b) Vector2()
    {
        var startYV = Vector256.LoadUnsafe(ref _data[0]);
        var endYV = Vector256.LoadUnsafe(ref _data[Vector256<int>.Count]);

        (uint min_t, uint max_t) = GetMinMaxValue2(startYV);
        (uint min_b, uint max_b) = GetMinMaxValue2(endYV);


        return (min_t, min_b, max_t, max_b);
    }

    [Benchmark]
    public (uint min_t, uint min_b, uint max_t, uint max_b) Vector3()
    {
        var startYV = Vector256.LoadUnsafe(ref _data[0]);
        var endYV = Vector256.LoadUnsafe(ref _data[Vector256<int>.Count]);

        (uint min_t, uint max_t) = GetMinMaxValue3(startYV);
        (uint min_b, uint max_b) = GetMinMaxValue3(endYV);


        return (min_t, min_b, max_t, max_b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (uint min, uint max) GetMinMaxValue3(Vector256<uint> value)
    {
        // Split 8 uint into upper 4 and lower 4
        var valueLower = value.GetLower();
        var valueUpper = value.GetUpper();

        // min 4 and 4
        var value128min = Vector128.MinNative(valueLower, valueUpper);
        // mine 2 and 2
        var value128Shuffledmin = Vector128.ShuffleNative(value128min, Vector128.Create(2U, 3U, 0U, 1U));
        value128min = Vector128.MinNative(value128min, value128Shuffledmin);
        // min 1 and 1
        value128Shuffledmin = Vector128.ShuffleNative(value128min, Vector128.Create(2U, 1U, 3U, 4U));
        value128min = Vector128.MinNative(value128min, value128Shuffledmin);

        var value128max = Vector128.MaxNative(valueLower, valueUpper);
        var value128Shuffledmax = Vector128.ShuffleNative(value128max, Vector128.Create(2U, 3U, 0U, 1U));
        value128max = Vector128.MaxNative(value128max, value128Shuffledmax);

        value128Shuffledmax = Vector128.ShuffleNative(value128max, Vector128.Create(2U, 1U, 3U, 4U));
        value128max = Vector128.MaxNative(value128max, value128Shuffledmax);

        return (value128min[0], value128max[0]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (uint min, uint max) GetMinMaxValue2(Vector256<uint> value)
    {
        var valueLower = value.GetLower();
        var valueUpper = value.GetUpper();

        var value128min = Vector128.MinNative(valueLower, valueUpper);
        var value128Shuffledmin = Vector128.ShuffleNative(value128min, Vector128.Create(2U, 3U, 0U, 1U));
        value128min = Vector128.MinNative(value128min, value128Shuffledmin);

        var value128max = Vector128.MaxNative(valueLower, valueUpper);
        var value128Shuffledmax = Vector128.ShuffleNative(value128max, Vector128.Create(2U, 3U, 0U, 1U));
        value128max = Vector128.MaxNative(value128max, value128Shuffledmax);

        uint min = Min(value128min[0], value128min[1]);
        uint max = Max(value128max[0], value128max[1]);

        return (min, max);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (uint min, uint max) GetMinMaxValue(Vector256<uint> value)
    {
        var valueLower = value.GetLower();
        var valueUpper = value.GetUpper();

        var value128min = Vector128.MinNative(valueLower, valueUpper);
        var value128Shuffledmin = Vector128.ShuffleNative(value128min, Vector128.Create(2U, 3U, 0U, 1U));
        value128min = Vector128.MinNative(value128min, value128Shuffledmin);

        var value128max = Vector128.MaxNative(valueLower, valueUpper);
        var value128Shuffledmax = Vector128.ShuffleNative(value128max, Vector128.Create(2U, 3U, 0U, 1U));
        value128max = Vector128.MaxNative(value128max, value128Shuffledmax);

        uint min = Math.Min(value128min[0], value128min[1]);
        uint max = Math.Max(value128max[0], value128max[1]);

        return (min, max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Min(uint a, uint b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Max(uint a, uint b) => a > b ? a : b;
}
