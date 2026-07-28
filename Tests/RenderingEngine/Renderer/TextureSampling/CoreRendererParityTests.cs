// AI Assisted
// Verifies CoreRendererForPowTextures and CoreRendererForOddTextures produce identical output
// for a 64x64 texture (power-of-2 height), since (y >> 16) & 63 == (y >> 16) % 64.
using RenderingEngine.Engine;
using RenderingEngine.Tooling;
using System.Runtime.Intrinsics;
using Tooling;

namespace Tests.RenderingEngine.Renderer.TextureSampling;

public unsafe class CoreRendererParity_DrawSimplePixel_Tests : CoreRendererTestBase
{
    private const int UniqueTextureSize = 64; // power-of-2; width and height are equal

    // Column-major layout: index = col * UniqueTextureSize + row. Values are 1-based to distinguish from cleared (0) screen.
    private readonly uint[] _uniqueTexture;

    public CoreRendererParity_DrawSimplePixel_Tests()
    {
        _uniqueTexture = new uint[UniqueTextureSize * UniqueTextureSize];
        for (int i = 0; i < _uniqueTexture.Length; i++)
            _uniqueTexture[i] = (uint)(i + 1);
    }

    [Fact]
    public unsafe void RenderWallColumn_PowAndOddProduceSameOutput()
    {
        uint[] screenPow = new uint[ScreenWidth * ScreenHeight];
        uint[] screenOdd = new uint[ScreenWidth * ScreenHeight];

        const uint x = 5;
        const uint startY = 10;
        const uint endY = 74; // 64 rows = one full texture column

        fixed (uint* texturePtr = _uniqueTexture)
        {
            fixed (uint* screenPtr = screenPow)
                CoreRendererForPowTextures<DrawSimplePixel>.RenderWallColumn(
                    ScreenWidth, x, UniqueTextureSize, startY, endY, 0, 1u << 16, screenPtr, texturePtr);

            fixed (uint* screenPtr = screenOdd)
                CoreRendererForOddTextures<DrawSimplePixel>.RenderWallColumn(
                    ScreenWidth, x, UniqueTextureSize, startY, endY, 0, 1u << 16, screenPtr, texturePtr);
        }

        for (uint y = startY; y < endY; y++)
        {
            int idx = (int)(y * ScreenWidth + x);
            Assert.Equal(screenPow[idx], screenOdd[idx]);
            Assert.NotEqual(0u, screenPow[idx]);
        }

        AssertOnlyRectRendered(screenPow, ScreenWidth, (int)x, (int)x + 1, (int)startY, (int)endY);
        AssertOnlyRectRendered(screenOdd, ScreenWidth, (int)x, (int)x + 1, (int)startY, (int)endY);
    }

    [Fact]
    public unsafe void RenderWallColumn2_PowAndOddProduceSameOutput()
    {
        uint[] screenPow = new uint[ScreenWidth * ScreenHeight];
        uint[] screenOdd = new uint[ScreenWidth * ScreenHeight];

        const uint x = 7;
        const uint startY = 5;
        const uint endY = 69; // 64 rows
        const uint incr = 1u << 16;

        fixed (uint* texturePtr = _uniqueTexture)
        {
            fixed (uint* screenPtr = screenPow)
                CoreRendererForPowTextures<DrawSimplePixel>.RenderWallColumn2(
                    ScreenWidth, x, UniqueTextureSize, startY, endY, 0, incr, screenPtr, texturePtr);

            fixed (uint* screenPtr = screenOdd)
                CoreRendererForOddTextures<DrawSimplePixel>.RenderWallColumn2(
                    ScreenWidth, x, UniqueTextureSize, startY, endY, 0, incr, screenPtr, texturePtr);
        }

        for (uint y = startY; y < endY; y++)
        {
            int idx = (int)(y * ScreenWidth + x);
            Assert.Equal(screenPow[idx], screenOdd[idx]);
            Assert.NotEqual(0u, screenPow[idx]);
        }

        AssertOnlyRectRendered(screenPow, ScreenWidth, (int)x, (int)x + 1, (int)startY, (int)endY);
        AssertOnlyRectRendered(screenOdd, ScreenWidth, (int)x, (int)x + 1, (int)startY, (int)endY);
    }

    [Fact]
    public unsafe void RenderWall_PowAndOddProduceSameOutput()
    {
        uint[] screenPow = new uint[ScreenWidth * ScreenHeight];
        uint[] screenOdd = new uint[ScreenWidth * ScreenHeight];

        const int spriteFromX = 0;
        const int spriteToX = 2;
        const int colCount = spriteToX - spriteFromX + 1;
        const uint startY = 50;
        const uint endY = 114; // 64 rows

        AlignedMemoryPool buffers = AlignedMemoryPool.GeneratePool(colCount, RenderWallBucketCount);
        Span<ushort> repeatedCount = buffers.GetBucket<ushort>(RepeatedCountBucket)[..colCount];
        Span<uint> fromClamped = buffers.GetBucket<uint>(FromClampedBucket)[..colCount];
        Span<uint> toClamped = buffers.GetBucket<uint>(ToClampedBucket)[..colCount];
        Span<uint> textureXLoc = buffers.GetBucket<uint>(TextureXLocBucket)[..colCount]; // column 0 for all
        Span<uint> textureYLoc = buffers.GetBucket<uint>(TextureYLocBucket)[..colCount];
        Span<uint> textureYIncr = buffers.GetBucket<uint>(TextureYIncrBucket)[..colCount];

        repeatedCount.Clear();
        repeatedCount[0] = colCount;
        fromClamped.Fill(startY);
        toClamped.Fill(endY);
        textureXLoc.Clear();
        textureYLoc.Clear();
        textureYIncr.Fill(1u << 16);

        fixed (uint* texturePtr = _uniqueTexture)
        fixed (ushort* repeatedCountPtr = repeatedCount)
        fixed (uint* fromClampedPtr = fromClamped)
        fixed (uint* toClampedPtr = toClamped)
        fixed (uint* textureXLocPtr = textureXLoc)
        fixed (uint* textureYLocPtr = textureYLoc)
        fixed (uint* textureYIncrPtr = textureYIncr)
        {
            fixed (uint* screenPtr = screenPow)
                CoreRendererForPowTextures<DrawSimplePixel>.RenderWall(
                    spriteFromX, spriteToX,
                    ScreenWidth, UniqueTextureSize,
                    repeatedCountPtr, texturePtr, screenPtr,
                    fromClampedPtr, toClampedPtr,
                    textureXLocPtr, textureYLocPtr, textureYIncrPtr);

            fixed (uint* screenPtr = screenOdd)
                CoreRendererForOddTextures<DrawSimplePixel>.RenderWall(
                    spriteFromX, spriteToX,
                    ScreenWidth, UniqueTextureSize,
                    repeatedCountPtr, texturePtr, screenPtr,
                    fromClampedPtr, toClampedPtr,
                    textureXLocPtr, textureYLocPtr, textureYIncrPtr);
        }

        for (int col = 0; col < colCount; col++)
            for (uint y = startY; y < endY; y++)
            {
                int idx = (int)(y * ScreenWidth + col);
                Assert.Equal(screenPow[idx], screenOdd[idx]);
                Assert.NotEqual(0u, screenPow[idx]);
            }

        AssertOnlyRectRendered(screenPow, ScreenWidth, spriteFromX, spriteToX + 1, (int)startY, (int)endY);
        AssertOnlyRectRendered(screenOdd, ScreenWidth, spriteFromX, spriteToX + 1, (int)startY, (int)endY);
    }

    [Fact]
    public unsafe void RenderMultipleWallLinesV128_PowAndOddProduceSameOutput()
    {
        if (!Vector128.IsHardwareAccelerated)
            return;

        uint[] screenPow = new uint[ScreenWidth * ScreenHeight];
        uint[] screenOdd = new uint[ScreenWidth * ScreenHeight];

        int count = Vector128<uint>.Count;
        const uint x = 100;
        const uint startY = 30;
        const uint endY = 94; // 64 rows

        uint[] startYArr = Enumerable.Repeat(startY, count).ToArray();
        uint[] endYArr = Enumerable.Repeat(endY, count).ToArray();
        uint[] textureYPos = new uint[count];
        uint[] textureYIncr = Enumerable.Repeat(1u << 16, count).ToArray();
        uint[] texturePos = new uint[count]; // column 0 for all

        fixed (uint* texturePtr = _uniqueTexture)
        fixed (uint* startYPtr = startYArr)
        fixed (uint* endYPtr = endYArr)
        fixed (uint* textureYPosPtr = textureYPos)
        fixed (uint* textureYIncrPtr = textureYIncr)
        fixed (uint* texturePosPtr = texturePos)
        {
            fixed (uint* screenPtr = screenPow)
                CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLinesV128<UnalignedMemory>(
                    ScreenWidth, x, UniqueTextureSize,
                    startYPtr, endYPtr,
                    textureYPosPtr, textureYIncrPtr,
                    screenPtr, texturePosPtr, texturePtr);

            fixed (uint* screenPtr = screenOdd)
                CoreRendererForOddTextures<DrawSimplePixel>.RenderMultipleWallLinesV128<UnalignedMemory>(
                    ScreenWidth, x, UniqueTextureSize,
                    startYPtr, endYPtr,
                    textureYPosPtr, textureYIncrPtr,
                    screenPtr, texturePosPtr, texturePtr);
        }

        for (int col = 0; col < count; col++)
            for (uint y = startY; y < endY; y++)
            {
                int idx = (int)(y * ScreenWidth + x + (uint)col);
                Assert.Equal(screenPow[idx], screenOdd[idx]);
                Assert.NotEqual(0u, screenPow[idx]);
            }

        AssertOnlyRectRendered(screenPow, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
        AssertOnlyRectRendered(screenOdd, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
    }

    [Fact]
    public unsafe void RenderMultipleWallLinesV256_PowAndOddProduceSameOutput()
    {
        if (!Vector256.IsHardwareAccelerated)
            return;

        uint[] screenPow = new uint[ScreenWidth * ScreenHeight];
        uint[] screenOdd = new uint[ScreenWidth * ScreenHeight];

        int count = Vector256<uint>.Count;
        const uint x = 200;
        const uint startY = 20;
        const uint endY = 84; // 64 rows

        uint[] startYArr = Enumerable.Repeat(startY, count).ToArray();
        uint[] endYArr = Enumerable.Repeat(endY, count).ToArray();
        uint[] textureYPos = new uint[count];
        uint[] textureYIncr = Enumerable.Repeat(1u << 16, count).ToArray();
        uint[] texturePos = new uint[count]; // column 0 for all

        fixed (uint* texturePtr = _uniqueTexture)
        fixed (uint* startYPtr = startYArr)
        fixed (uint* endYPtr = endYArr)
        fixed (uint* textureYPosPtr = textureYPos)
        fixed (uint* textureYIncrPtr = textureYIncr)
        fixed (uint* texturePosPtr = texturePos)
        {
            fixed (uint* screenPtr = screenPow)
                CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLinesV256<UnalignedMemory>(
                    ScreenWidth, x, UniqueTextureSize,
                    startYPtr, endYPtr,
                    textureYPosPtr, textureYIncrPtr,
                    screenPtr, texturePosPtr, texturePtr);

            fixed (uint* screenPtr = screenOdd)
                CoreRendererForOddTextures<DrawSimplePixel>.RenderMultipleWallLinesV256<UnalignedMemory>(
                    ScreenWidth, x, UniqueTextureSize,
                    startYPtr, endYPtr,
                    textureYPosPtr, textureYIncrPtr,
                    screenPtr, texturePosPtr, texturePtr);
        }

        for (int col = 0; col < count; col++)
            for (uint y = startY; y < endY; y++)
            {
                int idx = (int)(y * ScreenWidth + x + (uint)col);
                Assert.Equal(screenPow[idx], screenOdd[idx]);
                Assert.NotEqual(0u, screenPow[idx]);
            }

        AssertOnlyRectRendered(screenPow, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
        AssertOnlyRectRendered(screenOdd, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
    }
}

// Verifies CoreRendererForUntiledTextures produces the same output as CoreRendererForPowTextures
// when the texture Y index never reaches textureHeight (no wrapping needed).
public unsafe class CoreRendererParityUntiled_DrawSimplePixel_Tests : CoreRendererTestBase
{
    private const int UniqueTextureSize = 64;

    private readonly uint[] _uniqueTexture;

    public CoreRendererParityUntiled_DrawSimplePixel_Tests()
    {
        _uniqueTexture = new uint[UniqueTextureSize * UniqueTextureSize];
        for (int i = 0; i < _uniqueTexture.Length; i++)
            _uniqueTexture[i] = (uint)(i + 1);
    }

    [Fact]
    public unsafe void RenderWallColumn_UntiledAndPowProduceSameOutput()
    {
        uint[] screenPow = new uint[ScreenWidth * ScreenHeight];
        uint[] screenUntiled = new uint[ScreenWidth * ScreenHeight];

        const uint x = 5;
        const uint startY = 10;
        const uint endY = 60; // 50 rows, no y tiling (50 < UniqueTextureSize)

        fixed (uint* texturePtr = _uniqueTexture)
        {
            fixed (uint* screenPtr = screenPow)
                CoreRendererForPowTextures<DrawSimplePixel>.RenderWallColumn(
                    ScreenWidth, x, UniqueTextureSize, startY, endY, 0, 1u << 16, screenPtr, texturePtr);

            fixed (uint* screenPtr = screenUntiled)
                CoreRendererForUntiledTextures<DrawSimplePixel>.RenderWallColumn(
                    ScreenWidth, x, UniqueTextureSize, startY, endY, 0, 1u << 16, screenPtr, texturePtr);
        }

        for (uint y = startY; y < endY; y++)
        {
            int idx = (int)(y * ScreenWidth + x);
            Assert.Equal(screenPow[idx], screenUntiled[idx]);
            Assert.NotEqual(0u, screenPow[idx]);
        }

        AssertOnlyRectRendered(screenPow, ScreenWidth, (int)x, (int)x + 1, (int)startY, (int)endY);
        AssertOnlyRectRendered(screenUntiled, ScreenWidth, (int)x, (int)x + 1, (int)startY, (int)endY);
    }

    [Fact]
    public unsafe void RenderWallColumn2_UntiledAndPowProduceSameOutput()
    {
        uint[] screenPow = new uint[ScreenWidth * ScreenHeight];
        uint[] screenUntiled = new uint[ScreenWidth * ScreenHeight];

        const uint x = 7;
        const uint startY = 5;
        const uint endY = 55; // 50 rows, no y tiling
        const uint incr = 1u << 16;

        fixed (uint* texturePtr = _uniqueTexture)
        {
            fixed (uint* screenPtr = screenPow)
                CoreRendererForPowTextures<DrawSimplePixel>.RenderWallColumn2(
                    ScreenWidth, x, UniqueTextureSize, startY, endY, 0, incr, screenPtr, texturePtr);

            fixed (uint* screenPtr = screenUntiled)
                CoreRendererForUntiledTextures<DrawSimplePixel>.RenderWallColumn2(
                    ScreenWidth, x, UniqueTextureSize, startY, endY, 0, incr, screenPtr, texturePtr);
        }

        for (uint y = startY; y < endY; y++)
        {
            int idx = (int)(y * ScreenWidth + x);
            Assert.Equal(screenPow[idx], screenUntiled[idx]);
            Assert.NotEqual(0u, screenPow[idx]);
        }

        AssertOnlyRectRendered(screenPow, ScreenWidth, (int)x, (int)x + 1, (int)startY, (int)endY);
        AssertOnlyRectRendered(screenUntiled, ScreenWidth, (int)x, (int)x + 1, (int)startY, (int)endY);
    }

    [Fact]
    public unsafe void RenderWall_UntiledAndPowProduceSameOutput()
    {
        uint[] screenPow = new uint[ScreenWidth * ScreenHeight];
        uint[] screenUntiled = new uint[ScreenWidth * ScreenHeight];

        const int spriteFromX = 0;
        const int spriteToX = 15;
        const int colCount = spriteToX - spriteFromX + 1; // 16
        const uint startY = 50;
        const uint endY = 100; // 50 rows, no y tiling

        // 4 groups of 4 → V128 path, which does not write back textureYLoc, so the array is safe to share.
        AlignedMemoryPool buffers = AlignedMemoryPool.GeneratePool(colCount, RenderWallBucketCount);
        Span<ushort> repeatedCount = buffers.GetBucket<ushort>(RepeatedCountBucket)[..colCount];
        Span<uint> fromClamped = buffers.GetBucket<uint>(FromClampedBucket)[..colCount];
        Span<uint> toClamped = buffers.GetBucket<uint>(ToClampedBucket)[..colCount];
        Span<uint> textureXLoc = buffers.GetBucket<uint>(TextureXLocBucket)[..colCount];
        Span<uint> textureYLoc = buffers.GetBucket<uint>(TextureYLocBucket)[..colCount];
        Span<uint> textureYIncr = buffers.GetBucket<uint>(TextureYIncrBucket)[..colCount];

        repeatedCount.Clear();
        repeatedCount[0] = 4; repeatedCount[4] = 4; repeatedCount[8] = 4; repeatedCount[12] = 4;
        fromClamped.Fill(startY);
        toClamped.Fill(endY);
        textureXLoc.Clear();
        textureYLoc.Clear();
        textureYIncr.Fill(1u << 16);

        fixed (uint* texturePtr = _uniqueTexture)
        fixed (ushort* repeatedCountPtr = repeatedCount)
        fixed (uint* fromClampedPtr = fromClamped)
        fixed (uint* toClampedPtr = toClamped)
        fixed (uint* textureXLocPtr = textureXLoc)
        fixed (uint* textureYLocPtr = textureYLoc)
        fixed (uint* textureYIncrPtr = textureYIncr)
        {
            fixed (uint* screenPtr = screenPow)
                CoreRendererForPowTextures<DrawSimplePixel>.RenderWall(
                    spriteFromX, spriteToX,
                    ScreenWidth, UniqueTextureSize,
                    repeatedCountPtr, texturePtr, screenPtr,
                    fromClampedPtr, toClampedPtr,
                    textureXLocPtr, textureYLocPtr, textureYIncrPtr);

            fixed (uint* screenPtr = screenUntiled)
                CoreRendererForUntiledTextures<DrawSimplePixel>.RenderWall(
                    spriteFromX, spriteToX,
                    ScreenWidth, UniqueTextureSize,
                    repeatedCountPtr, texturePtr, screenPtr,
                    fromClampedPtr, toClampedPtr,
                    textureXLocPtr, textureYLocPtr, textureYIncrPtr);
        }

        for (int col = 0; col < colCount; col++)
            for (uint y = startY; y < endY; y++)
            {
                int idx = (int)(y * ScreenWidth + col);
                Assert.Equal(screenPow[idx], screenUntiled[idx]);
                Assert.NotEqual(0u, screenPow[idx]);
            }

        AssertOnlyRectRendered(screenPow, ScreenWidth, spriteFromX, spriteToX + 1, (int)startY, (int)endY);
        AssertOnlyRectRendered(screenUntiled, ScreenWidth, spriteFromX, spriteToX + 1, (int)startY, (int)endY);
    }

    [Fact]
    public unsafe void RenderMultipleWallLinesV128_UntiledAndPowProduceSameOutput()
    {
        if (!Vector128.IsHardwareAccelerated)
            return;

        uint[] screenPow = new uint[ScreenWidth * ScreenHeight];
        uint[] screenUntiled = new uint[ScreenWidth * ScreenHeight];

        int count = Vector128<uint>.Count;
        const uint x = 100;
        const uint startY = 30;
        const uint endY = 60; // 30 rows, no y tiling

        uint[] startYArr = Enumerable.Repeat(startY, count).ToArray();
        uint[] endYArr = Enumerable.Repeat(endY, count).ToArray();
        uint[] textureYPos = new uint[count];
        uint[] textureYIncr = Enumerable.Repeat(1u << 16, count).ToArray();
        uint[] texturePos = new uint[count];

        fixed (uint* texturePtr = _uniqueTexture)
        fixed (uint* startYPtr = startYArr)
        fixed (uint* endYPtr = endYArr)
        fixed (uint* textureYPosPtr = textureYPos)
        fixed (uint* textureYIncrPtr = textureYIncr)
        fixed (uint* texturePosPtr = texturePos)
        {
            fixed (uint* screenPtr = screenPow)
                CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLinesV128<UnalignedMemory>(
                    ScreenWidth, x, UniqueTextureSize,
                    startYPtr, endYPtr,
                    textureYPosPtr, textureYIncrPtr,
                    screenPtr, texturePosPtr, texturePtr);

            fixed (uint* screenPtr = screenUntiled)
                CoreRendererForUntiledTextures<DrawSimplePixel>.RenderMultipleWallLinesV128<UnalignedMemory>(
                    ScreenWidth, x, UniqueTextureSize,
                    startYPtr, endYPtr,
                    textureYPosPtr, textureYIncrPtr,
                    screenPtr, texturePosPtr, texturePtr);
        }

        for (int col = 0; col < count; col++)
            for (uint y = startY; y < endY; y++)
            {
                int idx = (int)(y * ScreenWidth + x + (uint)col);
                Assert.Equal(screenPow[idx], screenUntiled[idx]);
                Assert.NotEqual(0u, screenPow[idx]);
            }

        AssertOnlyRectRendered(screenPow, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
        AssertOnlyRectRendered(screenUntiled, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
    }

    [Fact]
    public unsafe void RenderMultipleWallLinesV256_UntiledAndPowProduceSameOutput()
    {
        if (!Vector256.IsHardwareAccelerated)
            return;

        uint[] screenPow = new uint[ScreenWidth * ScreenHeight];
        uint[] screenUntiled = new uint[ScreenWidth * ScreenHeight];

        int count = Vector256<uint>.Count;
        const uint x = 200;
        const uint startY = 20;
        const uint endY = 60; // 40 rows, no y tiling

        uint[] startYArr = Enumerable.Repeat(startY, count).ToArray();
        uint[] endYArr = Enumerable.Repeat(endY, count).ToArray();
        uint[] textureYPos = new uint[count];
        uint[] textureYIncr = Enumerable.Repeat(1u << 16, count).ToArray();
        uint[] texturePos = new uint[count];

        fixed (uint* texturePtr = _uniqueTexture)
        fixed (uint* startYPtr = startYArr)
        fixed (uint* endYPtr = endYArr)
        fixed (uint* textureYPosPtr = textureYPos)
        fixed (uint* textureYIncrPtr = textureYIncr)
        fixed (uint* texturePosPtr = texturePos)
        {
            fixed (uint* screenPtr = screenPow)
                CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLinesV256<UnalignedMemory>(
                    ScreenWidth, x, UniqueTextureSize,
                    startYPtr, endYPtr,
                    textureYPosPtr, textureYIncrPtr,
                    screenPtr, texturePosPtr, texturePtr);

            fixed (uint* screenPtr = screenUntiled)
                CoreRendererForUntiledTextures<DrawSimplePixel>.RenderMultipleWallLinesV256<UnalignedMemory>(
                    ScreenWidth, x, UniqueTextureSize,
                    startYPtr, endYPtr,
                    textureYPosPtr, textureYIncrPtr,
                    screenPtr, texturePosPtr, texturePtr);
        }

        for (int col = 0; col < count; col++)
            for (uint y = startY; y < endY; y++)
            {
                int idx = (int)(y * ScreenWidth + x + (uint)col);
                Assert.Equal(screenPow[idx], screenUntiled[idx]);
                Assert.NotEqual(0u, screenPow[idx]);
            }

        AssertOnlyRectRendered(screenPow, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
        AssertOnlyRectRendered(screenUntiled, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
    }
}
