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

        private void CalculateRenderWindow2(bool fromTop, RenderablePortalWall portalWall, TextureInfo textureInfo)//, ReadOnlySpan<Sector> sectors)
        {
            RenderableWall wall = portalWall.Wall;

            // Span<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            // Span<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);
            // Span<int> portalFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFrom);
            // Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);
            Span<int> textureYIncrement = memoryPool.GetBucket<int>(fromTop ? MemoryPoolBucket.TopTextureYIncrement : MemoryPoolBucket.BottomTextureYIncrement);

            (int textureHeight, _, _, float scaledTextureWidth) = CalculateScale(wall.Sector, wall, textureInfo);

            int offset = portalWall.Offset;
            int wallFromX = portalWall.XLeft;
            int wallToX = portalWall.XRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;
            float? portalStartY = yPlaneInfo.PortalStartY;
            float? portalEndY = yPlaneInfo.PortalEndY;
            float? portalStartIncr = yPlaneInfo.PortalStartIncr;
            float? portalEndIncr = yPlaneInfo.PortalEndIncr;

            bool slopedTop = wall.IsPortal && portalStartY != null && portalStartIncr != null;
            bool slopedBottom = wall.IsPortal && portalEndY != null && portalEndIncr != null;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = RenderWindowHelper.Status[x];

                if (columnStatus.IsFinished || !columnStatus.WallRenderable)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;

                    if (slopedTop)
                    {
                        portalStartY += portalStartIncr;
                    }

                    if (slopedBottom)
                    {
                        portalEndY += portalEndIncr;
                    }

                    continue;
                }

                Debug.Assert(slopedTop == false);
                Debug.Assert(slopedTop == slopedBottom);

                int textureYIncr = float.ConvertToIntegerNative<int>(scaledTextureWidth / (wallEndY - wallStartY));
                textureYIncrement[x] = textureYIncr;

                if (slopedTop && slopedBottom)
                {
                    // portalFrom[x] = float.ConvertToIntegerNative<int>(portalStartY!.Value);
                    // portalTo[x] = float.ConvertToIntegerNative<int>(portalEndY!.Value);
                }
                else if (wall.IsPortal)
                {
                    /*
                    (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, wall);

                    float pixelsPerHeight = (wallEndY - wallStartY) / sectorHeight;

                    float ceilPixelOffset = pixelsPerHeight * ceilOffset;
                    float floorPixelOffset = pixelsPerHeight * floorOffset;
                    float portalToY = wallEndY - floorPixelOffset;
                    float portalFromY = wallStartY - ceilPixelOffset;

                    portalFrom[x] = float.ConvertToIntegerNative<int>(portalFromY);
                    portalTo[x] = float.ConvertToIntegerNative<int>(portalToY);
                    */
                }
                /*
                int wallStartYInt = float.ConvertToIntegerNative<int>(wallStartY);
                int wallEndYInt = float.ConvertToIntegerNative<int>(wallEndY);

                wallStart[x] = wallStartYInt;
                wallEnd[x] = wallEndYInt;
                */
                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;

                if (slopedTop)
                {
                    portalStartY += portalStartIncr;
                }

                if (slopedBottom)
                {
                    portalEndY += portalEndIncr;
                }

            }
        }


        private void CalculateTextureYStartAndIncrement(bool fromTop, RenderablePortalWall renderableWall, TextureInfo textureInfo)
        {
            Span<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            Span<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);

            Span<int> textureYIncrement = memoryPool.GetBucket<int>(fromTop ? MemoryPoolBucket.TopTextureYIncrement : MemoryPoolBucket.BottomTextureYIncrement);
            Span<int> startingYTexturePosition = memoryPool.GetBucket<int>(MemoryPoolBucket.StartingYTexturePosition);

            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = wallToX - wallFromX;

            var wall = renderableWall.Wall;
            int textureStart = textureInfo.YOffset << 16;

            (int textureHeight, _, _, float scaledTextureWidth) = CalculateScale(renderableWall.Wall.Sector, wall, textureInfo);

            textureStart = SharedHelpers.EnsureOffsetIsPositive(textureHeight << 16, textureStart);

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int rem = length % Vector<float>.Count;
                wallToX -= rem;

                Vector<float> scaledTextureHeightV = Vector.Create(scaledTextureWidth);
                Vector<int> textureStartV = Vector.Create(textureStart);
                Vector<int> textureWidthV = Vector.Create(textureHeight << 16);


                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> wallStartV = Vector.LoadUnsafe(ref wallStart[x]);
                    Vector<int> wallEndV = Vector.LoadUnsafe(ref wallEnd[x]);
                    Vector<int> wallStartClampedV = Vector.LoadUnsafe(ref wallStartClamped[x]);

                    Vector<int> textureYIncr = Vector.ConvertToInt32Native(scaledTextureHeightV / Vector.ConvertToSingle(wallEndV - wallStartV));

                    Vector<int> textureYPosV;
                    if (fromTop)
                    {
                        textureYPosV = textureStartV + textureYIncr * (wallStartClampedV - wallStartV);
                    }
                    else
                    {
                        Vector<int> portalToV = Vector.LoadUnsafe(ref portalTo[x]);
                        Vector<int> portalToClampedV = Vector.LoadUnsafe(ref portalToClamped[x]);
                        textureYPosV = textureStartV + (portalToClampedV - portalToV) * textureYIncr;
                    }

                    // TODO: WTH
                    textureYPosV = SharedHelpers.EnsureOffsetIsPositive(textureWidthV, textureYPosV);

                    Vector.StoreUnsafe(textureYIncr, ref textureYIncrement[x]);
                    Vector.StoreUnsafe(textureYPosV, ref startingYTexturePosition[x]);
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

            for (int x = wallFromX; x <= wallToX; x++)
            {
                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int clamptedFromY = wallStartClamped[x];

                int textureYIncr = float.ConvertToIntegerNative<int>(scaledTextureWidth / (wallEndY - wallStartY));

                int textureYPosY;

                if (fromTop)
                {
                    textureYPosY = textureStart + textureYIncr * (clamptedFromY - wallStartY);
                }
                else
                {
                    int portalToY = portalTo[x];
                    int portalToClampedY = portalToClamped[x];
                    textureYPosY = textureStart + (portalToClampedY - portalToY) * textureYIncr;
                }

                // TODO: WTH
                // Debug.Assert(textureYPosY >= 0);

                textureYPosY = SharedHelpers.EnsureOffsetIsPositive(textureHeight << 16, textureYPosY);

                textureYIncrement[x] = textureYIncr;
                startingYTexturePosition[x] = textureYPosY;
            }
        }

        private void CalculateTextureDistanceAndXPosition(Span<int> xLocation, RenderablePortalWall renderableWall, TextureInfo textureInfo)
        {
            int width = PixelWidth;

            Span<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);

            RenderableWall wall = renderableWall.Wall;

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = (wallToX - wallFromX);


            int textureHeight = textureInfo.Height;
            int textureWidth = textureInfo.Width;
            float xOffset = textureInfo.XOffset;

            if (textureInfo.XScale is float xScale)
            {
                float wallLength = wall.Length;
                xScale = xScale / wallLength * textureWidth;
            }
            else
            {
                xScale = 1f;
            }

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            bool flipX = textureInfo.RenderingOptions.IsFlippedX;
            flipX = wall.Flipped ? !flipX : flipX;
            float rX = flipX ? wall.R2.X : wall.R1.X;
            float rY = flipX ? wall.R2.Y : wall.R1.Y;

            bool textureWidthEven = SharedHelpers.IsPowerOfTwo(textureWidth);
            int widthMask = textureWidthEven ? textureWidth - 1 : default;

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int rem = length % Vector<float>.Count;
                wallToX -= rem;

                Vector<int> widthMaskV = Vector.Create(widthMask);
                Vector<int> textureHeightV = Vector.Create(textureHeight);

                Vector<float> t1V = Vector.Create(t1);
                Vector<float> d2yV = Vector.Create(d2y);
                Vector<float> d2xV = Vector.Create(d2x);
                Vector<float> rXV = Vector.Create(rX);
                Vector<float> rYV = Vector.Create(rY);
                Vector<float> xScaleV = Vector.Create(xScale);
                Vector<float> xOffsetV = Vector.Create(xOffset);

                Vector<float> cameraRayV = Vector.CreateSequence(cameraRay, cameraWidthIncr);
                Vector<float> cameraWidthIncrV = Vector.Create(cameraWidthIncr * Vector<float>.Count);

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count, cameraRayV += cameraWidthIncrV)
                {
                    (Vector<float> fromToXdist, Vector<float> fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRayV, t1V, d2yV, d2xV);
                    Vector<float> distX = rXV - fromToXdist;
                    Vector<float> distY = rYV - fromToYdist;

                    Vector<float> textureDist = Vector.SquareRoot(distX * distX + distY * distY);
                    Vector<int> topXLocationV = Vector.ConvertToInt32Native(Vector.FusedMultiplyAdd(textureDist, xScaleV, xOffsetV));

                    if (textureWidthEven)
                    {
                        topXLocationV = (topXLocationV & widthMaskV) * textureHeightV;
                        Vector.StoreUnsafe(topXLocationV, ref xLocation[x]);
                    }
                    else
                    {
                        for (int i = 0; i < Vector<float>.Count; i++)
                        {
                            xLocation[x + i] = (topXLocationV[i] % textureWidth) * textureHeight;
                        }
                    }

                    Vector.StoreUnsafe(fromToYdist, ref distance[x]);
                }

                wallFromX = wallToX;
                wallToX += rem;
                cameraRay = cameraRayV[0];
            }

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                (float fromToXdist, float fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                float distX = rX - fromToXdist;
                float distY = rY - fromToYdist;

                float textureDist = MathF.Sqrt(distX * distX + distY * distY);

                xLocation[x] = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, xScale, xOffset));
                xLocation[x] = (xLocation[x] % textureWidth) * textureHeight;

                distance[x] = fromToYdist;
            }
        }

        private void CalculatePortalClamp(RenderablePortalWall renderableWall)
        {
            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);

            Span<int> portalFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFrom);
            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);

            Span<int> portalFromClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFromClamped);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);


            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = (wallToX - wallFromX);

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int rem = length % Vector<float>.Count;
                wallToX -= rem;

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> wallStartClampedV = Vector.LoadUnsafe(ref wallStartClamped[x]);
                    Vector<int> wallEndClampedV = Vector.LoadUnsafe(ref wallEndClamped[x]);

                    Vector<int> portalFromV = Vector.LoadUnsafe(ref portalFrom[x]);
                    Vector<int> portalToV = Vector.LoadUnsafe(ref portalTo[x]);

                    Vector<int> clamptedFromYV = Vector.ClampNative(portalFromV, wallStartClampedV, wallEndClampedV);
                    Vector<int> clamptedToYV = Vector.ClampNative(portalToV, wallStartClampedV, wallEndClampedV);

                    Vector.StoreUnsafe(clamptedFromYV, ref portalFromClamped[x]);
                    Vector.StoreUnsafe(clamptedToYV, ref portalToClamped[x]);
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

            for (int x = wallFromX; x <= wallToX; x++)
            {
                int wallStartClampedY = wallStartClamped[x];
                int wallEndClampedY = wallEndClamped[x];

                int portalFromY = portalFrom[x];
                int portalToY = portalTo[x];
                int clamptedFromY = Math.Clamp(portalFromY, wallStartClampedY, wallEndClampedY);
                int clamptedToY = Math.Clamp(portalToY, wallStartClampedY, wallEndClampedY);

                portalFromClamped[x] = clamptedFromY;
                portalToClamped[x] = clamptedToY;
            }
        }
        private void CalculateWallClamp(RenderablePortalWall renderableWall)
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            Span<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = (wallToX - wallFromX);

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int rem = length % Vector<float>.Count;
                wallToX -= rem;

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> ceilingStartYV = Vector.LoadUnsafe(ref ceilingStart[x]);
                    Vector<int> floorEndYV = Vector.LoadUnsafe(ref floorEnd[x]);

                    Vector<int> wallStartV = Vector.LoadUnsafe(ref wallStart[x]);
                    Vector<int> wallEndV = Vector.LoadUnsafe(ref wallEnd[x]);

                    Vector<int> clamptedFromYV = Vector.ClampNative(wallStartV, ceilingStartYV, floorEndYV);
                    Vector<int> clamptedToYV = Vector.ClampNative(wallEndV, ceilingStartYV, floorEndYV);

                    Vector.StoreUnsafe(clamptedFromYV, ref wallStartClamped[x]);
                    Vector.StoreUnsafe(clamptedToYV, ref wallEndClamped[x]);

                    // if after clamping there is nothing that can be rendered,
                    // set those columns as finished
                    for (int i = 0; i < Vector<int>.Count; i++)
                    {
                        int clamptedFromY = clamptedFromYV[i];
                        int clamptedToY = clamptedToYV[i];

                        if (!status[x + i].WallRenderable || clamptedFromY >= clamptedToY)
                        {
                            status[x + i] = RenderColumnStatus.FinishedRendering;
                        }
                    }
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

            for (int x = wallFromX; x <= wallToX; x++)
            {
                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);

                wallStartClamped[x] = clamptedFromY;
                wallEndClamped[x] = clamptedToY;

                if (!status[x].WallRenderable || clamptedFromY >= clamptedToY)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                }
            }
        }
    }
}
