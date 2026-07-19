
using BenchmarkDotNet.Attributes;
using RenderingEngine.Engine;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Benchmark.Benchmarks;

[DisassemblyDiagnoser]
[Config(typeof(BenchmarkWithWithoutAvx2))]
public unsafe partial class RenderMultipleWallLinesAvx2Gather
{

    private const uint Width = 1024;
    private const int Height = 512;
    private const int TextureWidth = 64;
    private const int TextureHeight = 64;
    private const int LaneCount = 32;

    private uint* _screen;
    private uint* _texture;
    private uint* _startY;
    private uint* _endY;
    private uint* _textureYPos;
    private uint* _textureYIncr;
    private uint* _texturePos;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);

        _screen = (uint*)NativeMemory.AlignedAlloc((nuint)Width * Height * sizeof(uint), 32);
        _texture = (uint*)NativeMemory.AlignedAlloc(TextureWidth * TextureHeight * sizeof(uint), 32);
        _startY = (uint*)NativeMemory.AlignedAlloc(LaneCount * sizeof(uint), 32);
        _endY = (uint*)NativeMemory.AlignedAlloc(LaneCount * sizeof(uint), 32);
        _textureYPos = (uint*)NativeMemory.AlignedAlloc(LaneCount * sizeof(uint), 32);
        _textureYIncr = (uint*)NativeMemory.AlignedAlloc(LaneCount * sizeof(uint), 32);
        _texturePos = (uint*)NativeMemory.AlignedAlloc(LaneCount * sizeof(uint), 32);

        for (int i = 0; i < TextureWidth * TextureHeight; i++)
        {
            _texture[i] = (uint)rand.Next();
        }

        // Staggered start/end rows per lane so the top, shared, and bottom
        // render paths inside RenderMultipleWallLinesV128/V256 all get exercised.
        for (int i = 0; i < LaneCount; i++)
        {
            uint start = (uint)rand.Next(0, 100);
            _startY[i] = start;
            _endY[i] = start + (uint)rand.Next(50, 400);
            _textureYPos[i] = (uint)rand.Next() & 0x00FF_FFFF;
            _textureYIncr[i] = (uint)rand.Next(1 << 14, 1 << 17);
            _texturePos[i] = (uint)(i * TextureHeight);
        }
    }

    [Benchmark]
    public void V256()
    {
        int stride = Vector256<uint>.Count;

        for (int x = 0; x < LaneCount; x += stride)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLinesV256(
                Width,
                (uint)x,
                TextureHeight,
                _startY + x,
                _endY + x,
                _textureYPos + x,
                _textureYIncr + x,
                _screen,
                _texturePos + x,
                _texture
            );
        }
    }

    [Benchmark]
    public void V128()
    {
        int stride = Vector128<uint>.Count;

        for (int x = 0; x < LaneCount; x += stride)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLinesV128(
                Width,
                (uint)x,
                TextureHeight,
                _startY + x,
                _endY + x,
                _textureYPos + x,
                _textureYIncr + x,
                _screen,
                _texturePos + x,
                _texture
            );
        }
    }
}
