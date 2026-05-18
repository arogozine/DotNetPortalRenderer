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

            uint* wallTexturePtr = (uint*)Unsafe.AsPointer(ref textureInfo.Texture.GetBinaryRef<uint>(wall.Shade, transform));
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
                    RenderMultipleWallLinesV256(
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

        private unsafe static void RenderMultipleWallLinesV256(
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            uint* startY,
            uint* endY,
            uint* textureYPos_u,
            uint* textureYIncr_u,
            uint* screenPtr,
            uint* texturePos,
            uint* textureBuffer
    )
        {
            var startYV = Vector256.Load(startY);
            var endYV = Vector256.Load(endY);
            var textureXIncr_uV = Vector256.Load(textureYIncr_u);

            (uint min_t, uint max_t) = GetMinMaxValue(startYV);
            (uint min_b, uint max_b) = GetMinMaxValue(endYV);

            if (min_b <= max_t)
            {
                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    uint* textureYPos = textureYPos_u + i;
                    uint incr = textureXIncr_uV[i];
                    uint xi = x + (uint)i;

                    uint top = startYV[i];
                    uint bottom = endYV[i];

                    RenderWallColumn(isPowerOfTwo, width, xi, textureHeight, top, bottom, *textureYPos, incr, screenPtr,
                        textureBuffer + *(texturePos + i));
                }

                return;
            }

            // render tops where there is no shared window
            if (min_t != max_t)
            {
                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    uint top = startYV[i];

                    if (top < max_t)
                    {
                        uint* textureYPos = textureYPos_u + i;
                        uint incr = textureXIncr_uV[i];
                        uint xi = x + (uint)i;

                        *textureYPos = RenderWallColumn2(isPowerOfTwo, width, xi, textureHeight, top, max_t, *textureYPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i)
                        );
                    }
                }
            }
            Vector256<uint> textureYPos_uV = Vector256.Load(textureYPos_u);

            if (min_b > max_t)
            {
                // prepare for the shared vertical window
                Vector256<uint> textureXPosV = Vector256.Load(texturePos);
                uint* screenIndexPtr = screenPtr + (max_t * width + x);
                uint* screenIndexPtrEnd = screenPtr + (min_b * width + x);

                // cache base Ptr for texture buffer and precompute step
                uint widthMinusLanes = width - (uint)Vector256<uint>.Count;

                if (isPowerOfTwo)
                {
                    Vector256<uint> textureMaskV = Vector256.Create((uint)(textureHeight - 1));

                    if (Avx2.IsSupported)
                    {
                        while (screenIndexPtr < screenIndexPtrEnd)
                        {
                            Vector256<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                            texelIndexV += textureXPosV;

                            Vector256<uint> gathered = Avx2.GatherVector256(
                                textureBuffer,
                                texelIndexV.AsInt32(),
                                scale: sizeof(uint)
                            );

                            gathered.Store(screenIndexPtr);

                            textureYPos_uV += textureXIncr_uV;
                            screenIndexPtr += width;
                        }
                    }
                    else
                    {
                        // go down the column set
                        while (screenIndexPtr < screenIndexPtrEnd)
                        {
                            Vector256<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                            texelIndexV += textureXPosV;

                            // horizontally draw the texture
                            for (int i = 0; i < Vector256<uint>.Count; i++)
                            {
                                uint shaded = *(textureBuffer + texelIndexV[i]);

                                *screenIndexPtr = shaded;
                                screenIndexPtr++;
                            }

                            textureYPos_uV += textureXIncr_uV;
                            screenIndexPtr += widthMinusLanes;
                        }
                    }
                }
                else
                {
                    uint textureHeightMask = (uint)textureHeight;

                    // go down the column set
                    while (screenIndexPtr < screenIndexPtrEnd)
                    {
                        Vector256<uint> texelIndexV = textureYPos_uV >> 16;

                        // horizontally draw the texture (keeps per-lane behavior)
                        for (int i = 0; i < Vector256<uint>.Count; i++)
                        {
                            uint shaded = *(textureBuffer + textureXPosV[i] + (texelIndexV[i] % textureHeightMask));

                            *screenIndexPtr = shaded;
                            screenIndexPtr++;
                        }

                        textureYPos_uV += textureXIncr_uV;
                        screenIndexPtr += widthMinusLanes;
                    }
                }
            }

            // render bottoms where there is no shared window
            if (min_b != max_b)
            {
                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    uint bottom = endYV[i];

                    if (bottom > min_b)
                    {
                        uint textureXPos = textureYPos_uV[i];
                        uint incr = textureXIncr_uV[i];

                        uint xi = x + (uint)i;

                        RenderWallColumn(isPowerOfTwo, width, xi, textureHeight, min_b, bottom, textureXPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i)
                        );
                    }
                }
            }
        }

        private static unsafe void RenderMultipleWallLinesV128(
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            uint* startY,
            uint* endY,
            uint* textureYPos_u,
            uint* textureYIncr_u,
            uint* screenPtr,
            uint* texturePos,
            uint* textureBuffer
            )
        {
            var startYV = Vector128.Load(startY);
            var endYV = Vector128.Load(endY);
            var textureXIncr_uV = Vector128.Load(textureYIncr_u);

            (uint min_t, uint max_t) = GetMinMaxValue(startYV);
            (uint min_b, uint max_b) = GetMinMaxValue(endYV);

            if (min_b <= max_t)
            {
                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    uint* textureYPos = textureYPos_u + i;
                    uint incr = textureXIncr_uV[i];
                    uint xi = x + (uint)i;

                    uint top = startYV[i];
                    uint bottom = endYV[i];

                    RenderWallColumn(isPowerOfTwo, width, xi, textureHeight, top, bottom, *textureYPos, incr, screenPtr,
                        textureBuffer + *(texturePos + i));
                }

                return;
            }

            // render tops where there is no shared window
            if (min_t != max_t)
            {
                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    uint top = startYV[i];

                    if (top < max_t)
                    {
                        uint* textureYPos = textureYPos_u + i;
                        uint incr = textureXIncr_uV[i];
                        uint xi = x + (uint)i;

                        *textureYPos = RenderWallColumn2(isPowerOfTwo, width, xi, textureHeight, top, max_t, *textureYPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i));
                    }
                }
            }

            Vector128<uint> textureYPos_uV = Vector128.Load(textureYPos_u);

            // shared window
            if (min_b > max_t)
            {
                // prepare for the shared vertical window
                Vector128<uint> textureXPosV = Vector128.Load(texturePos);
                uint* screenIndexPtr = screenPtr + (max_t * width + x);
                uint* screenIndexPtrEnd = screenPtr + (min_b * width + x);

                // cache base Ptr for texture buffer and precompute step
                uint widthMinusLanes = width - (uint)Vector128<uint>.Count;

                if (isPowerOfTwo)
                {
                    Vector128<uint> textureMaskV = Vector128.Create((uint)(textureHeight - 1));

                    // go down the column set
                    while (screenIndexPtr < screenIndexPtrEnd)
                    {
                        Vector128<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                        texelIndexV += textureXPosV;

                        if (Avx2.IsSupported)
                        {
                            Vector128<uint> gathered = Avx2.GatherVector128(
                                textureBuffer,
                                texelIndexV.AsInt32(),
                                scale: sizeof(uint)
                            );

                            gathered.Store(screenIndexPtr);

                            screenIndexPtr += Vector128<uint>.Count;
                        }
                        else
                        {
                            // horizontally draw the texture (keeps per-lane behavior but with cached Ptrs)
                            for (int i = 0; i < Vector128<uint>.Count; i++)
                            {
                                uint shaded = *(textureBuffer + texelIndexV[i]);

                                *screenIndexPtr = shaded;
                                screenIndexPtr++;
                            }

                        }

                        textureYPos_uV += textureXIncr_uV;
                        screenIndexPtr += widthMinusLanes;
                    }
                }
                else
                {
                    uint textureMask = (uint)textureHeight;

                    // go down the column set
                    while (screenIndexPtr < screenIndexPtrEnd)
                    {
                        Vector128<uint> texelIndexV = textureYPos_uV >> 16;

                        // horizontally draw the texture (keeps per-lane behavior but with cached Ptrs)
                        for (int i = 0; i < Vector128<uint>.Count; i++)
                        {
                            uint shaded = *(textureBuffer + textureXPosV[i] + (texelIndexV[i] % textureMask));

                            *screenIndexPtr = shaded;
                            screenIndexPtr++;
                        }

                        textureYPos_uV += textureXIncr_uV;
                        screenIndexPtr += widthMinusLanes;
                    }
                }
            }

            // render bottoms where there is no shared window
            if (min_b != max_b)
            {
                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    uint bottom = endYV[i];

                    if (bottom > min_b)
                    {
                        uint textureXPos = textureYPos_uV[i];
                        uint incr = textureXIncr_uV[i];
                        uint xi = x + (uint)i;

                        RenderWallColumn(isPowerOfTwo, width, xi, textureHeight, min_b, bottom, textureXPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i));
                    }
                }
            }
        }


        private static unsafe void RenderMultipleWallLines(
            bool isPowerOfTwo,
            uint count,
            uint width,
            uint x,
            int textureHeight,
            uint* startY,
            uint* endY,
            uint* textureYPos_u,
            uint* textureYIncr_u,
            uint* screenPtr,
            uint* texturePos,
            uint* textureBuffer
            )
        {
            // each line can start and end at different y positions
            // so we determine the window where all lines can be rendered at once
            uint min_t = int.MaxValue, max_t = 0;
            uint min_b = int.MaxValue, max_b = 0;

            for (int i = 0; i < count; i++)
            {
                uint top = *(startY + i);
                min_t = Math.Min(min_t, top);
                max_t = Math.Max(max_t, top);

                uint bottom = *(endY + i);
                min_b = Math.Min(min_b, bottom);
                max_b = Math.Max(max_b, bottom);
            }

            if (min_b <= max_t)
            {
                for (uint i = 0; i < count; i++)
                {
                    uint textureYPos = *(textureYPos_u + i);
                    uint incr = *(textureYIncr_u + i);
                    uint xi = x + i;
                    uint top = *(startY + i);
                    uint bottom = *(endY + i);

                    RenderWallColumn(isPowerOfTwo, width, xi, textureHeight, top, bottom, textureYPos, incr, screenPtr,
                        textureBuffer + *(texturePos + i));
                }

                return;
            }

            // render tops of each line where there is no shared window
            if (min_t < max_t)
            {
                for (uint i = 0; i < count; i++)
                {
                    uint top = *(startY + i);

                    if (top < max_t)
                    {
                        uint* textureYPos = textureYPos_u + i;
                        uint incr = *(textureYIncr_u + i);

                        uint xi = x + i;

                        *textureYPos = RenderWallColumn2(isPowerOfTwo, width, xi, textureHeight, top, max_t, *textureYPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i));
                    }
                }
            }

            // shared window
            if (min_b > max_t)
            {
                uint* screenIndexPtr = screenPtr + max_t * width + x;
                uint* screenIndexPtrEnd = screenPtr + min_b * width + x;

                if (isPowerOfTwo)
                {
                    uint textureMask = (uint)(textureHeight - 1);

                    // go down the column set
                    while (screenIndexPtr < screenIndexPtrEnd)
                    {
                        // horizontally draw the texture
                        for (int i = 0; i < count; i++)
                        {
                            uint* textureXPos = textureYPos_u + i;
                            uint texelIndex = (*textureXPos >> 16) & textureMask;
                            texelIndex += *(texturePos + i);

                            uint pixel = *(textureBuffer + texelIndex);

                            *screenIndexPtr = pixel;
                            *textureXPos += *(textureYIncr_u + i);
                            screenIndexPtr++;
                        }

                        screenIndexPtr += width - count;
                    }
                }
                else
                {
                    uint textureMask = (uint)textureHeight;

                    // go down the column set
                    while (screenIndexPtr < screenIndexPtrEnd)
                    {
                        // horizontally draw the texture
                        for (int i = 0; i < count; i++)
                        {
                            uint* textureYPos = textureYPos_u + i;
                            uint texelIndex = (*textureYPos >> 16) % textureMask;
                            texelIndex += *(texturePos + i);

                            uint pixel = *(textureBuffer + texelIndex);

                            *screenIndexPtr = pixel;
                            *textureYPos += *(textureYIncr_u + i);
                            screenIndexPtr++;
                        }

                        screenIndexPtr += width - count;
                    }
                }
            }

            // render bottoms of each line where there is no shared window
            if (min_b < max_b)
            {
                for (uint i = 0; i < count; i++)
                {
                    uint bottom = *(endY + i);

                    if (bottom > min_b)
                    {
                        uint textureXPos = *(textureYPos_u + i);
                        uint incr = *(textureYIncr_u + i);
                        uint xi = x + i;

                        RenderWallColumn(isPowerOfTwo, width, xi, textureHeight, min_b, bottom, textureXPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i));
                    }
                }
            }
        }

        private static unsafe uint RenderWallColumn2(
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            uint startY,
            uint endY,
            uint textureYPos_u,
            uint textureYIncr_u,
            uint* screenPtr,
            uint* textureBuffer
            )
        {
            Debug.Assert(endY >= startY);
            uint* screenIndexPtr = screenPtr + startY * width + x;
            uint* screenIndexPtrEnd = screenPtr + endY * width + x;

            if (isPowerOfTwo)
            {
                uint textureHeightMask = (uint)(textureHeight - 1);

                while (screenIndexPtr != screenIndexPtrEnd)
                {
                    uint texelIndex = (textureYPos_u >> 16) & textureHeightMask;
                    uint pixel = *(textureBuffer + texelIndex);

                    *screenIndexPtr = pixel;
                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }
            else
            {
                uint textureHeightMask = (uint)textureHeight;

                while (screenIndexPtr != screenIndexPtrEnd)
                {
                    uint texelIndex = (textureYPos_u >> 16) % textureHeightMask;
                    uint pixel = *(textureBuffer + texelIndex);

                    *screenIndexPtr = pixel;
                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }

            return textureYPos_u;
        }

        private static unsafe void RenderWallColumn(
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            uint startY,
            uint endY,
            uint textureYPos_u,
            uint textureYIncr_u,
            uint* screenPtr,
            uint* textureBuffer
            )
        {
            Debug.Assert(endY >= startY);
            uint* screenIndexPtr = screenPtr + startY * width + x;
            uint* screenIndexPtrEnd = screenPtr + endY * width + x;

            if (isPowerOfTwo)
            {
                uint textureHeightMask = (uint)(textureHeight - 1);

                while (screenIndexPtr != screenIndexPtrEnd)
                {
                    uint texelIndex = (textureYPos_u >> 16) & textureHeightMask;
                    uint pixel = *(textureBuffer + texelIndex);

                    *screenIndexPtr = pixel;
                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }
            else
            {
                uint textureHeightMask = (uint)textureHeight;

                while (screenIndexPtr != screenIndexPtrEnd)
                {
                    uint texelIndex = (textureYPos_u >> 16) % textureHeightMask;
                    uint pixel = *(textureBuffer + texelIndex);

                    *screenIndexPtr = pixel;
                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }
        }
    }
}
