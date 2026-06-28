using System.Numerics;
using System.Runtime.Intrinsics;
using RenderingEngine.Engine;
using SoftwareRendererModels;

namespace Tests;

// For textures with no transparency (all pixels fully opaque), DrawSimplePixel
// and DrawTransparentPixel must produce identical results.
public class DrawPixelParityTests
{
    // --- Draw ---

    [Fact]
    public unsafe void Draw_OpaquePixel_SimpleAndTransparentProduceSameResult()
    {
        uint[] simpleSurface = [BGRA.White.Value];
        uint[] transparentSurface = [BGRA.White.Value];

        fixed (uint* sPtr = simpleSurface)
            DrawSimplePixel.Draw(sPtr, BGRA.Red.Value);

        fixed (uint* tPtr = transparentSurface)
            DrawTransparentPixel.Draw(tPtr, BGRA.Red.Value);

        Assert.Equal(simpleSurface[0], transparentSurface[0]);
    }

    // --- Vector256 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector256_OpaquePixels_SimpleAndTransparentProduceSameResult()
    {
        int count = Vector256<uint>.Count;
        uint[] simpleSurface = new uint[count];
        uint[] transparentSurface = new uint[count];
        Array.Fill(simpleSurface, BGRA.Black.Value);
        Array.Fill(transparentSurface, BGRA.Black.Value);
        var pixels = Vector256.Create(BGRA.Blue.Value);

        fixed (uint* sPtr = simpleSurface)
            DrawSimplePixel.DrawLine(sPtr, pixels);

        fixed (uint* tPtr = transparentSurface)
            DrawTransparentPixel.DrawLine(tPtr, pixels);

        for (int i = 0; i < count; i++)
            Assert.Equal(simpleSurface[i], transparentSurface[i]);
    }

    // --- Vector128 (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector128_OpaquePixels_SimpleAndTransparentProduceSameResult()
    {
        uint[] simpleSurface = new uint[Vector128<uint>.Count];
        uint[] transparentSurface = new uint[Vector128<uint>.Count];
        Array.Fill(simpleSurface, BGRA.Black.Value);
        Array.Fill(transparentSurface, BGRA.Black.Value);
        var pixels = Vector128.Create(BGRA.Red.Value, BGRA.Green.Value, BGRA.Blue.Value, BGRA.Yellow.Value);

        fixed (uint* sPtr = simpleSurface)
            DrawSimplePixel.DrawLine(sPtr, pixels);

        fixed (uint* tPtr = transparentSurface)
            DrawTransparentPixel.DrawLine(tPtr, pixels);

        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(simpleSurface[i], transparentSurface[i]);
    }

    // --- Vector<uint> (no mask) ---

    [Fact]
    public unsafe void DrawLine_Vector_OpaquePixels_SimpleAndTransparentProduceSameResult()
    {
        int count = Vector<uint>.Count;
        uint[] simpleSurface = new uint[count];
        uint[] transparentSurface = new uint[count];
        Array.Fill(simpleSurface, BGRA.Black.Value);
        Array.Fill(transparentSurface, BGRA.Black.Value);

        fixed (uint* sPtr = simpleSurface)
            DrawSimplePixel.DrawLine(sPtr, new Vector<uint>(BGRA.Yellow.Value));

        fixed (uint* tPtr = transparentSurface)
            DrawTransparentPixel.DrawLine(tPtr, new Vector<uint>(BGRA.Yellow.Value));

        for (int i = 0; i < count; i++)
            Assert.Equal(simpleSurface[i], transparentSurface[i]);
    }

    // --- Vector256 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector256_WithMask_OpaquePixels_SimpleAndTransparentProduceSameResult()
    {
        int count = Vector256<uint>.Count;
        uint[] simpleSurface = new uint[count];
        uint[] transparentSurface = new uint[count];
        Array.Fill(simpleSurface, BGRA.White.Value);
        Array.Fill(transparentSurface, BGRA.White.Value);
        var pixels = Vector256.Create(BGRA.Blue.Value);
        var mask = Vector256.Create(uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* sPtr = simpleSurface)
            DrawSimplePixel.DrawLine(sPtr, pixels, mask);

        fixed (uint* tPtr = transparentSurface)
            DrawTransparentPixel.DrawLine(tPtr, pixels, mask);

        for (int i = 0; i < count; i++)
            Assert.Equal(simpleSurface[i], transparentSurface[i]);
    }

    // --- Vector128 with mask ---

    [Fact]
    public unsafe void DrawLine_Vector128_WithMask_OpaquePixels_SimpleAndTransparentProduceSameResult()
    {
        uint[] simpleSurface = new uint[Vector128<uint>.Count];
        uint[] transparentSurface = new uint[Vector128<uint>.Count];
        Array.Fill(simpleSurface, BGRA.White.Value);
        Array.Fill(transparentSurface, BGRA.White.Value);
        var pixels = Vector128.Create(BGRA.Green.Value, BGRA.Red.Value, BGRA.Blue.Value, BGRA.Yellow.Value);
        var mask = Vector128.Create(uint.MaxValue, 0u, uint.MaxValue, 0u);

        fixed (uint* sPtr = simpleSurface)
            DrawSimplePixel.DrawLine(sPtr, pixels, mask);

        fixed (uint* tPtr = transparentSurface)
            DrawTransparentPixel.DrawLine(tPtr, pixels, mask);

        for (int i = 0; i < Vector128<uint>.Count; i++)
            Assert.Equal(simpleSurface[i], transparentSurface[i]);
    }

    // --- Vector<uint> with mask ---

    [Fact]
    public unsafe void DrawLine_VectorWithMask_OpaquePixels_SimpleAndTransparentProduceSameResult()
    {
        int count = Vector<uint>.Count;
        uint[] simpleSurface = new uint[count];
        uint[] transparentSurface = new uint[count];
        Array.Fill(simpleSurface, BGRA.White.Value);
        Array.Fill(transparentSurface, BGRA.White.Value);
        uint[] maskValues = new uint[count];
        for (int i = 0; i < count; i++)
            maskValues[i] = i % 2 == 0 ? uint.MaxValue : 0u;

        fixed (uint* sPtr = simpleSurface)
            DrawSimplePixel.DrawLine(sPtr, new Vector<uint>(BGRA.Red.Value), new Vector<uint>(maskValues));

        fixed (uint* tPtr = transparentSurface)
            DrawTransparentPixel.DrawLine(tPtr, new Vector<uint>(BGRA.Red.Value), new Vector<uint>(maskValues));

        for (int i = 0; i < count; i++)
            Assert.Equal(simpleSurface[i], transparentSurface[i]);
    }
}
