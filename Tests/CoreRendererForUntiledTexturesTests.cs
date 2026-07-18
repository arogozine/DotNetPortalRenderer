// AI Assisted
using RenderingEngine.Engine;
using System.Runtime.Intrinsics;

namespace Tests;

public class CoreRendererForUntiledTextures_DrawSimplePixel_Tests : CoreRendererTestBase
{
    [Fact]
    public unsafe void RenderWallColumn_FillsColumnWithTextureColor()
    {
        Span<uint> screen = ClearScreen();

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        {
            CoreRendererForUntiledTextures<DrawSimplePixel>.RenderWallColumn(
                width: ScreenWidth, x: 5, textureHeight: TextureHeight,
                startY: 10, endY: 20, textureYPos_u: 0, textureYIncr_u: 1u << 16,
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
            CoreRendererForUntiledTextures<DrawSimplePixel>.RenderWallColumn(
                width: ScreenWidth, x: 0, textureHeight: TextureHeight,
                startY: 10, endY: 10, textureYPos_u: 0, textureYIncr_u: 1u << 16,
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
            uint finalPos = CoreRendererForUntiledTextures<DrawSimplePixel>.RenderWallColumn2(
                width: ScreenWidth, x: 10, textureHeight: TextureHeight,
                startY, endY, textureYPos_u: 0, textureYIncr_u: incr,
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
        const uint endY = 100; // 50 rows, stays within TextureHeight (64)

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
            CoreRendererForUntiledTextures<DrawSimplePixel>.RenderMultipleWallLines(
                count, ScreenWidth, x, textureHeight: TextureHeight,
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
        const uint endY = 60; // 30 rows, stays within TextureHeight (64)

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
            CoreRendererForUntiledTextures<DrawSimplePixel>.RenderMultipleWallLinesV128(
                ScreenWidth, x, textureHeight: TextureHeight,
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
        const uint endY = 60; // 40 rows, stays within TextureHeight (64)

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
            CoreRendererForUntiledTextures<DrawSimplePixel>.RenderMultipleWallLinesV256(
                ScreenWidth, x, textureHeight: TextureHeight,
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
        const uint endY = 100; // 50 rows, stays within TextureHeight (64)

        // 4 groups of 4: count=4 < Vector256 width (8) so each group takes the V128 path,
        // guaranteeing all 16 columns are rendered on every machine.
        ushort[] repeatedCount = new ushort[colCount];
        repeatedCount[0] = 4; repeatedCount[4] = 4; repeatedCount[8] = 4; repeatedCount[12] = 4;
        uint[] fromClamped = Enumerable.Repeat(startY, colCount).ToArray();
        uint[] toClamped = Enumerable.Repeat(endY, colCount).ToArray();
        uint[] textureXLoc = new uint[colCount];
        uint[] textureYLoc = new uint[colCount];
        uint[] textureYIncr = Enumerable.Repeat(1u << 16, colCount).ToArray();

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        fixed (ushort* repeatedCountPtr = repeatedCount)
        fixed (uint* fromClampedPtr = fromClamped)
        fixed (uint* toClampedPtr = toClamped)
        fixed (uint* textureXLocPtr = textureXLoc)
        fixed (uint* textureYLocPtr = textureYLoc)
        fixed (uint* textureYIncrPtr = textureYIncr)
        {
            CoreRendererForUntiledTextures<DrawSimplePixel>.RenderWall(
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

        ushort[] repeatedCount = [0, 0, 0];
        uint[] fromClamped = [10, 10, 10];
        uint[] toClamped = [20, 20, 20];
        uint[] textureXLoc = new uint[3];
        uint[] textureYLoc = new uint[3];
        uint[] textureYIncr = [1u << 16, 1u << 16, 1u << 16];

        fixed (uint* screenPtr = screen)
        fixed (uint* texturePtr = Texture)
        fixed (ushort* repeatedCountPtr = repeatedCount)
        fixed (uint* fromClampedPtr = fromClamped)
        fixed (uint* toClampedPtr = toClamped)
        fixed (uint* textureXLocPtr = textureXLoc)
        fixed (uint* textureYLocPtr = textureYLoc)
        fixed (uint* textureYIncrPtr = textureYIncr)
        {
            CoreRendererForUntiledTextures<DrawSimplePixel>.RenderWall(
                spriteFromX: 0, spriteToX: 2,
                width: ScreenWidth, textureHeight: TextureHeight,
                repeatedCountPtr,
                texturePtr, screenPtr,
                fromClampedPtr, toClampedPtr,
                textureXLocPtr, textureYLocPtr, textureYIncrPtr);

            AssertScreenEmpty(screen);
        }
    }
}
