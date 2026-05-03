using BenchmarkDotNet.Attributes;
using RenderingEngine.Models;

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
}
