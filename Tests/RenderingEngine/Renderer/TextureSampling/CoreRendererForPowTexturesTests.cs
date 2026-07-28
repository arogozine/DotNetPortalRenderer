// AI Assisted
using RenderingEngine.Engine;
using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Tooling;

namespace Tests.RenderingEngine.Renderer.TextureSampling;

public unsafe class CoreRendererForPowTextures_DrawSimplePixel_Tests : CoreRendererTestBase
{
    private readonly uint[] _tiledTexture;

    public CoreRendererForPowTextures_DrawSimplePixel_Tests()
    {
        _tiledTexture = new uint[TextureWidth * TextureHeight];
        for (int i = 0; i < _tiledTexture.Length; i++)
            _tiledTexture[i] = (uint)(i + 1);
    }

    [Fact]
    public unsafe void RenderWallColumn_FillsColumnWithTextureColor()
    {
        Span<uint> screen = ClearScreen();

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderWallColumn(
                width: ScreenWidth,
                x: 5,
                textureHeight: TextureHeight,
                startY: 10, endY: 20,
                textureYPos_u: 0,
                textureYIncr_u: 1u << 16,
                screenPtr, texturePtr);

            for (uint y = 10; y < 20; y++)
                Assert.Equal(GreenPixel, screen[(int)(y * ScreenWidth + 5)]);

            AssertOnlyRectRendered(screen, ScreenWidth, 5, 6, 10, 20);
        }
    }

    [Fact]
    public unsafe void RenderWallColumn_NoopWhenStartEqualsEnd()
    {
        Span<uint> screen = ClearScreen();

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderWallColumn(
                width: ScreenWidth,
                x: 0,
                textureHeight: TextureHeight,
                startY: 10, endY: 10,
                textureYPos_u: 0,
                textureYIncr_u: 1u << 16,
                screenPtr, texturePtr);

            AssertScreenEmpty(screen);
        }
    }

    [Fact]
    public unsafe void RenderWallColumn2_FillsColumnAndReturnsAdvancedTexturePosition()
    {
        Span<uint> screen = ClearScreen();

        const uint incr = 1u << 16;
        const uint startY = 5;
        const uint endY = 8;

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        {
            uint finalPos = CoreRendererForPowTextures<DrawSimplePixel>.RenderWallColumn2(
                width: ScreenWidth,
                x: 10,
                textureHeight: TextureHeight,
                startY, endY,
                textureYPos_u: 0,
                textureYIncr_u: incr,
                screenPtr, texturePtr);

            for (uint y = startY; y < endY; y++)
                Assert.Equal(GreenPixel, screen[(int)(y * ScreenWidth + 10)]);

            Assert.Equal((endY - startY) * incr, finalPos);
            AssertOnlyRectRendered(screen, ScreenWidth, 10, 11, (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderMultipleWallLines_FillsColumnsWithTextureColor()
    {
        Span<uint> screen = ClearScreen();

        const uint count = 3;
        const uint x = 20;
        const uint startY = 50;
        const uint endY = 100;

        uint[] startYArr = [startY, startY, startY];
        uint[] endYArr = [endY, endY, endY];
        uint[] textureYPos = new uint[count];
        uint[] textureYIncr = [1u << 16, 1u << 16, 1u << 16];
        uint[] texturePos = new uint[count];

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        fixed (uint* startYPtr = startYArr)
        fixed (uint* endYPtr = endYArr)
        fixed (uint* textureYPosPtr = textureYPos)
        fixed (uint* textureYIncrPtr = textureYIncr)
        fixed (uint* texturePosPtr = texturePos)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLines(
                count, ScreenWidth, x, TextureHeight,
                startYPtr, endYPtr,
                textureYPosPtr, textureYIncrPtr,
                screenPtr, texturePosPtr, texturePtr);

            for (uint col = 0; col < count; col++)
                for (uint y = startY; y < endY; y++)
                    Assert.Equal(GreenPixel, screen[(int)(y * ScreenWidth + x + col)]);

            AssertOnlyRectRendered(screen, ScreenWidth, (int)x, (int)(x + count), (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderMultipleWallLinesV128_FillsColumnsWithTextureColor()
    {
        if (!Vector128.IsHardwareAccelerated)
            return;

        Span<uint> screen = ClearScreen();

        int count = Vector128<uint>.Count;
        const uint x = 100;
        const uint startY = 30;
        const uint endY = 60;

        uint[] startYArr = Enumerable.Repeat(startY, count).ToArray();
        uint[] endYArr = Enumerable.Repeat(endY, count).ToArray();
        uint[] textureYPos = new uint[count];
        uint[] textureYIncr = Enumerable.Repeat(1u << 16, count).ToArray();
        uint[] texturePos = new uint[count];

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        fixed (uint* startYPtr = startYArr)
        fixed (uint* endYPtr = endYArr)
        fixed (uint* textureYPosPtr = textureYPos)
        fixed (uint* textureYIncrPtr = textureYIncr)
        fixed (uint* texturePosPtr = texturePos)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLinesV128<UnalignedMemory>(
                ScreenWidth, x, TextureHeight,
                startYPtr, endYPtr,
                textureYPosPtr, textureYIncrPtr,
                screenPtr, texturePosPtr, texturePtr);

            for (int col = 0; col < count; col++)
                for (uint y = startY; y < endY; y++)
                    Assert.Equal(GreenPixel, screen[(int)(y * ScreenWidth + x + (uint)col)]);

            AssertOnlyRectRendered(screen, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderMultipleWallLinesV256_FillsColumnsWithTextureColor()
    {
        if (!Vector256.IsHardwareAccelerated)
            return;

        Span<uint> screen = ClearScreen();

        int count = Vector256<uint>.Count;
        const uint x = 200;
        const uint startY = 20;
        const uint endY = 80;

        uint[] startYArr = Enumerable.Repeat(startY, count).ToArray();
        uint[] endYArr = Enumerable.Repeat(endY, count).ToArray();
        uint[] textureYPos = new uint[count];
        uint[] textureYIncr = Enumerable.Repeat(1u << 16, count).ToArray();
        uint[] texturePos = new uint[count];

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        fixed (uint* startYPtr = startYArr)
        fixed (uint* endYPtr = endYArr)
        fixed (uint* textureYPosPtr = textureYPos)
        fixed (uint* textureYIncrPtr = textureYIncr)
        fixed (uint* texturePosPtr = texturePos)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLinesV256<UnalignedMemory>(
                ScreenWidth, x, TextureHeight,
                startYPtr, endYPtr,
                textureYPosPtr, textureYIncrPtr,
                screenPtr, texturePosPtr, texturePtr);

            for (int col = 0; col < count; col++)
                for (uint y = startY; y < endY; y++)
                    Assert.Equal(GreenPixel, screen[(int)(y * ScreenWidth + x + (uint)col)]);

            AssertOnlyRectRendered(screen, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderWall_FillsColumnsWithTextureColor()
    {
        Span<uint> screen = ClearScreen();

        const int spriteFromX = 0;
        const int spriteToX = 15;
        const int colCount = spriteToX - spriteFromX + 1; // 16
        const uint startY = 50;
        const uint endY = 100;

        // 4 groups of 4: count=4 < Vector256 width (8) so each group takes the V128 path,
        // guaranteeing all 16 columns are rendered on every machine.
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

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        fixed (ushort* repeatedCountPtr = repeatedCount)
        fixed (uint* fromClampedPtr = fromClamped)
        fixed (uint* toClampedPtr = toClamped)
        fixed (uint* textureXLocPtr = textureXLoc)
        fixed (uint* textureYLocPtr = textureYLoc)
        fixed (uint* textureYIncrPtr = textureYIncr)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderWall(
                spriteFromX, spriteToX,
                width: ScreenWidth, textureHeight: TextureHeight,
                repeatedCountPtr,
                texturePtr, screenPtr,
                fromClampedPtr, toClampedPtr,
                textureXLocPtr, textureYLocPtr, textureYIncrPtr);

            for (int col = 0; col < colCount; col++)
                for (uint y = startY; y < endY; y++)
                    Assert.Equal(GreenPixel, screen[(int)(y * ScreenWidth + col)]);

            AssertOnlyRectRendered(screen, ScreenWidth, spriteFromX, spriteToX + 1, (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderWall_SkipsColumnsWithZeroCount()
    {
        Span<uint> screen = ClearScreen();
        const int colCount = 3;

        AlignedMemoryPool buffers = AlignedMemoryPool.GeneratePool(colCount, RenderWallBucketCount);
        Span<ushort> repeatedCount = buffers.GetBucket<ushort>(RepeatedCountBucket)[..colCount];
        Span<uint> fromClamped = buffers.GetBucket<uint>(FromClampedBucket)[..colCount];
        Span<uint> toClamped = buffers.GetBucket<uint>(ToClampedBucket)[..colCount];
        Span<uint> textureXLoc = buffers.GetBucket<uint>(TextureXLocBucket)[..colCount];
        Span<uint> textureYLoc = buffers.GetBucket<uint>(TextureYLocBucket)[..colCount];
        Span<uint> textureYIncr = buffers.GetBucket<uint>(TextureYIncrBucket)[..colCount];

        repeatedCount.Clear();
        fromClamped.Fill(10);
        toClamped.Fill(20);
        textureXLoc.Clear();
        textureYLoc.Clear();
        textureYIncr.Fill(1u << 16);

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        fixed (ushort* repeatedCountPtr = repeatedCount)
        fixed (uint* fromClampedPtr = fromClamped)
        fixed (uint* toClampedPtr = toClamped)
        fixed (uint* textureXLocPtr = textureXLoc)
        fixed (uint* textureYLocPtr = textureYLoc)
        fixed (uint* textureYIncrPtr = textureYIncr)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderWall(
                spriteFromX: 0, spriteToX: 2,
                width: ScreenWidth, textureHeight: TextureHeight,
                repeatedCountPtr,
                texturePtr, screenPtr,
                fromClampedPtr, toClampedPtr,
                textureXLocPtr, textureYLocPtr, textureYIncrPtr);

            AssertScreenEmpty(screen);
        }
    }

    [Fact]
    public unsafe void RenderWallColumn_TilesY()
    {
        Span<uint> screen = ClearScreen();
        const uint x = 5;
        const uint startY = 100;
        const uint endY = 228; // 128 rows = 2 × TextureHeight

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = _tiledTexture)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderWallColumn(
                width: ScreenWidth, x, textureHeight: TextureHeight,
                startY, endY, textureYPos_u: 0, textureYIncr_u: 1u << 16,
                screenPtr, texturePtr);

            for (uint y = startY; y < endY; y++)
            {
                uint r = y - startY;
                Assert.Equal(_tiledTexture[r & ((uint)TextureHeight - 1)], screen[(int)(y * ScreenWidth + x)]);
            }

            AssertOnlyRectRendered(screen, ScreenWidth, (int)x, (int)x + 1, (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderWallColumn2_TilesY()
    {
        Span<uint> screen = ClearScreen();
        const uint x = 10;
        const uint startY = 100;
        const uint endY = 228; // 128 rows = 2 × TextureHeight
        const uint incr = 1u << 16;

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = _tiledTexture)
        {
            uint finalPos = CoreRendererForPowTextures<DrawSimplePixel>.RenderWallColumn2(
                width: ScreenWidth, x, textureHeight: TextureHeight,
                startY, endY, textureYPos_u: 0, textureYIncr_u: incr,
                screenPtr, texturePtr);

            for (uint y = startY; y < endY; y++)
            {
                uint r = y - startY;
                Assert.Equal(_tiledTexture[r & ((uint)TextureHeight - 1)], screen[(int)(y * ScreenWidth + x)]);
            }

            Assert.Equal((endY - startY) * incr, finalPos);
            AssertOnlyRectRendered(screen, ScreenWidth, (int)x, (int)x + 1, (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderMultipleWallLines_TilesY()
    {
        Span<uint> screen = ClearScreen();
        const uint count = 3;
        const uint x = 20;
        const uint startY = 100;
        const uint endY = 228; // 128 rows = 2 × TextureHeight

        uint[] startYArr = [startY, startY, startY];
        uint[] endYArr = [endY, endY, endY];
        uint[] textureYPos = new uint[count];
        uint[] textureYIncr = [1u << 16, 1u << 16, 1u << 16];
        uint[] texturePos = new uint[count];

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = _tiledTexture)
        fixed (uint* startYPtr = startYArr)
        fixed (uint* endYPtr = endYArr)
        fixed (uint* textureYPosPtr = textureYPos)
        fixed (uint* textureYIncrPtr = textureYIncr)
        fixed (uint* texturePosPtr = texturePos)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLines(
                count, ScreenWidth, x, TextureHeight,
                startYPtr, endYPtr,
                textureYPosPtr, textureYIncrPtr,
                screenPtr, texturePosPtr, texturePtr);

            for (uint col = 0; col < count; col++)
                for (uint y = startY; y < endY; y++)
                {
                    uint r = y - startY;
                    Assert.Equal(_tiledTexture[r & ((uint)TextureHeight - 1)], screen[(int)(y * ScreenWidth + x + col)]);
                }

            AssertOnlyRectRendered(screen, ScreenWidth, (int)x, (int)(x + count), (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderMultipleWallLinesV128_TilesY()
    {
        if (!Vector128.IsHardwareAccelerated)
            return;

        Span<uint> screen = ClearScreen();
        int count = Vector128<uint>.Count;
        const uint x = 100;
        const uint startY = 100;
        const uint endY = 228; // 128 rows = 2 × TextureHeight

        uint[] startYArr = Enumerable.Repeat(startY, count).ToArray();
        uint[] endYArr = Enumerable.Repeat(endY, count).ToArray();
        uint[] textureYPos = new uint[count];
        uint[] textureYIncr = Enumerable.Repeat(1u << 16, count).ToArray();
        uint[] texturePos = new uint[count];

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = _tiledTexture)
        fixed (uint* startYPtr = startYArr)
        fixed (uint* endYPtr = endYArr)
        fixed (uint* textureYPosPtr = textureYPos)
        fixed (uint* textureYIncrPtr = textureYIncr)
        fixed (uint* texturePosPtr = texturePos)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLinesV128<UnalignedMemory>(
                ScreenWidth, x, TextureHeight,
                startYPtr, endYPtr,
                textureYPosPtr, textureYIncrPtr,
                screenPtr, texturePosPtr, texturePtr);

            for (int col = 0; col < count; col++)
                for (uint y = startY; y < endY; y++)
                {
                    uint r = y - startY;
                    Assert.Equal(_tiledTexture[r & ((uint)TextureHeight - 1)], screen[(int)(y * ScreenWidth + x + (uint)col)]);
                }

            AssertOnlyRectRendered(screen, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderMultipleWallLinesV256_TilesY()
    {
        if (!Vector256.IsHardwareAccelerated)
            return;

        Span<uint> screen = ClearScreen();
        int count = Vector256<uint>.Count;
        const uint x = 200;
        const uint startY = 100;
        const uint endY = 228; // 128 rows = 2 × TextureHeight

        uint[] startYArr = Enumerable.Repeat(startY, count).ToArray();
        uint[] endYArr = Enumerable.Repeat(endY, count).ToArray();
        uint[] textureYPos = new uint[count];
        uint[] textureYIncr = Enumerable.Repeat(1u << 16, count).ToArray();
        uint[] texturePos = new uint[count];

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = _tiledTexture)
        fixed (uint* startYPtr = startYArr)
        fixed (uint* endYPtr = endYArr)
        fixed (uint* textureYPosPtr = textureYPos)
        fixed (uint* textureYIncrPtr = textureYIncr)
        fixed (uint* texturePosPtr = texturePos)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderMultipleWallLinesV256<UnalignedMemory>(
                ScreenWidth, x, TextureHeight,
                startYPtr, endYPtr,
                textureYPosPtr, textureYIncrPtr,
                screenPtr, texturePosPtr, texturePtr);

            for (int col = 0; col < count; col++)
                for (uint y = startY; y < endY; y++)
                {
                    uint r = y - startY;
                    Assert.Equal(_tiledTexture[r & ((uint)TextureHeight - 1)], screen[(int)(y * ScreenWidth + x + (uint)col)]);
                }

            AssertOnlyRectRendered(screen, ScreenWidth, (int)x, (int)x + count, (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderWall_TilesY()
    {
        Span<uint> screen = ClearScreen();
        const int spriteFromX = 0;
        const int spriteToX = 15;
        const int colCount = spriteToX - spriteFromX + 1; // 16
        const uint startY = 100;
        const uint endY = 228; // 128 rows = 2 × TextureHeight

        // 4 groups of 4 → V128 path, which does not write back textureYLoc.
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

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = _tiledTexture)
        fixed (ushort* repeatedCountPtr = repeatedCount)
        fixed (uint* fromClampedPtr = fromClamped)
        fixed (uint* toClampedPtr = toClamped)
        fixed (uint* textureXLocPtr = textureXLoc)
        fixed (uint* textureYLocPtr = textureYLoc)
        fixed (uint* textureYIncrPtr = textureYIncr)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderWall(
                spriteFromX, spriteToX,
                width: ScreenWidth, textureHeight: TextureHeight,
                repeatedCountPtr,
                texturePtr, screenPtr,
                fromClampedPtr, toClampedPtr,
                textureXLocPtr, textureYLocPtr, textureYIncrPtr);

            for (int col = 0; col < colCount; col++)
                for (uint y = startY; y < endY; y++)
                {
                    uint r = y - startY;
                    Assert.Equal(_tiledTexture[r & ((uint)TextureHeight - 1)], screen[(int)(y * ScreenWidth + col)]);
                }

            AssertOnlyRectRendered(screen, ScreenWidth, spriteFromX, spriteToX + 1, (int)startY, (int)endY);
        }
    }

    [Fact]
    public unsafe void RenderSkybox_StaggeredLanes_RendersTopSharedAndBottomRegionsPerLane()
    {
        // AI Assisted
        // Exercises the AVX2 masked-gather path in RenderColumnAngleTop/Bottom by staggering
        // wall bounds per lane so min_t != max_t and min_b != max_b within the vector group.
        if (!Avx2.IsSupported || Vector<int>.Count != Vector256<int>.Count)
            return;

        const int laneCount = 8; // Vector256<int>.Count
        const int minTop = 50;
        const int maxTop = minTop + (laneCount - 1) * 10; // 120
        const int minBottom = 250 - (laneCount - 1) * 10; // 180
        const int maxBottom = 250;

        Span<uint> screen = ClearScreen();

        int[] fromY = new int[laneCount];
        int[] toY = new int[laneCount];
        int[] ceilingStart = new int[laneCount];
        int[] floorEnd = new int[laneCount];
        float[] angleCache = new float[laneCount];
        ushort[] repeatedCount = new ushort[laneCount];
        // RenderSkybox never masks the row index against textureHeight, so the backing buffer
        // must cover every row the wall bounds can reach (up to maxBottom), not just TextureHeight.
        const int textureRows = maxBottom + 1;
        uint[] texture = new uint[TextureWidth * textureRows];

        for (int i = 0; i < laneCount; i++)
        {
            fromY[i] = minTop + i * 10;
            toY[i] = maxBottom - i * 10;
            ceilingStart[i] = 0;
            floorEnd[i] = 300;
            angleCache[i] = 0f;
        }

        repeatedCount[0] = laneCount;

        for (int row = 0; row < textureRows; row++)
            texture[row * TextureWidth] = (uint)(row + 1);

        PortalPlayerSnapshot player = new() { Angle = 0f };

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = texture)
        fixed (int* fromYPtr = fromY)
        fixed (int* toYPtr = toY)
        fixed (int* ceilingStartPtr = ceilingStart)
        fixed (int* floorEndPtr = floorEnd)
        fixed (float* angleCachePtr = angleCache)
        fixed (ushort* repeatedCountPtr = repeatedCount)
        {
            CoreRendererForPowTextures<DrawSimplePixel>.RenderSkybox(
                player,
                repeatCount: 1,
                repeatedCountPtr,
                angleCachePtr,
                screenPtr,
                texturePtr,
                sectorFromX: 0, sectorToX: laneCount,
                fromYPtr, toYPtr,
                ceilingStartPtr, floorEndPtr,
                width: ScreenWidth,
                textureWidth: TextureWidth,
                textureHeight: textureRows,
                yTextureIncr: 1f);
        }

        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int lane = 0; lane < laneCount; lane++)
            {
                bool expectedWrite;
                if (y < minTop || y >= maxBottom)
                    expectedWrite = false;
                else if (y >= maxTop && y <= minBottom)
                    expectedWrite = true; // shared window: always written
                else if (y < maxTop)
                    expectedWrite = fromY[lane] < y; // top region
                else
                    expectedWrite = toY[lane] > y; // bottom region

                uint actual = screen[y * ScreenWidth + lane];
                uint expected = expectedWrite ? (uint)(y + 1) : 0u;

                Assert.True(expected == actual,
                    $"Mismatch at y={y}, lane={lane}: expected {expected}, got {actual}");
            }
        }

        AssertOnlyRectRendered(screen, ScreenWidth, 0, laneCount, minTop, maxBottom);
    }
}
