using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Runtime.Intrinsics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void DrawWallShared(
            RenderablePortalWall renderableWall,
            TextureInfo textureInfo,
            scoped Span<ushort> repeatedCount,
            scoped Span<uint> fromYClamped,
            scoped Span<uint> toYClamped)
        {
            Span<uint> textureXLocation = memoryPool.GetBucket<uint>(MemoryPoolBucket.TextureXLocation);
            Span<uint> textureYIncrement = memoryPool.GetBucket<uint>(MemoryPoolBucket.TextureYIncrement);

            RenderableWall wall = renderableWall.Wall;

            bool flipY = textureInfo.RenderingOptions.IsFlippedY;

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ref uint wallTexturePtr = ref textureInfo.Texture.GetBinaryRef<uint>(wall.Shade, !flipY
                ? TextureTransform.Rotated : TextureTransform.RotatedFlipped);
            int textureWidth = textureInfo.Height;

            ref RenderColumnStatus statusRef = ref memoryPool.GetBucketRef<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            ref uint textureXLocationRef = ref textureXLocation[0];
            ref uint textureYIncrementRef = ref textureYIncrement[0];
            ref uint wallStartClampedRef = ref fromYClamped[0];
            ref uint wallEndClampedRef = ref toYClamped[0];
            ref uint textureYPosRefRef = ref memoryPool.GetBucketRef<uint>(MemoryPoolBucket.StartingYTexturePosition);

            ushort length = (ushort)(wallToX - wallFromX + 1);
            using TempBuffer<uint> buffer = TempBuffer<uint>.GetBuffer(textureInfo.Height);
            Span<ushort> repeatedCountB = TempBuffer<ushort>.GetBuffer(length);

            // See if any two columns share the same horizontal texture position
            // considering small texture size and large modern resolutions,
            // this is often true for textures up close
            bool repeat =
                SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount)
                && SharedHelpers.PopulateRepeatedValues(repeatedCountB, textureXLocation[wallFromX..wallToX])
                && SharedHelpers.RefineRepeatedValues(repeatedCount, repeatedCountB);

            bool isPowerOfTwo = SharedHelpers.IsPowerOfTwo(textureInfo.Height);

            if (!repeat)
            {
                for (int x = wallFromX; x <= wallToX; x++)
                {
                    ushort count = repeatedCount[x - wallFromX];

                    if (count == 0)
                    {
                        continue;
                    }

                    ref uint clamptedFromY = ref Unsafe.Add(ref wallStartClampedRef, x);
                    ref uint clamptedToY = ref Unsafe.Add(ref wallEndClampedRef, x);
                    ref uint textureYIncr = ref Unsafe.Add(ref textureYIncrementRef, x);
                    ref uint textureYPos = ref Unsafe.Add(ref textureYPosRefRef, x);
                    ref uint textureXPos = ref Unsafe.Add(ref textureXLocationRef, x);

                    RenderWallLine(
                        isPowerOfTwo,
                        (uint)width,
                        (uint)x,
                        textureWidth,
                        clamptedFromY,
                        clamptedToY,
                        textureYPos,
                        textureYIncr,
                        ref screenPtr,
                        ref Unsafe.Add(ref wallTexturePtr, textureXPos));
                }

                return;
            }

            for (int x = wallFromX; x <= wallToX;)
            {
                ushort count = repeatedCount[x - wallFromX];

                if (count == 0)
                {
                    x++;
                    continue;
                }

                ref uint clamptedFromY = ref Unsafe.Add(ref wallStartClampedRef, x);
                ref uint clamptedToY = ref Unsafe.Add(ref wallEndClampedRef, x);
                ref uint textureYIncr = ref Unsafe.Add(ref textureYIncrementRef, x);
                ref uint textureYPos = ref Unsafe.Add(ref textureYPosRefRef, x);
                ref uint textureXPos = ref Unsafe.Add(ref textureXLocationRef, x);

                // count is the number of horizontal columns that stretch a single
                // texture columns
                if (count > 1)
                {
                    // Render 8 columns at once
                    while (Vector256.IsHardwareAccelerated && count >= Vector256<uint>.Count)
                    {
                        RenderMultipleWallLinesV256(
                            isPowerOfTwo,
                            (uint)width,
                            (uint)x,
                            textureWidth,
                            ref clamptedFromY,
                            ref clamptedToY,
                            ref textureYPos,
                            ref textureYIncr,
                            ref screenPtr,
                            ref Unsafe.Add(ref wallTexturePtr, textureXPos)
                        );

                        count -= (ushort)Vector256<uint>.Count;
                        x += Vector256<uint>.Count;

                        clamptedFromY = ref Unsafe.Add(ref wallStartClampedRef, x);
                        clamptedToY = ref Unsafe.Add(ref wallEndClampedRef, x);
                        textureYIncr = ref Unsafe.Add(ref textureYIncrementRef, x);
                        textureYPos = ref Unsafe.Add(ref textureYPosRefRef, x);
                        //textureXPos = ref Unsafe.Add(ref textureXLocationRef, x);
                    }

                    // Render 4 columns at once
                    while (Vector128.IsHardwareAccelerated && count >= Vector128<uint>.Count)
                    {
                        RenderMultipleWallLinesV128(
                            isPowerOfTwo,
                            (uint)width,
                            (uint)x,
                            textureWidth,
                            ref clamptedFromY,
                            ref clamptedToY,
                            ref textureYPos,
                            ref textureYIncr,
                            ref screenPtr,
                            ref Unsafe.Add(ref wallTexturePtr, textureXPos)
                        );

                        count -= (ushort)Vector128<uint>.Count;
                        x += Vector128<uint>.Count;

                        clamptedFromY = ref Unsafe.Add(ref wallStartClampedRef, x);
                        clamptedToY = ref Unsafe.Add(ref wallEndClampedRef, x);
                        textureYIncr = ref Unsafe.Add(ref textureYIncrementRef, x);
                        textureYPos = ref Unsafe.Add(ref textureYPosRefRef, x);
                        //textureXPos = ref Unsafe.Add(ref textureXLocationRef, x);
                    }

                    // Render the rest. Also fallback if CPU is potato.
                    if (count > 0)
                    {
                        RenderMultipleWallLines(
                            isPowerOfTwo,
                            count,
                            (uint)width,
                            (uint)x,
                            textureWidth,
                            ref clamptedFromY,
                            ref clamptedToY,
                            ref textureYPos,
                            ref textureYIncr,
                            ref screenPtr,
                            ref Unsafe.Add(ref wallTexturePtr, textureXPos)
                        );

                        x += count;
                    }

                    continue;
                }

                RenderWallLine(
                    isPowerOfTwo,
                    (uint)width,
                    (uint)x,
                    textureWidth,
                    clamptedFromY,
                    clamptedToY,
                    textureYPos,
                    textureYIncr,
                    ref screenPtr,
                    ref Unsafe.Add(ref wallTexturePtr, textureXPos));

                x++;
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

        private static void RenderMultipleWallLinesV256(
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            scoped ref uint startY,
            scoped ref uint endY,
            scoped ref uint textureXPos_u,
            scoped ref uint textureXIncr_u,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
            )
        {
            var startYV = Vector256.LoadUnsafe(ref startY);
            var endYV = Vector256.LoadUnsafe(ref endY);

            (uint min_t, uint max_t) = GetMinMaxValue(startYV);
            (uint min_b, uint max_b) = GetMinMaxValue(endYV);

            var textureXIncr_uV = Vector256.LoadUnsafe(ref textureXIncr_u);

            // render tops where there is no shared window
            if (min_t != max_t)
            {
                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    uint top = startYV[i];

                    if (top < max_t)
                    {
                        ref uint textureXPos = ref Unsafe.Add(ref textureXPos_u, i);
                        uint incr = textureXIncr_uV[i];
                        uint xi = x + (uint)i;

                        uint topTexturePosition = textureXPos + incr * (max_t - top);
                        RenderWallLine(isPowerOfTwo, width, xi, textureHeight, top, max_t, topTexturePosition, incr, ref screenPtr, ref textureBuffer);
                        textureXPos = topTexturePosition;
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
                        ref uint textureXPos = ref Unsafe.Add(ref textureXPos_u, i);
                        uint incr = textureXIncr_uV[i];
                        uint bottomTexturePosition = textureXPos + (incr * min_b);
                        uint xi = x + (uint)i;

                        RenderWallLine(isPowerOfTwo, width, xi, textureHeight, min_b, bottom, bottomTexturePosition, incr, ref screenPtr, ref textureBuffer);
                    }
                }
            }

            // no shared window
            if (min_b <= max_t)
            {
                return;
            }

            // prepare for the shared vertical window
            Vector256<uint> textureXPos_uV = Vector256.LoadUnsafe(ref textureXPos_u);
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, (int)(max_t * width + x));
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, (int)(min_b * width + x));

            // cache base ref for texture buffer and precompute step
            uint widthMinusLanes = width - (uint)Vector256<uint>.Count;

            if (isPowerOfTwo)
            {
                Vector256<uint> textureMaskV = Vector256.Create((uint)(textureHeight - 1));

                // go down the column set
                while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    Vector256<uint> texelIndexV = (textureXPos_uV >> 16) & textureMaskV;

                    // horizontally draw the texture
                    for (int i = 0; i < Vector256<uint>.Count; i++)
                    {
                        uint shaded = Unsafe.Add(ref textureBuffer, texelIndexV[i]);

                        screenIndexPtr = shaded;
                        screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, 1);
                    }

                    textureXPos_uV += textureXIncr_uV;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, widthMinusLanes);
                }
            }
            else
            {
                uint textureHeightMask = (uint)textureHeight;

                // go down the column set
                while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    Vector256<uint> texelIndexV = textureXPos_uV >> 16;

                    // horizontally draw the texture (keeps per-lane behavior)
                    for (int i = 0; i < Vector256<uint>.Count; i++)
                    {
                        uint shaded = Unsafe.Add(ref textureBuffer, texelIndexV[i] % textureHeightMask);

                        screenIndexPtr = shaded;
                        screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, 1);
                    }

                    textureXPos_uV += textureXIncr_uV;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, widthMinusLanes);
                }
            }
        }

        private static void RenderMultipleWallLinesV128(
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            scoped ref uint startY,
            scoped ref uint endY,
            scoped ref uint textureXPos_u,
            scoped ref uint textureXIncr_u,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
            )
        {
            var startYV = Vector128.LoadUnsafe(ref startY);
            var endYV = Vector128.LoadUnsafe(ref endY);
            var textureXIncr_uV = Vector128.LoadUnsafe(ref textureXIncr_u);

            (uint min_t, uint max_t) = GetMinMaxValue(startYV);
            (uint min_b, uint max_b) = GetMinMaxValue(endYV);

            // render tops where there is no shared window
            if (min_t != max_t)
            {
                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    uint top = startYV[i];

                    if (top < max_t)
                    {
                        ref uint textureXPos = ref Unsafe.Add(ref textureXPos_u, i);
                        uint incr = textureXIncr_uV[i];
                        uint xi = x + (uint)i;

                        uint topTexturePosition = textureXPos + incr * (max_t - top);
                        RenderWallLine(isPowerOfTwo, width, xi, textureHeight, top, max_t, topTexturePosition, incr, ref screenPtr, ref textureBuffer);
                        textureXPos = topTexturePosition;
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
                        ref uint textureXPos = ref Unsafe.Add(ref textureXPos_u, i);
                        uint incr = textureXIncr_uV[i];
                        uint bottomTexturePosition = textureXPos + (incr * min_b);
                        uint xi = x + (uint)i;

                        RenderWallLine(isPowerOfTwo, width, xi, textureHeight, min_b, bottom, bottomTexturePosition, incr, ref screenPtr, ref textureBuffer);
                    }
                }
            }

            // no shared window
            if (min_b <= max_t)
            {
                return;
            }

            // prepare for the shared vertical window
            Vector128<uint> textureXPos_uV = Vector128.LoadUnsafe(ref textureXPos_u);
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, (int)(max_t * width + x));
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, (int)(min_b * width + x));

            // cache base ref for texture buffer and precompute step
            ref uint textureBufferRef = ref textureBuffer;
            uint widthMinusLanes = width - (uint)Vector128<uint>.Count;

            if (isPowerOfTwo)
            {
                Vector128<uint> textureMaskV = Vector128.Create((uint)(textureHeight - 1));

                // go down the column set
                while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    Vector128<uint> texelIndexV = (textureXPos_uV >> 16) & textureMaskV;

                    // horizontally draw the texture (keeps per-lane behavior but with cached refs)
                    for (int i = 0; i < Vector128<uint>.Count; i++)
                    {
                        uint shaded = Unsafe.Add(ref textureBufferRef, texelIndexV[i]);

                        screenIndexPtr = shaded;
                        screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, 1);
                    }

                    textureXPos_uV += textureXIncr_uV;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, widthMinusLanes);
                }
            }
            else
            {
                uint textureMask = (uint)textureHeight;

                // go down the column set
                while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    Vector128<uint> texelIndexV = textureXPos_uV >> 16;

                    // horizontally draw the texture (keeps per-lane behavior but with cached refs)
                    for (int i = 0; i < Vector128<uint>.Count; i++)
                    {
                        uint shaded = Unsafe.Add(ref textureBufferRef, texelIndexV[i] % textureMask);

                        screenIndexPtr = shaded;
                        screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, 1);
                    }

                    textureXPos_uV += textureXIncr_uV;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, widthMinusLanes);
                }
            }
        }


        private static void RenderMultipleWallLines(
            bool isPowerOfTwo,
            uint count,
            uint width,
            uint x,
            int textureHeight,
            scoped ref uint startY,
            scoped ref uint endY,
            scoped ref uint textureXPos_u,
            scoped ref uint textureXIncr_u,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
            )
        {
            // each line can start and end at different y positions
            // so we determine the window where all lines can be rendered at once
            uint min_t = int.MaxValue, max_t = 0;
            uint min_b = int.MaxValue, max_b = 0;

            for (int i = 0; i < count; i++)
            {
                ref uint top = ref Unsafe.Add(ref startY, i);
                min_t = Math.Min(min_t, top);
                max_t = Math.Max(max_t, top);

                ref uint bottom = ref Unsafe.Add(ref endY, i);
                min_b = Math.Min(min_b, bottom);
                max_b = Math.Max(max_b, bottom);
            }

            // render tops of each line where there is no shared window
            if (min_t != max_t)
            {
                for (int i = 0; i < count; i++)
                {
                    ref uint top = ref Unsafe.Add(ref startY, i);

                    if (top < max_t)
                    {
                        ref uint textureXPos = ref Unsafe.Add(ref textureXPos_u, i);
                        uint incr = Unsafe.Add(ref textureXIncr_u, i);

                        uint xi = x + (uint)i;

                        uint topTexturePosition = textureXPos + incr * (max_t - top);
                        RenderWallLine(isPowerOfTwo, width, xi, textureHeight, top, max_t, topTexturePosition, incr, ref screenPtr, ref textureBuffer);
                        textureXPos = topTexturePosition;
                    }
                }
            }

            // render bottoms of each line where there is no shared window
            if (min_b != max_b)
            {
                for (int i = 0; i < count; i++)
                {
                    ref uint bottom = ref Unsafe.Add(ref endY, i);

                    if (bottom > min_b)
                    {
                        ref uint textureXPos = ref Unsafe.Add(ref textureXPos_u, i);
                        uint incr = Unsafe.Add(ref textureXIncr_u, i);
                        uint bottomTexturePosition = textureXPos + (incr * min_b);
                        uint xi = x + (uint)i;

                        RenderWallLine(isPowerOfTwo, width, xi, textureHeight, min_b, bottom, bottomTexturePosition, incr, ref screenPtr, ref textureBuffer);
                    }
                }
            }

            // no shared window
            if (min_b <= max_t)
            {
                return;
            }

            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, max_t * width + x);
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, min_b * width + x);

            if (isPowerOfTwo)
            {
                uint textureMask = (uint)(textureHeight - 1);

                // go down the column set
                while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    // horizontally draw the texture
                    for (int i = 0; i < count; i++)
                    {
                        ref uint textureXPos = ref Unsafe.Add(ref textureXPos_u, i);
                        uint texelIndex = (textureXPos >> 16) & textureMask;
                        uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                        screenIndexPtr = shaded;
                        textureXPos += Unsafe.Add(ref textureXIncr_u, i);
                        screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, 1);
                    }

                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width - count);
                }
            }
            else
            {
                uint textureMask = (uint)(textureHeight);

                // go down the column set
                while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    // horizontally draw the texture
                    for (int i = 0; i < count; i++)
                    {
                        ref uint textureXPos = ref Unsafe.Add(ref textureXPos_u, i);
                        uint texelIndex = (textureXPos >> 16) % textureMask;
                        uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                        screenIndexPtr = shaded;
                        textureXPos += Unsafe.Add(ref textureXIncr_u, i);
                        screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, 1);
                    }

                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width - count);
                }
            }
        }


        private static void RenderWallLine(
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            uint startY,
            uint endY,
            uint textureXPos_u,
            uint textureXIncr_u,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
            )
        {
            Debug.Assert(endY >= startY);
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, startY * width + x);
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, endY * width + x);

            if (isPowerOfTwo)
            {
                uint textureHeightMask = (uint)(textureHeight - 1);

                while (!Unsafe.AreSame(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    uint texelIndex = (textureXPos_u >> 16) & textureHeightMask;
                    uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos_u += textureXIncr_u;
                }
            }
            else
            {
                uint textureHeightMask = (uint)textureHeight;

                while (!Unsafe.AreSame(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    uint texelIndex = (textureXPos_u >> 16) % textureHeightMask;
                    uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos_u += textureXIncr_u;
                }
            }
        }
    }
}
