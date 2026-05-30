using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using RenderingEngine.Engine;
// using BenchmarkDotNet.Running;

namespace Benchmark.Benchmarks;

// [MemoryDiagnoser]
[DisassemblyDiagnoser(printSource: true)]
public class LoopBenchmark
{
    private uint[] _startY = null!;
    private uint[] _endY = null!;

    [Params(256)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _startY = new uint[Count];
        _endY = new uint[Count];

        for (int i = 0; i < Count; i++)
        {
            _startY[i] = (uint)rng.Next(0, 10_000);
            _endY[i] = (uint)rng.Next(0, 10_000);
        }
    }

    [Benchmark(Baseline = true)]
    public (uint min_t, uint max_t, uint min_b, uint max_b) MathLoop()
    {
        ref uint startY = ref _startY[0];
        ref uint endY = ref _endY[0];
        int count = Count;

        uint min_t = uint.MaxValue, max_t = uint.MinValue;
        uint min_b = uint.MaxValue, max_b = uint.MinValue;

        for (int i = 0; i < count; i++)
        {
            ref uint top = ref Unsafe.Add(ref startY, i);
            min_t = Math.Min(min_t, top);
            max_t = Math.Max(max_t, top);

            ref uint bottom = ref Unsafe.Add(ref endY, i);
            min_b = Math.Min(min_b, bottom);
            max_b = Math.Max(max_b, bottom);
        }

        return (min_t, max_t, min_b, max_b);
    }

    [Benchmark]
    public (uint min_t, uint max_t, uint min_b, uint max_b) MathFormulasLoop()
    {
        ref uint startY = ref _startY[0];
        ref uint endY = ref _endY[0];
        int count = Count;

        uint min_t = uint.MaxValue, max_t = uint.MinValue;
        uint min_b = uint.MaxValue, max_b = uint.MinValue;

        for (int i = 0; i < count; i++)
        {
            ref uint top = ref Unsafe.Add(ref startY, i);
            min_t = MathFormulas.Min(min_t, top);
            max_t = MathFormulas.Max(max_t, top);

            ref uint bottom = ref Unsafe.Add(ref endY, i);
            min_b = MathFormulas.Min(min_b, bottom);
            max_b = MathFormulas.Max(max_b, bottom);
        }

        return (min_t, max_t, min_b, max_b);
    }

    /*
    /// <summary>
    /// Single loop: iterates startY and endY together, updating all four
    /// min/max accumulators in one pass.
    /// </summary>
    [Benchmark(Baseline = true)]
    public (uint min_t, uint max_t, uint min_b, uint max_b) CombinedLoop()
    {
        ref uint startY = ref _startY[0];
        ref uint endY = ref _endY[0];
        int count = Count;

        uint min_t = uint.MaxValue, max_t = uint.MinValue;
        uint min_b = uint.MaxValue, max_b = uint.MinValue;

        for (int i = 0; i < count; i++)
        {
            ref uint top = ref Unsafe.Add(ref startY, i);
            min_t = Math.Min(min_t, top);
            max_t = Math.Max(max_t, top);

            ref uint bottom = ref Unsafe.Add(ref endY, i);
            min_b = Math.Min(min_b, bottom);
            max_b = Math.Max(max_b, bottom);
        }

        return (min_t, max_t, min_b, max_b);
    }

    /// <summary>
    /// Two separate loops: first pass covers startY, second pass covers endY.
    /// Each loop only touches one array, which may help the hardware prefetcher
    /// but at the cost of iterating twice.
    /// </summary>
    [Benchmark]
    public (uint min_t, uint max_t, uint min_b, uint max_b) SeparateLoops()
    {
        ref uint startY = ref _startY[0];
        ref uint endY = ref _endY[0];
        int count = Count;

        uint min_t = uint.MaxValue, max_t = uint.MinValue;
        uint min_b = uint.MaxValue, max_b = uint.MinValue;

        for (int i = 0; i < count; i++)
        {
            ref uint top = ref Unsafe.Add(ref startY, i);
            min_t = Math.Min(min_t, top);
            max_t = Math.Max(max_t, top);
        }

        for (int i = 0; i < count; i++)
        {
            ref uint bottom = ref Unsafe.Add(ref endY, i);
            min_b = Math.Min(min_b, bottom);
            max_b = Math.Max(max_b, bottom);
        }

        return (min_t, max_t, min_b, max_b);
    }
    */
}