using BenchmarkDotNet.Attributes;
using SoftwareRendererModels;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Benchmark.Benchmarks;

public class MemoryLocality
{
    private const int Width = 1024;
    private const int Height = 512;

    private readonly BGRA[] space;
    public MemoryLocality()
    {
        space = new BGRA[Width * Height];
    }

    [Benchmark(Baseline = true)]
    public void PopulateByColumn()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                int index = y * Width + x;
                space[index] = BGRA.Red;
            }
        }
    }

    [Benchmark]
    public void PopulateByRow()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int index = y * Width + x;
                space[index] = BGRA.Red;
            }
        }
    }

    [Benchmark]
    public void PopulateByRowAvx2()
    {

        Vector256<uint> redV = Vector256.Create(BGRA.Red.Value);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x += Vector256<uint>.Count)
            {
                int index = y * Width + x;
                Vector256.StoreUnsafe(redV, ref Unsafe.As<uint[]>(space)[index]);
            }
        }
    }
}
