using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void DrawWallShared(
            RenderablePortalWall renderableWall,
            TextureInfo textureInfo,
            scoped Span<ushort> repeatedCount,
            scoped Span<uint> textureXLocation,
            scoped Span<uint> textureYIncrement,
            scoped Span<uint> fromYClamped,
            scoped Span<uint> toYClamped)
        {
            RenderableWall wall = renderableWall.Wall;

            bool flipY = textureInfo.RenderingOptions.IsFlippedY;

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ref uint wallTexturePtr = ref textureInfo.Texture.GetBinaryRef<uint>(true, wall.Shade);
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
                CalculateAndCacheWallColumn(buffer, ref wallTexturePtr, (int)textureXPos, flipY);

                // count is the number of horizontal columns that stretch a single
                // texture columns
                if (repeat && count > 1)
                {
                    // Render 8 columns at once
                    while (Vector256.IsHardwareAccelerated && count > Vector256<uint>.Count)
                    {
                        RenderMultipleWallLinesV256(
                            (uint)width,
                            (uint)x,
                            textureWidth,
                            ref clamptedFromY,
                            ref clamptedToY,
                            ref textureYPos,
                            ref textureYIncr,
                            ref screenPtr,
                            ref buffer.Pointer
                        );

                        count -= (ushort)Vector256<uint>.Count;
                        x += Vector256<uint>.Count;
                    }

                    // Render 4 columns at once
                    while (Vector128.IsHardwareAccelerated && count > Vector128<uint>.Count)
                    {
                        RenderMultipleWallLinesV128(
                            (uint)width,
                            (uint)x,
                            textureWidth,
                            ref clamptedFromY,
                            ref clamptedToY,
                            ref textureYPos,
                            ref textureYIncr,
                            ref screenPtr,
                            ref buffer.Pointer
                        );

                        count -= (ushort)Vector128<uint>.Count;
                        x += Vector128<uint>.Count;
                    }

                    // Render the rest. Also fallback if CPU is potato.
                    if (count > 0)
                    {
                        RenderMultipleWallLines(
                            count,
                            (uint)width,
                            (uint)x,
                            textureWidth,
                            ref clamptedFromY,
                            ref clamptedToY,
                            ref textureYPos,
                            ref textureYIncr,
                            ref screenPtr,
                            ref buffer.Pointer
                        );

                        x += count;
                    }

                    x--;
                    continue;
                }

                RenderWallLine2(
                    (uint)width,
                    (uint)x,
                    textureWidth,
                    clamptedFromY,
                    clamptedToY,
                    textureYPos,
                    textureYIncr,
                    ref screenPtr,
                    ref buffer.Pointer);
            }
        }

        private static void RenderMultipleWallLinesV256(
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
            var textureXIncr_uV = Vector256.LoadUnsafe(ref textureXIncr_u);

            // cache lane count
            uint min_t = uint.MaxValue, max_t = 0;
            uint min_b = uint.MaxValue, max_b = 0;

            // compute per-lane tops/bottoms
            for (int i = 0; i < Vector256<uint>.Count; i++)
            {
                uint top = startYV[i];
                min_t = Math.Min(min_t, top);
                max_t = Math.Max(max_t, top);

                uint bottom = endYV[i];
                min_b = Math.Min(min_b, bottom);
                max_b = Math.Max(max_b, bottom);
            }

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
                        RenderWallLine2(width, xi, textureHeight, top, max_t, topTexturePosition, incr, ref screenPtr, ref textureBuffer);
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

                        RenderWallLine2(width, xi, textureHeight, min_b, bottom, bottomTexturePosition, incr, ref screenPtr, ref textureBuffer);
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
            Vector256<uint> textureMaskV = Vector256.Create((uint)(textureHeight - 1));

            // cache base ref for texture buffer and precompute step
            ref uint textureBufferRef = ref textureBuffer;
            uint widthMinusLanes = width - (uint)Vector256<uint>.Count;

            // go down the column set
            while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
            {
                Vector256<uint> texelIndexV = (textureXPos_uV >> 16) & textureMaskV;

                // horizontally draw the texture (keeps per-lane behavior but with cached refs)
                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    uint shaded = Unsafe.Add(ref textureBufferRef, texelIndexV[i]);

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, 1);
                }

                textureXPos_uV += textureXIncr_uV;
                screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, widthMinusLanes);
            }
        }

        private static void RenderMultipleWallLinesV128(
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

            // cache lane count
            uint min_t = uint.MaxValue, max_t = 0;
            uint min_b = uint.MaxValue, max_b = 0;

            // compute per-lane tops/bottoms
            for (int i = 0; i < Vector128<uint>.Count; i++)
            {
                uint top = startYV[i];
                min_t = Math.Min(min_t, top);
                max_t = Math.Max(max_t, top);

                uint bottom = endYV[i];
                min_b = Math.Min(min_b, bottom);
                max_b = Math.Max(max_b, bottom);
            }

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
                        RenderWallLine2(width, xi, textureHeight, top, max_t, topTexturePosition, incr, ref screenPtr, ref textureBuffer);
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

                        RenderWallLine2(width, xi, textureHeight, min_b, bottom, bottomTexturePosition, incr, ref screenPtr, ref textureBuffer);
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
            Vector128<uint> textureMaskV = Vector128.Create((uint)(textureHeight - 1));

            // cache base ref for texture buffer and precompute step
            ref uint textureBufferRef = ref textureBuffer;
            uint widthMinusLanes = width - (uint)Vector128<uint>.Count;

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


        private static void RenderMultipleWallLines(
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
                        RenderWallLine2(width, xi, textureHeight, top, max_t, topTexturePosition, incr, ref screenPtr, ref textureBuffer);
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

                        RenderWallLine2(width, xi, textureHeight, min_b, bottom, bottomTexturePosition, incr, ref screenPtr, ref textureBuffer);
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


        private static void RenderWallLine2(
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

            uint textureMask = (uint)(textureHeight - 1);

            while (!Unsafe.AreSame(in screenIndexPtr, in screenIndexPtrEnd))
            {
                uint texelIndex = (textureXPos_u >> 16) & textureMask;
                uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                screenIndexPtr = shaded;
                screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                textureXPos_u += textureXIncr_u;
            }
        }
    }
}
