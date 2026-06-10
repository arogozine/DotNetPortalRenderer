using System.Numerics;
using System.Runtime.Intrinsics;
using RenderingEngine.Engine;

namespace Tests;

public class DrawTransparentPixelTests
{
    // --- Draw ---

    [Fact]
    public unsafe void Draw_ZeroPixel_LeaveSurfaceUnchanged()
    {
        uint[] surface = [0xDEADBEEFu];
        fixed (uint* ptr = surface)
            DrawTransparentPixel.Draw(ptr, 0u);
        Assert.Equal(0xDEADBEEFu, surface[0]);
    }

    [Fact]
    public unsafe void Draw_NonZeroPixel_WritesPixel()
    {
        uint[] surface = [0u];
        fixed (uint* ptr = surface)
            DrawTransparentPixel.Draw(ptr, 0x12345678u);
        Assert.Equal(0x12345678u, surface[0]);
    }

    [Fact]
    public unsafe void Draw_DoesNotWriteAdjacentPixels()
    {
        uint[] surface = [0xAAAAu, 0xBBBBu, 0xCCCCu];
        fixed (uint* ptr = surface)
            DrawTransparentPixel.Draw(ptr + 1, 0x1234u);
        Assert.Equal(0xAAAAu, surface[0]);
        Assert.Equal(0xCCCCu, surface[2]);
    }

    // --- Vector256 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector256_AllZero_WritesNothing()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, 0xFFFFFFFFu);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector256<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(0xFFFFFFFFu, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_AllNonZero_WritesAll()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        var pixels = Vector256.Create(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels);

        for (int i = 0; i < count; i++)
            Assert.Equal((uint)(i + 1), surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_ZeroLanesNotWritten()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        uint sentinel = 0xCAFEBABEu;
        Array.Fill(surface, sentinel);
        // even lanes: non-zero, odd lanes: zero
        var pixels = Vector256.Create(10u, 0u, 30u, 0u, 50u, 0u, 70u, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels);

        Assert.Equal(10u, surface[0]);
        Assert.Equal(sentinel, surface[1]);
        Assert.Equal(30u, surface[2]);
        Assert.Equal(sentinel, surface[3]);
        Assert.Equal(50u, surface[4]);
        Assert.Equal(sentinel, surface[5]);
        Assert.Equal(70u, surface[6]);
        Assert.Equal(sentinel, surface[7]);
    }

    // --- Vector128 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector128_AllZero_WritesNothing()
    {
        int count = Vector128<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, 0xFFFFFFFFu);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector128<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(0xFFFFFFFFu, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_AllNonZero_WritesAll()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        var pixels = Vector128.Create(0xAAu, 0xBBu, 0xCCu, 0xDDu);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels);

        Assert.Equal(0xAAu, surface[0]);
        Assert.Equal(0xBBu, surface[1]);
        Assert.Equal(0xCCu, surface[2]);
        Assert.Equal(0xDDu, surface[3]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_ZeroLanesNotWritten()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        uint sentinel = 0xDEADBEEFu;
        Array.Fill(surface, sentinel);
        var pixels = Vector128.Create(11u, 0u, 33u, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels);

        Assert.Equal(11u, surface[0]);
        Assert.Equal(sentinel, surface[1]);
        Assert.Equal(33u, surface[2]);
        Assert.Equal(sentinel, surface[3]);
    }

    // --- Vector<uint> (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector_AllZero_WritesNothing()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, 0xFFFFFFFFu);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(0xFFFFFFFFu, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector_AllNonZero_WritesAll()
    {
        int count = Vector<uint>.Count;
        uint[] values = new uint[count];
        uint[] surface = new uint[count];
        for (int i = 0; i < count; i++)
            values[i] = (uint)(i + 1) * 0x01010101u;

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, new Vector<uint>(values));

        for (int i = 0; i < count; i++)
            Assert.Equal(values[i], surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector_ZeroLanesNotWritten()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        uint sentinel = 0xBEEFu;
        Array.Fill(surface, sentinel);
        uint[] pixelValues = new uint[count];
        for (int i = 0; i < count; i++)
            pixelValues[i] = i % 2 == 0 ? (uint)(i + 1) * 10u : 0u;

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, new Vector<uint>(pixelValues));

        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(pixelValues[i], surface[i]);
            else
                Assert.Equal(sentinel, surface[i]);
        }
    }

    // --- Vector256 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector256_WithMask_ZeroPixelNotWrittenEvenIfMaskEnabled()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        uint sentinel = 0xABCDu;
        Array.Fill(surface, sentinel);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector256<uint>.Zero, Vector256.Create(uint.MaxValue));

        for (int i = 0; i < count; i++)
            Assert.Equal(sentinel, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithMask_NonZeroPixelNotWrittenIfMaskDisabled()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        uint sentinel = 0xABCDu;
        Array.Fill(surface, sentinel);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector256.Create(0x12345678u), Vector256<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(sentinel, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithMask_NonZeroPixelWrittenWhereMaskEnabled()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        uint sentinel = 0xFFFFFFFFu;
        Array.Fill(surface, sentinel);
        var pixels = Vector256.Create(10u, 20u, 30u, 40u, 50u, 60u, 70u, 80u);
        var mask = Vector256.Create(uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels, mask);

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
    public unsafe void DrawLine_Vector256_WithMask_TransparencyAndMaskBothRequired()
    {
        // lane 0: non-zero pixel + mask enabled  → written
        // lane 1: zero pixel + mask enabled      → not written (transparency wins)
        // lane 2: non-zero pixel + mask disabled → not written (mask wins)
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        uint sentinel = 0xDEADu;
        Array.Fill(surface, sentinel);
        var pixels = Vector256.Create(99u, 0u, 77u, 0u, 0u, 0u, 0u, 0u);
        var mask = Vector256.Create(uint.MaxValue, uint.MaxValue, 0u, 0u, 0u, 0u, 0u, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels, mask);

        Assert.Equal(99u, surface[0]);
        Assert.Equal(sentinel, surface[1]);
        Assert.Equal(sentinel, surface[2]);
        for (int i = 3; i < count; i++)
            Assert.Equal(sentinel, surface[i]);
    }

    // --- Vector128 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector128_WithMask_ZeroPixelNotWrittenEvenIfMaskEnabled()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, 0xFFFFu);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector128<uint>.Zero, Vector128.Create(uint.MaxValue));

        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(0xFFFFu, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithMask_NonZeroPixelWrittenWhereMaskEnabled()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        uint sentinel = 0xDDDDu;
        Array.Fill(surface, sentinel);
        var mask = Vector128.Create(uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector128.Create(11u, 22u, 33u, 44u), mask);

        Assert.Equal(11u, surface[0]);
        Assert.Equal(sentinel, surface[1]);
        Assert.Equal(33u, surface[2]);
        Assert.Equal(sentinel, surface[3]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithMask_TransparencyAndMaskBothRequired()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        uint sentinel = 0xBEEFu;
        Array.Fill(surface, sentinel);
        // lane 0: non-zero + enabled → write
        // lane 1: zero + enabled     → skip
        // lane 2: non-zero + disabled → skip
        // lane 3: zero + disabled     → skip
        var pixels = Vector128.Create(42u, 0u, 99u, 0u);
        var mask = Vector128.Create(uint.MaxValue, uint.MaxValue, 0u, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels, mask);

        Assert.Equal(42u, surface[0]);
        Assert.Equal(sentinel, surface[1]);
        Assert.Equal(sentinel, surface[2]);
        Assert.Equal(sentinel, surface[3]);
    }

    // --- Vector<uint> with mask ---

    [Fact]
    public unsafe void DrawLine_VectorWithMask_ZeroPixelNotWrittenEvenIfMaskEnabled()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, 0xFFFFu);
        uint[] maskValues = new uint[count];
        Array.Fill(maskValues, uint.MaxValue);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector<uint>.Zero, new Vector<uint>(maskValues));

        for (int i = 0; i < count; i++)
            Assert.Equal(0xFFFFu, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_VectorWithMask_NonZeroPixelWrittenWhereMaskEnabled()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        uint sentinel = 0xAAAAu;
        Array.Fill(surface, sentinel);
        uint[] pixelValues = new uint[count];
        uint[] maskValues = new uint[count];
        for (int i = 0; i < count; i++)
        {
            pixelValues[i] = (uint)(i + 1) * 10u;
            maskValues[i] = i % 2 == 0 ? uint.MaxValue : 0u;
        }

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, new Vector<uint>(pixelValues), new Vector<uint>(maskValues));

        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(pixelValues[i], surface[i]);
            else
                Assert.Equal(sentinel, surface[i]);
        }
    }
}
