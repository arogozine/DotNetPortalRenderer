using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private unsafe void DrawWallShared(
            RenderablePortalWall renderableWall,
            GameTextureInfo textureInfo,
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
                    CoreRenderer<DrawSimplePixel>.RenderMultipleWallLinesV256(
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
                    CoreRenderer<DrawSimplePixel>.RenderMultipleWallLinesV128(
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

                CoreRenderer<DrawSimplePixel>.RenderMultipleWallLines(
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
    }
}
