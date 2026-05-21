using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private unsafe void DrawWallShared(
            RenderablePortalWall renderableWall,
            TextureInfo textureInfo,
            scoped Span<ushort> repeatedCount,
            uint* fromYClamped,
            uint* toYClamped)
        {
            RenderableWall wall = renderableWall.Wall;

            bool flipY = textureInfo.RenderingOptions.IsFlippedY;
            bool flipX = textureInfo.RenderingOptions.IsFlippedX;

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            uint* screenPtr = (uint*)buffer;

            var transform = TextureTransform.Rotated;

            if (flipY)
            {
                transform |= TextureTransform.FlippedY;
            }

            if (flipX)
            {
                transform |= TextureTransform.FlippedX;
            }

            uint* textureYPosPtrPtr = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureXLocation = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureXLocation);
            uint* textureYIncrement = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);

            uint* wallTexturePtr = (uint*)Unsafe.AsPointer(ref textureInfo.Texture.GetBinaryRef<uint>(wall.Shade ?? default, transform));
            int textureWidth = textureInfo.Height;

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            bool isPowerOfTwo = SharedHelpers.IsPowerOfTwo(textureInfo.Height);

            Sse.Prefetch2(wallTexturePtr);

            var drawSimplePixel = new DrawSimplePixel();

            for (int x = wallFromX; x <= wallToX;)
            {
                ushort count = repeatedCount[x - wallFromX];

                if (count == 0)
                {
                    x++;
                    continue;
                }

                uint* clamptedFromY = fromYClamped + x;
                uint* clamptedToY = toYClamped + x;
                uint* textureYIncr = textureYIncrement + x;
                uint* textureYPos = textureYPosPtrPtr + x;
                uint* textureXPos = textureXLocation + x;

                if (Vector256.IsHardwareAccelerated && count >= Vector256<uint>.Count)
                {
                    RenderMultipleWallLinesV256(
                        drawSimplePixel,
                        isPowerOfTwo,
                        (uint)width,
                        (uint)x,
                        textureWidth,
                        clamptedFromY,
                        clamptedToY,
                        textureYPos,
                        textureYIncr,
                        screenPtr,
                        textureXPos,
                        wallTexturePtr
                    );

                    x += Vector256<uint>.Count;
                    continue;
                }

                if (Vector128.IsHardwareAccelerated && count >= Vector128<uint>.Count)
                {
                    RenderMultipleWallLinesV128(
                        drawSimplePixel,
                        isPowerOfTwo,
                        (uint)width,
                        (uint)x,
                        textureWidth,
                        clamptedFromY,
                        clamptedToY,
                        textureYPos,
                        textureYIncr,
                        screenPtr,
                        textureXPos,
                        wallTexturePtr
                    );

                    x += Vector128<uint>.Count;
                    continue;
                }

                RenderMultipleWallLines(
                    drawSimplePixel,
                    isPowerOfTwo,
                    count,
                    (uint)width,
                    (uint)x,
                    textureWidth,
                    clamptedFromY,
                    clamptedToY,
                    textureYPos,
                    textureYIncr,
                    screenPtr,
                    textureXPos,
                    wallTexturePtr
                );

                x += count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (uint min, uint max) GetMinMaxValue(Vector128<uint> value)
        {
            var valueShuffled = Vector128.ShuffleNative(value, Vector128.Create(2U, 3U, 0U, 1U));
            var valueMax = Vector128.MaxNative(value, valueShuffled);
            var valueMin = Vector128.MinNative(value, valueShuffled);

            uint min = Math.Min(valueMin[0], valueMin[1]);
            uint max = Math.Max(valueMax[0], valueMax[1]);

            return (min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int min, int max) GetMinMaxValue(Vector128<int> value)
        {
            var valueShuffled = Vector128.ShuffleNative(value, Vector128.Create(2, 3, 0, 1));
            var valueMax = Vector128.MaxNative(value, valueShuffled);
            var valueMin = Vector128.MinNative(value, valueShuffled);

            int min = MathFormulas.Min(valueMin[0], valueMin[1]);
            int max = MathFormulas.Max(valueMax[0], valueMax[1]);

            return (min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int min, int max) GetMinMaxValue(Vector256<int> value)
        {
            var valueLower = value.GetLower();
            var valueUpper = value.GetUpper();

            var value128min = Vector128.MinNative(valueLower, valueUpper);
            var shuffle = Vector128.ShuffleNative(value128min, Vector128.Create(2, 3, 0, 1));
            value128min = Vector128.MinNative(value128min, shuffle);

            var value128max = Vector128.MaxNative(valueLower, valueUpper);
            shuffle = Vector128.ShuffleNative(value128max, Vector128.Create(2, 3, 0, 1));
            value128max = Vector128.MaxNative(value128max, shuffle);

            int min = MathFormulas.Min(value128min[0], value128min[1]);
            int max = MathFormulas.Max(value128max[0], value128max[1]);

            return (min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (uint min, uint max) GetMinMaxValue(Vector256<uint> value)
        {
            var valueLower = value.GetLower();
            var valueUpper = value.GetUpper();

            var value128min = Vector128.MinNative(valueLower, valueUpper);
            var shuffle = Vector128.ShuffleNative(value128min, Vector128.Create(2U, 3U, 0U, 1U));
            value128min = Vector128.MinNative(value128min, shuffle);

            var value128max = Vector128.MaxNative(valueLower, valueUpper);
            shuffle = Vector128.ShuffleNative(value128max, Vector128.Create(2U, 3U, 0U, 1U));
            value128max = Vector128.MaxNative(value128max, shuffle);

            uint min = Math.Min(value128min[0], value128min[1]);
            uint max = Math.Max(value128max[0], value128max[1]);

            return (min, max);
        }
    }
}
