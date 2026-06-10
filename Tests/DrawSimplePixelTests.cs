using System.Numerics;
using System.Runtime.Intrinsics;
using RenderingEngine.Engine;

namespace Tests;

public class DrawSimplePixelTests
{
    [Fact]
    public unsafe void Draw_WritesPixelToSurface()
    {
        uint[] surface = new uint[1];
        fixed (uint* ptr = surface)
            DrawSimplePixel.Draw(ptr, 0xDEADBEEFu);
        Assert.Equal(0xDEADBEEFu, surface[0]);
    }

    [Fact]
    public unsafe void Draw_DoesNotWriteAdjacentPixels()
    {
        uint[] surface = [0xAAAAAAAAu, 0xBBBBBBBBu, 0xCCCCCCCCu];
        fixed (uint* ptr = surface)
            DrawSimplePixel.Draw(ptr + 1, 0x12345678u);
        Assert.Equal(0xAAAAAAAAu, surface[0]);
        Assert.Equal(0x12345678u, surface[1]);
        Assert.Equal(0xCCCCCCCCu, surface[2]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WritesAllPixels()
    {
        int count = Vector256<uint>.Count; // 8
        uint[] surface = new uint[count];
        var pixels = Vector256.Create(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, pixels);

        for (int i = 0; i < count; i++)
            Assert.Equal((uint)(i + 1), surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WritesAllPixels()
    {
        uint[] surface = new uint[Vector128<uint>.Count]; // 4
        var pixels = Vector128.Create(0xAAu, 0xBBu, 0xCCu, 0xDDu);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, pixels);

        Assert.Equal(0xAAu, surface[0]);
        Assert.Equal(0xBBu, surface[1]);
        Assert.Equal(0xCCu, surface[2]);
        Assert.Equal(0xDDu, surface[3]);
    }

    [Fact]
    public unsafe void DrawLine_Vector_WritesAllPixels()
    {
        int count = Vector<uint>.Count;
        uint[] values = new uint[count];
        uint[] surface = new uint[count];
        for (int i = 0; i < count; i++)
            values[i] = (uint)(i + 1) * 0x01010101u;

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, new Vector<uint>(values));

        for (int i = 0; i < count; i++)
            Assert.Equal(values[i], surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithAllZeroMask_WritesNothing()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, 0xFFFFFFFFu);
        var pixels = Vector256.Create(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, pixels, Vector256<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(0xFFFFFFFFu, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithAllOnesMask_WritesAll()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        var pixels = Vector256.Create(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, pixels, Vector256.Create(uint.MaxValue));

        for (int i = 0; i < count; i++)
            Assert.Equal((uint)(i + 1), surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithAlternatingMask_WritesOnlyEvenLanes()
    {
        int count = Vector256<uint>.Count; // 8
        uint[] surface = new uint[count];
        uint sentinel = 0xDEADBEEFu;
        Array.Fill(surface, sentinel);
        var pixels = Vector256.Create(10u, 20u, 30u, 40u, 50u, 60u, 70u, 80u);
        var mask = Vector256.Create(uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, pixels, mask);

        Assert.Equal(10u, surface[0]);
        Assert.Equal(sentinel, surface[1]);
        Assert.Equal(30u, surface[2]);
        Assert.Equal(sentinel, surface[3]);
        Assert.Equal(50u, surface[4]);
        Assert.Equal(sentinel, surface[5]);
        Assert.Equal(70u, surface[6]);
        Assert.Equal(sentinel, surface[7]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithAllZeroMask_WritesNothing()
    {
        int count = Vector128<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, 0xFFFFFFFFu);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, Vector128.Create(1u, 2u, 3u, 4u), Vector128<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(0xFFFFFFFFu, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithAllOnesMask_WritesAll()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        var pixels = Vector128.Create(0xAAu, 0xBBu, 0xCCu, 0xDDu);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, pixels, Vector128.Create(uint.MaxValue));

        Assert.Equal(0xAAu, surface[0]);
        Assert.Equal(0xBBu, surface[1]);
        Assert.Equal(0xCCu, surface[2]);
        Assert.Equal(0xDDu, surface[3]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithAlternatingMask_WritesOnlyEvenLanes()
    {
        uint[] surface = new uint[Vector128<uint>.Count]; // 4
        uint sentinel = 0xCAFEBABEu;
        Array.Fill(surface, sentinel);
        var pixels = Vector128.Create(11u, 22u, 33u, 44u);
        var mask = Vector128.Create(uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, pixels, mask);

        Assert.Equal(11u, surface[0]);
        Assert.Equal(sentinel, surface[1]);
        Assert.Equal(33u, surface[2]);
        Assert.Equal(sentinel, surface[3]);
    }

    [Fact]
    public unsafe void DrawLine_VectorWithMask_WritesOnlyMaskedLanes()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        uint sentinel = 0xBEEFu;
        Array.Fill(surface, sentinel);
        uint[] pixelValues = new uint[count];
        uint[] maskValues = new uint[count];
        for (int i = 0; i < count; i++)
        {
            pixelValues[i] = (uint)(i + 1) * 100u;
            maskValues[i] = i % 2 == 0 ? uint.MaxValue : 0u;
        }

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, new Vector<uint>(pixelValues), new Vector<uint>(maskValues));

        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(pixelValues[i], surface[i]);
            else
                Assert.Equal(sentinel, surface[i]);
        }
    }

    [Fact]
    public unsafe void DrawLine_VectorWithAllZeroMask_WritesNothing()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, 0xFFFFFFFFu);
        uint[] pixelValues = new uint[count];
        for (int i = 0; i < count; i++)
            pixelValues[i] = (uint)(i + 1);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, new Vector<uint>(pixelValues), Vector<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(0xFFFFFFFFu, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_VectorWithAllOnesMask_WritesAll()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        uint[] pixelValues = new uint[count];
        for (int i = 0; i < count; i++)
            pixelValues[i] = (uint)(i + 1) * 111u;

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, new Vector<uint>(pixelValues), new Vector<uint>(uint.MaxValue));

        for (int i = 0; i < count; i++)
            Assert.Equal(pixelValues[i], surface[i]);
    }
}
