using System.Numerics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using RenderingEngine.Engine;
using RenderingEngine.Tooling;

namespace Benchmark.Benchmarks;

public unsafe class LoadVsCalculate
{
    private AlignedMemoryPool memoryPool;
    private int x = 7;
    private readonly int PixelWidth = 1280;
    private readonly int PixelHeight = 720;
    private float* xMapPosMultiplierCache;
    private float* cameraHeightToMapYPos;

    private Vector<int> widthDiv2V;
    private Vector<float> xPosIncrV;
    private Vector<int> xIncrV;
    private Vector<int> widthDiv2V2;
    
    [GlobalSetup]
    public void Setup()
    {
        x = new Random().Next(0, PixelHeight - 32);
        
        widthDiv2V = Vector.Create(PixelWidth >> 1);
        xPosIncrV = Vector.Create(1f / (PixelWidth * -EngineConstants.HeightToWidthRatio));
        xIncrV = Vector.CreateSequence(0, 1);
        widthDiv2V2 = widthDiv2V - xIncrV;
        
        memoryPool = AlignedMemoryPool.GeneratePool(PixelWidth, 2);

        xMapPosMultiplierCache = (float*)memoryPool.GetBucketPtr(0);
        cameraHeightToMapYPos = (float*)memoryPool.GetBucketPtr(1);

        int width = PixelWidth;
        int height = PixelHeight;
        int halfHeightInt = PixelHeight / 2;

        for (int y = 0; y < height; y++)
        {
            int lower = halfHeightInt - y;
            if (lower == 0)
            {
                lower = 1;
            }

            cameraHeightToMapYPos[y] = height / (float)lower;
        }

        float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);
        int widthDiv2 = width / 2;

        for (int x = 0; x < width; x++)
        {
            xMapPosMultiplierCache[x] = (widthDiv2 - x) * xPosIncr;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        
    }

    [Benchmark(Baseline = true)]
    public Vector<float> Load()
    {
        return Vector.Load(cameraHeightToMapYPos + x);
    }

    [Benchmark]
    public Vector<float> Calculate()
    {
        Vector<int> widthDiv2V = Vector.Create(PixelWidth >> 1);
        Vector<float> xPosIncrV = Vector.Create(1f / (PixelWidth * -EngineConstants.HeightToWidthRatio));
        Vector<int> xIncrV = Vector.CreateSequence(0, 1);
        
        Vector<float> diff = Vector.ConvertToSingle(widthDiv2V - Vector.Create(x) - xIncrV);
        Vector<float> xMapPosMultiplierCacheV = diff * xPosIncrV;

        return xMapPosMultiplierCacheV;
    }
    
    [Benchmark]
    public Vector<float> Calculate2()
    {
        Vector<float> diff = Vector.ConvertToSingle(widthDiv2V2 - Vector.Create(x));
        Vector<float> xMapPosMultiplierCacheV = diff * xPosIncrV;

        return xMapPosMultiplierCacheV;
    }
}