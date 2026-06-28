using System.Numerics;
using System.Runtime.Intrinsics;
using RenderingEngine.Engine;
using SoftwareRendererModels;

namespace Tests;

public class DrawTransparentPixelTests
{
    // --- Draw ---

    [Fact]
    public unsafe void Draw_TransparentPixel_LeaveSurfaceUnchanged()
    {
        uint[] surface = [BGRA.White.Value];
        fixed (uint* ptr = surface)
            DrawTransparentPixel.Draw(ptr, BGRA.Transparent.Value);
        Assert.Equal(BGRA.White.Value, surface[0]);
    }

    [Fact]
    public unsafe void Draw_OpaquePixel_WritesPixel()
    {
        uint[] surface = [BGRA.Black.Value];
        fixed (uint* ptr = surface)
            DrawTransparentPixel.Draw(ptr, BGRA.Red.Value);
        Assert.Equal(BGRA.Red.Value, surface[0]);
    }

    [Fact]
    public unsafe void Draw_DoesNotWriteAdjacentPixels()
    {
        uint[] surface = [BGRA.Red.Value, BGRA.White.Value, BGRA.Blue.Value];
        fixed (uint* ptr = surface)
            DrawTransparentPixel.Draw(ptr + 1, BGRA.Green.Value);
        Assert.Equal(BGRA.Red.Value, surface[0]);
        Assert.Equal(BGRA.Green.Value, surface[1]);
        Assert.Equal(BGRA.Blue.Value, surface[2]);
    }

    // --- Vector256 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector256_AllTransparent_WritesNothing()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector256<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_AllOpaque_WritesAll()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.Black.Value);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector256.Create(BGRA.Red.Value));

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.Red.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_TransparentLanesNotWritten()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);
        // even lanes: opaque, odd lanes: transparent
        var pixels = Vector256.Create(BGRA.Blue.Value, 0u, BGRA.Blue.Value, 0u, BGRA.Blue.Value, 0u, BGRA.Blue.Value, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels);

        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(BGRA.Blue.Value, surface[i]);
            else
                Assert.Equal(BGRA.White.Value, surface[i]);
        }
    }

    // --- Vector128 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector128_AllTransparent_WritesNothing()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector128<uint>.Zero);

        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_AllOpaque_WritesAll()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.Black.Value);
        var pixels = Vector128.Create(BGRA.Red.Value, BGRA.Green.Value, BGRA.Blue.Value, BGRA.Yellow.Value);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels);

        Assert.Equal(BGRA.Red.Value, surface[0]);
        Assert.Equal(BGRA.Green.Value, surface[1]);
        Assert.Equal(BGRA.Blue.Value, surface[2]);
        Assert.Equal(BGRA.Yellow.Value, surface[3]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_TransparentLanesNotWritten()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.White.Value);
        var pixels = Vector128.Create(BGRA.Green.Value, 0u, BGRA.Green.Value, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels);

        Assert.Equal(BGRA.Green.Value, surface[0]);
        Assert.Equal(BGRA.White.Value, surface[1]);
        Assert.Equal(BGRA.Green.Value, surface[2]);
        Assert.Equal(BGRA.White.Value, surface[3]);
    }

    // --- Vector<uint> (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector_AllTransparent_WritesNothing()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector_AllOpaque_WritesAll()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.Black.Value);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, new Vector<uint>(BGRA.Yellow.Value));

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.Yellow.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector_TransparentLanesNotWritten()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);
        uint[] pixelValues = new uint[count];
        for (int i = 0; i < count; i++)
            pixelValues[i] = i % 2 == 0 ? BGRA.Red.Value : BGRA.Transparent.Value;

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, new Vector<uint>(pixelValues));

        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(BGRA.Red.Value, surface[i]);
            else
                Assert.Equal(BGRA.White.Value, surface[i]);
        }
    }

    // --- Vector256 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector256_WithMask_TransparentPixelNotWrittenEvenIfMaskEnabled()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector256<uint>.Zero, Vector256.Create(uint.MaxValue));

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithMask_OpaquePixelNotWrittenIfMaskDisabled()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector256.Create(BGRA.Red.Value), Vector256<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithMask_OpaquePixelWrittenWhereMaskEnabled()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);
        var mask = Vector256.Create(uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector256.Create(BGRA.Blue.Value), mask);

        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(BGRA.Blue.Value, surface[i]);
            else
                Assert.Equal(BGRA.White.Value, surface[i]);
        }
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithMask_TransparencyAndMaskBothRequired()
    {
        // lane 0: opaque pixel + mask enabled   → written
        // lane 1: transparent pixel + mask enabled → not written (transparency wins)
        // lane 2: opaque pixel + mask disabled  → not written (mask wins)
        // lanes 3-7: transparent + disabled     → not written
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);
        var pixels = Vector256.Create(BGRA.Red.Value, 0u, BGRA.Red.Value, 0u, 0u, 0u, 0u, 0u);
        var mask = Vector256.Create(uint.MaxValue, uint.MaxValue, 0u, 0u, 0u, 0u, 0u, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels, mask);

        Assert.Equal(BGRA.Red.Value, surface[0]);
        for (int i = 1; i < count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    // --- Vector128 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector128_WithMask_TransparentPixelNotWrittenEvenIfMaskEnabled()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector128<uint>.Zero, Vector128.Create(uint.MaxValue));

        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithMask_OpaquePixelWrittenWhereMaskEnabled()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.White.Value);
        var mask = Vector128.Create(uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector128.Create(BGRA.Green.Value), mask);

        Assert.Equal(BGRA.Green.Value, surface[0]);
        Assert.Equal(BGRA.White.Value, surface[1]);
        Assert.Equal(BGRA.Green.Value, surface[2]);
        Assert.Equal(BGRA.White.Value, surface[3]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithMask_TransparencyAndMaskBothRequired()
    {
        // lane 0: opaque + enabled   → written
        // lane 1: transparent + enabled → not written
        // lane 2: opaque + disabled  → not written
        // lane 3: transparent + disabled → not written
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.White.Value);
        var pixels = Vector128.Create(BGRA.Blue.Value, 0u, BGRA.Blue.Value, 0u);
        var mask = Vector128.Create(uint.MaxValue, uint.MaxValue, 0u, 0u);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, pixels, mask);

        Assert.Equal(BGRA.Blue.Value, surface[0]);
        Assert.Equal(BGRA.White.Value, surface[1]);
        Assert.Equal(BGRA.White.Value, surface[2]);
        Assert.Equal(BGRA.White.Value, surface[3]);
    }

    // --- Vector<uint> with mask ---

    [Fact]
    public unsafe void DrawLine_VectorWithMask_TransparentPixelNotWrittenEvenIfMaskEnabled()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);
        uint[] maskValues = new uint[count];
        Array.Fill(maskValues, uint.MaxValue);

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, Vector<uint>.Zero, new Vector<uint>(maskValues));

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_VectorWithMask_OpaquePixelWrittenWhereMaskEnabled()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);
        uint[] maskValues = new uint[count];
        for (int i = 0; i < count; i++)
            maskValues[i] = i % 2 == 0 ? uint.MaxValue : 0u;

        fixed (uint* ptr = surface)
            DrawTransparentPixel.DrawLine(ptr, new Vector<uint>(BGRA.Yellow.Value), new Vector<uint>(maskValues));

        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(BGRA.Yellow.Value, surface[i]);
            else
                Assert.Equal(BGRA.White.Value, surface[i]);
        }
    }
}
