using System.Numerics;
using System.Runtime.Intrinsics;
using RenderingEngine.Engine;
using SoftwareRendererModels;

namespace Tests;

public class DrawAlphaPixelTests
{
    // Mirrors the scalar BlendBGRA in DrawAlphaPixel with proper rounding.
    // AI Assisted: Updated to match Avx2.Average behavior with (a + b + 1) >> 1
    static uint BlendBGRA(BGRA dst, BGRA src)
    {
        const uint Alpha = (uint)byte.MaxValue << 24;

        uint bOut = ((uint)src.B + dst.B + 1) >> 1;
        uint gOut = ((uint)src.G + dst.G + 1) >> 1;
        uint rOut = ((uint)src.R + dst.R + 1) >> 1;

        return Alpha | (rOut << 16) | (gOut << 8) | bOut;
    }

    // Vector overloads return dst unchanged when src is transparent, else blend.
    static uint BlendBGRAVector(BGRA dst, BGRA src) =>
        src.Value == 0u ? dst.Value : BlendBGRA(dst, src);

    // --- Draw ---

    [Fact]
    public unsafe void Draw_TransparentPixel_LeaveSurfaceUnchanged()
    {
        uint[] surface = [BGRA.White.Value];
        fixed (uint* ptr = surface)
            DrawAlphaPixel.Draw(ptr, BGRA.Transparent.Value);
        Assert.Equal(BGRA.White.Value, surface[0]);
    }

    [Fact]
    public unsafe void Draw_OpaquePixel_StoresBlendedResult()
    {
        BGRA dst = BGRA.White;
        BGRA src = BGRA.Red;
        uint[] surface = [dst.Value];

        fixed (uint* ptr = surface)
            DrawAlphaPixel.Draw(ptr, src.Value);

        Assert.Equal(BlendBGRA(dst, src), surface[0]);
    }

    [Fact]
    public unsafe void Draw_DoesNotWriteAdjacentPixels()
    {
        BGRA dst = BGRA.Blue;
        BGRA src = BGRA.Green;
        uint[] surface = [BGRA.White.Value, dst.Value, BGRA.Red.Value];

        fixed (uint* ptr = surface)
            DrawAlphaPixel.Draw(ptr + 1, src.Value);

        Assert.Equal(BGRA.White.Value, surface[0]);
        Assert.Equal(BlendBGRA(dst, src), surface[1]);
        Assert.Equal(BGRA.Red.Value, surface[2]);
    }

    // --- Vector256 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector256_OpaquePixels_StoresBlendedResult()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector256.Create(BGRA.Blue.Value));

        uint expected = BlendBGRAVector(BGRA.White, BGRA.Blue);
        for (int i = 0; i < count; i++)
            Assert.Equal(expected, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_TransparentPixels_LeaveSurfaceUnchanged()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector256<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    // --- Vector128 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector128_OpaquePixels_StoresBlendedResult()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.Red.Value);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector128.Create(BGRA.Blue.Value));

        uint expected = BlendBGRAVector(BGRA.Red, BGRA.Blue);
        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(expected, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_TransparentPixels_LeaveSurfaceUnchanged()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.Green.Value);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector128<uint>.Zero);

        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(BGRA.Green.Value, surface[i]);
    }

    // --- Vector<uint> (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector_OpaquePixels_StoresBlendedResult()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.Yellow.Value);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, new Vector<uint>(BGRA.Red.Value));

        uint expected = BlendBGRAVector(BGRA.Yellow, BGRA.Red);
        for (int i = 0; i < count; i++)
            Assert.Equal(expected, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector_TransparentPixels_LeaveSurfaceUnchanged()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.Blue.Value);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.Blue.Value, surface[i]);
    }

    // --- Vector256 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector256_WithAllZeroMask_LeaveSurfaceUnchanged()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector256.Create(BGRA.Red.Value), Vector256<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.White.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithAllOnesMask_StoresBlendedResult()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector256.Create(BGRA.Blue.Value), Vector256.Create(uint.MaxValue));

        uint expected = BlendBGRAVector(BGRA.White, BGRA.Blue);
        for (int i = 0; i < count; i++)
            Assert.Equal(expected, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithAlternatingMask_OnlyMaskedLanesBlended()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.White.Value);
        var mask = Vector256.Create(uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector256.Create(BGRA.Green.Value), mask);

        uint expected = BlendBGRAVector(BGRA.White, BGRA.Green);
        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(expected, surface[i]);
            else
                Assert.Equal(BGRA.White.Value, surface[i]);
        }
    }

    // --- Vector128 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector128_WithAllZeroMask_LeaveSurfaceUnchanged()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.Red.Value);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector128.Create(BGRA.Blue.Value), Vector128<uint>.Zero);

        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(BGRA.Red.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithAlternatingMask_OnlyMaskedLanesBlended()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        Array.Fill(surface, BGRA.Red.Value);
        var mask = Vector128.Create(uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector128.Create(BGRA.Blue.Value), mask);

        uint expected = BlendBGRAVector(BGRA.Red, BGRA.Blue);
        Assert.Equal(expected, surface[0]);
        Assert.Equal(BGRA.Red.Value, surface[1]);
        Assert.Equal(expected, surface[2]);
        Assert.Equal(BGRA.Red.Value, surface[3]);
    }

    // --- Vector<uint> with mask ---

    [Fact]
    public unsafe void DrawLine_VectorWithAllZeroMask_LeaveSurfaceUnchanged()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.Green.Value);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, new Vector<uint>(BGRA.Red.Value), Vector<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(BGRA.Green.Value, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_VectorWithAlternatingMask_OnlyMaskedLanesBlended()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        Array.Fill(surface, BGRA.Green.Value);
        uint[] maskValues = new uint[count];
        for (int i = 0; i < count; i++)
            maskValues[i] = i % 2 == 0 ? uint.MaxValue : 0u;

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, new Vector<uint>(BGRA.Yellow.Value), new Vector<uint>(maskValues));

        uint expected = BlendBGRAVector(BGRA.Green, BGRA.Yellow);
        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(expected, surface[i]);
            else
                Assert.Equal(BGRA.Green.Value, surface[i]);
        }
    }
}
