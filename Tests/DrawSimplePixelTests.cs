using System.Numerics;
using System.Runtime.Intrinsics;
using RenderingEngine.Engine;
using SoftwareRendererModels;

namespace Tests;

public class DrawSimplePixelTests
{
    // --- Draw ---

    [Fact]
    public unsafe void Draw_WritesPixelToSurface()
    {
        uint[] surface = [BGRA.Black.Value];
        fixed (uint* ptr = surface)
            DrawSimplePixel.Draw(ptr, BGRA.Red.Value);
        Assert.Equal(BGRA.Red.Value, surface[0]);
    }

    [Fact]
    public unsafe void Draw_DoesNotWriteAdjacentPixels()
    {
        uint[] surface = [BGRA.Red.Value, BGRA.Green.Value, BGRA.Blue.Value];
        fixed (uint* ptr = surface)
            DrawSimplePixel.Draw(ptr + 1, BGRA.White.Value);
        Assert.Equal(BGRA.Red.Value, surface[0]);
        Assert.Equal(BGRA.White.Value, surface[1]);
        Assert.Equal(BGRA.Blue.Value, surface[2]);
    }

    // --- Vector256 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector256_WritesAllPixels()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.Black.Value);
        var pixels = Vector256.Create(BGRA.Red.Value);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, pixels);

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.Red.Value, surface[i]);
    }

    // --- Vector128 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector128_WritesAllPixels()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.Black.Value);
        var pixels = Vector128.Create(BGRA.Red.Value, BGRA.Green.Value, BGRA.Blue.Value, BGRA.Yellow.Value);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, pixels);

        Assert.Equal(BGRA.Red.Value, surface[0]);
        Assert.Equal(BGRA.Green.Value, surface[1]);
        Assert.Equal(BGRA.Blue.Value, surface[2]);
        Assert.Equal(BGRA.Yellow.Value, surface[3]);
    }

    // --- Vector<uint> (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector_WritesAllPixels()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.Black.Value);
        uint[] values = new uint[count];
        for (int i = 0; i < count; i++)
            values[i] = (i % 2 == 0) ? BGRA.Red.Value : BGRA.Blue.Value;

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, new Vector<uint>(values));

        for (int i = 0; i < count; i++)
            Assert.Equal(values[i], surface[i]);
    }

    // --- Vector256 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector256_WithAllZeroMask_WritesNothing()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, Vector256.Create(BGRA.Red.Value), Vector256<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithAllOnesMask_WritesAll()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.Black.Value);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, Vector256.Create(BGRA.Green.Value), Vector256.Create(uint.MaxValue));

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.Green.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithAlternatingMask_WritesOnlyMaskedLanes()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);
        var mask = Vector256.Create(uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, Vector256.Create(BGRA.Blue.Value), mask);

        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(BGRA.Blue.Value, surface[i]);
            else
                Assert.Equal(BGRA.White.Value, surface[i]);
        }
    }

    // --- Vector128 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector128_WithAllZeroMask_WritesNothing()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, Vector128.Create(BGRA.Red.Value), Vector128<uint>.Zero);

        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithAllOnesMask_WritesAll()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.Black.Value);
        var pixels = Vector128.Create(BGRA.Red.Value, BGRA.Green.Value, BGRA.Blue.Value, BGRA.Yellow.Value);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, pixels, Vector128.Create(uint.MaxValue));

        Assert.Equal(BGRA.Red.Value, surface[0]);
        Assert.Equal(BGRA.Green.Value, surface[1]);
        Assert.Equal(BGRA.Blue.Value, surface[2]);
        Assert.Equal(BGRA.Yellow.Value, surface[3]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithAlternatingMask_WritesOnlyMaskedLanes()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.White.Value);
        var mask = Vector128.Create(uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, Vector128.Create(BGRA.Blue.Value), mask);

        Assert.Equal(BGRA.Blue.Value, surface[0]);
        Assert.Equal(BGRA.White.Value, surface[1]);
        Assert.Equal(BGRA.Blue.Value, surface[2]);
        Assert.Equal(BGRA.White.Value, surface[3]);
    }

    // --- Vector<uint> with mask ---

    [Fact]
    public unsafe void DrawLine_VectorWithAllZeroMask_WritesNothing()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, new Vector<uint>(BGRA.Red.Value), Vector<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_VectorWithAllOnesMask_WritesAll()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.Black.Value);

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, new Vector<uint>(BGRA.Green.Value), new Vector<uint>(uint.MaxValue));

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.Green.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_VectorWithAlternatingMask_WritesOnlyMaskedLanes()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);
        uint[] maskValues = new uint[count];
        for (int i = 0; i < count; i++)
            maskValues[i] = i % 2 == 0 ? uint.MaxValue : 0u;

        fixed (uint* ptr = surface)
            DrawSimplePixel.DrawLine(ptr, new Vector<uint>(BGRA.Red.Value), new Vector<uint>(maskValues));

        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(BGRA.Red.Value, surface[i]);
            else
                Assert.Equal(BGRA.White.Value, surface[i]);
        }
    }
}
