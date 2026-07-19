// AI Assisted
using BenchmarkDotNet.Attributes;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Benchmark.Benchmarks;

[DisassemblyDiagnoser]
public unsafe class ClampLoadAlignedVsNonTemporal
{
    private const int Width = 1920;
    private const int PixelHeight = 1080;

    private int* ceilingStartPtr;
    private int* floorEndPtr;
    private int* wallStartPtr;
    private int* wallEndPtr;

    [GlobalSetup]
    public void Setup()
    {
        nuint alignment = (nuint)Vector<byte>.Count;
        nuint byteCount = (nuint)(Width * sizeof(int));

        ceilingStartPtr = (int*)NativeMemory.AlignedAlloc(byteCount, alignment);
        floorEndPtr = (int*)NativeMemory.AlignedAlloc(byteCount, alignment);
        wallStartPtr = (int*)NativeMemory.AlignedAlloc(byteCount, alignment);
        wallEndPtr = (int*)NativeMemory.AlignedAlloc(byteCount, alignment);

        var rand = new Random(42);

        for (int i = 0; i < Width; i++)
        {
            ceilingStartPtr[i] = rand.Next(-10, PixelHeight + 10);
            floorEndPtr[i] = rand.Next(-10, PixelHeight + 10);
            wallStartPtr[i] = rand.Next(-10, PixelHeight + 10);
            wallEndPtr[i] = rand.Next(-10, PixelHeight + 10);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        NativeMemory.AlignedFree(ceilingStartPtr);
        NativeMemory.AlignedFree(floorEndPtr);
        NativeMemory.AlignedFree(wallStartPtr);
        NativeMemory.AlignedFree(wallEndPtr);
    }

    [Benchmark(Baseline = true)]
    public void LoadAligned()
    {
        Vector<int> zero = Vector<int>.Zero;
        Vector<int> max = Vector.Create(PixelHeight - 1);

        for (int i = 0; i < Width; i += Vector<int>.Count)
        {
            Vector<int> ceilingStartV = Vector.LoadAligned(&ceilingStartPtr[i]);
            Vector<int> floorEndV = Vector.LoadAligned(&floorEndPtr[i]);

            ceilingStartV = Vector.ClampNative(ceilingStartV, zero, max);
            floorEndV = Vector.ClampNative(floorEndV, zero, max);

            Vector<int> wallStartV = Vector.LoadAligned(&wallStartPtr[i]);
            wallStartV = Vector.ClampNative(wallStartV, ceilingStartV, floorEndV);
            Vector.StoreAligned(wallStartV, &wallStartPtr[i]);

            Vector<int> wallEndV = Vector.LoadAligned(&wallEndPtr[i]);
            wallEndV = Vector.ClampNative(wallEndV, ceilingStartV, floorEndV);
            Vector.StoreAligned(wallEndV, &wallEndPtr[i]);
        }
    }

    [Benchmark]
    public void LoadAlignedNonTemporal()
    {
        Vector<int> zero = Vector<int>.Zero;
        Vector<int> max = Vector.Create(PixelHeight - 1);

        for (int i = 0; i < Width; i += Vector<int>.Count)
        {
            Vector<int> ceilingStartV = Vector.LoadAlignedNonTemporal(&ceilingStartPtr[i]);
            Vector<int> floorEndV = Vector.LoadAlignedNonTemporal(&floorEndPtr[i]);

            ceilingStartV = Vector.ClampNative(ceilingStartV, zero, max);
            floorEndV = Vector.ClampNative(floorEndV, zero, max);

            Vector<int> wallStartV = Vector.LoadAlignedNonTemporal(&wallStartPtr[i]);
            wallStartV = Vector.ClampNative(wallStartV, ceilingStartV, floorEndV);
            Vector.StoreAligned(wallStartV, &wallStartPtr[i]);

            Vector<int> wallEndV = Vector.LoadAlignedNonTemporal(&wallEndPtr[i]);
            wallEndV = Vector.ClampNative(wallEndV, ceilingStartV, floorEndV);
            Vector.StoreAligned(wallEndV, &wallEndPtr[i]);
        }
    }
}
