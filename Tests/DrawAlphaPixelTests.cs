using System.Numerics;
using System.Runtime.Intrinsics;
using RenderingEngine.Engine;

namespace Tests;

public class DrawAlphaPixelTests
{
    // Mirrors the scalar BlendBGRA in DrawAlphaPixel (same constants).
    static uint BlendBGRA(uint dst, uint src)
    {
        const uint a = 127;
        const uint ByteMask = 0xFF;
        const uint Alpha = (uint)byte.MaxValue << 24;

        uint bOut = ((src & ByteMask) * a + (dst & ByteMask) * a) >> 8;
        uint gOut = (((src >> 8) & ByteMask) * a + ((dst >> 8) & ByteMask) * a) >> 8;
        uint rOut = (((src >> 16) & ByteMask) * a + ((dst >> 16) & ByteMask) * a) >> 8;

        return Alpha | (rOut << 16) | (gOut << 8) | bOut;
    }

    // Vector overloads return dst unchanged when src is zero, else blend.
    static uint BlendBGRAVector(uint dst, uint src) => src == 0u ? dst : BlendBGRA(dst, src);

    // --- Draw ---

    [Fact]
    public unsafe void Draw_ZeroPixel_LeaveSurfaceUnchanged()
    {
        uint[] surface = [0xDEADBEEFu];
        fixed (uint* ptr = surface)
            DrawAlphaPixel.Draw(ptr, 0u);
        Assert.Equal(0xDEADBEEFu, surface[0]);
    }

    [Fact]
    public unsafe void Draw_NonZeroPixel_StoresBlendedResult()
    {
        uint dst = 0x00808080u;
        uint src = 0x00404040u;
        uint[] surface = [dst];

        fixed (uint* ptr = surface)
            DrawAlphaPixel.Draw(ptr, src);

        Assert.Equal(BlendBGRA(dst, src), surface[0]);
    }

    [Fact]
    public unsafe void Draw_DoesNotWriteAdjacentPixels()
    {
        uint[] surface = [0x00111111u, 0x00222222u, 0x00333333u];
        uint src = 0x00808080u;

        fixed (uint* ptr = surface)
            DrawAlphaPixel.Draw(ptr + 1, src);

        Assert.Equal(0x00111111u, surface[0]);
        Assert.Equal(BlendBGRA(0x00222222u, src), surface[1]);
        Assert.Equal(0x00333333u, surface[2]);
    }

    // --- Vector256 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector256_StoresBlendedResult()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        uint dst = 0x00404040u;
        uint src = 0x00808080u;
        Array.Fill(surface, dst);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector256.Create(src));

        uint expected = BlendBGRAVector(dst, src);
        for (int i = 0; i < count; i++)
            Assert.Equal(expected, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_ZeroSrcLeavesUnchanged()
    {
        // With the refactored logic, zero src means dst is returned unchanged.
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        uint dst = 0x00808080u;
        Array.Fill(surface, dst);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector256<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(dst, surface[i]);
    }

    // --- Vector128 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector128_StoresBlendedResult()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        uint dst = 0x00204080u;
        uint src = 0x00101010u;
        Array.Fill(surface, dst);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector128.Create(src));

        uint expected = BlendBGRAVector(dst, src);
        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(expected, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_ZeroSrcLeavesUnchanged()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        uint dst = 0x00606060u;
        Array.Fill(surface, dst);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector128<uint>.Zero);

        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(dst, surface[i]);
    }

    // --- Vector<uint> (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector_StoresBlendedResult()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        uint dst = 0x00202020u;
        uint src = 0x00404040u;
        Array.Fill(surface, dst);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, new Vector<uint>(src));

        uint expected = BlendBGRAVector(dst, src);
        for (int i = 0; i < count; i++)
            Assert.Equal(expected, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector_ZeroSrcLeavesUnchanged()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        uint dst = 0x00808080u;
        Array.Fill(surface, dst);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(dst, surface[i]);
    }

    // --- Vector256 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector256_WithAllZeroMask_LeaveSurfaceUnchanged()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        uint dst = 0x00ABCD12u;
        Array.Fill(surface, dst);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector256.Create(0x00808080u), Vector256<uint>.Zero);

        for (int i = 0; i < count; i++)
            Assert.Equal(dst, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithAllOnesMask_StoresBlendedResult()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        uint dst = 0x00404040u;
        uint src = 0x00808080u;
        Array.Fill(surface, dst);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector256.Create(src), Vector256.Create(uint.MaxValue));

        uint expected = BlendBGRAVector(dst, src);
        for (int i = 0; i < count; i++)
            Assert.Equal(expected, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector256_WithAlternatingMask_OnlyMaskedLanesUpdated()
    {
        int count = Vector256<uint>.Count;
        uint[] surface = new uint[count];
        uint dst = 0x00101010u;
        uint src = 0x00808080u;
        Array.Fill(surface, dst);
        var mask = Vector256.Create(uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector256.Create(src), mask);

        uint expected = BlendBGRAVector(dst, src);
        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(expected, surface[i]);
            else
                Assert.Equal(dst, surface[i]);
        }
    }

    // --- Vector128 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector128_WithAllZeroMask_LeaveSurfaceUnchanged()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        uint dst = 0x00123456u;
        Array.Fill(surface, dst);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector128.Create(0x00808080u), Vector128<uint>.Zero);

        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(dst, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_Vector128_WithAlternatingMask_OnlyMaskedLanesUpdated()
    {
        uint[] surface = new uint[Vector128<uint>.Count];
        uint dst = 0x00202020u;
        uint src = 0x00606060u;
        Array.Fill(surface, dst);
        var mask = Vector128.Create(uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, Vector128.Create(src), mask);

        uint expected = BlendBGRAVector(dst, src);
        Assert.Equal(expected, surface[0]);
        Assert.Equal(dst, surface[1]);
        Assert.Equal(expected, surface[2]);
        Assert.Equal(dst, surface[3]);
    }

    // --- Vector<uint> with mask ---

    [Fact]
    public unsafe void DrawLine_VectorWithAllZeroMask_LeaveSurfaceUnchanged()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        uint dst = 0x00101020u;
        Array.Fill(surface, dst);
        uint[] maskValues = new uint[count]; // all zero

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, new Vector<uint>(0x00808080u), new Vector<uint>(maskValues));

        for (int i = 0; i < count; i++)
            Assert.Equal(dst, surface[i]);
    }

    [Fact]
    public unsafe void DrawLine_VectorWithAlternatingMask_OnlyMaskedLanesUpdated()
    {
        int count = Vector<uint>.Count;
        uint[] surface = new uint[count];
        uint dst = 0x00303030u;
        uint src = 0x00909090u;
        Array.Fill(surface, dst);
        uint[] maskValues = new uint[count];
        for (int i = 0; i < count; i++)
            maskValues[i] = i % 2 == 0 ? uint.MaxValue : 0u;

        fixed (uint* ptr = surface)
            DrawAlphaPixel.DrawLine(ptr, new Vector<uint>(src), new Vector<uint>(maskValues));

        uint expected = BlendBGRAVector(dst, src);
        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                Assert.Equal(expected, surface[i]);
            else
                Assert.Equal(dst, surface[i]);
        }
    }
}
