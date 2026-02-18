using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ref T GetScreenPtr<T>()
            where T : unmanaged
        {
            return ref Unsafe.AsRef<T>(buffer);
        }

        private bool DrawBasicWall(
            PortalPlayerSnapshot player,
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            // separate path for skybox rendering
            RenderableWall wall = renderableWall.Wall;
            TextureInfo textureInfo = wall.MiddleTexture!;

            (bool skybox, _, bool flipY) = GetFlags(textureInfo);

            if (skybox)
            {
                return DrawBasicSkyboxWall(player, renderableWall);
            }

            // Precalculate render window and texture positions
            PrecalculateBasicWallDistance(renderableWall, sector);

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ref uint wallTexturePtr = ref textureInfo.Texture.GetBinaryRef<uint>(true, wall.Shade);
            int textureWidth = textureInfo.Height;

            using TempBuffer<uint> buffer = TempBuffer<uint>.GetBuffer(textureInfo.Height);
            ref RenderColumnStatus statusRef = ref memoryPool.GetBucketRef<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<uint> textureXLocation = memoryPool.GetBucket<uint>(MemoryPoolBucket.TopTextureXLocation);
            ref uint textureXLocationRef = ref textureXLocation[0];
            ref uint textureYLocationRef = ref memoryPool.GetBucketRef<uint>(MemoryPoolBucket.TopTextureYLocation);
            ref uint clampedFromRef = ref memoryPool.GetBucketRef<uint>(MemoryPoolBucket.ClampedFrom);
            ref uint clampedToRef = ref memoryPool.GetBucketRef<uint>(MemoryPoolBucket.ClampedTo);
            ref uint textureXPosRef = ref memoryPool.GetBucketRef<uint>(MemoryPoolBucket.TextureXPos);

            ushort length = (ushort)(wallToX - wallFromX + 1);
            Span<ushort> repeatedCount = TempBuffer<ushort>.GetBuffer(length);
            Span<ushort> repeatedCountB = TempBuffer<ushort>.GetBuffer(length);
            repeatedCount.Fill(length);

            // Set the X position as "FinishedRendering" for this wall
            // and determines where there repeat count is 0 (nothing to draw)
            for (int x = wallFromX; x <= wallToX; x++)
            {
                ref RenderColumnStatus columnStatus = ref Unsafe.Add(ref statusRef, x);

                if (!columnStatus.WallRenderable)
                {
                    repeatedCount[x - wallFromX] = 0;
                }

                columnStatus = RenderColumnStatus.FinishedRendering;
            }

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

                ref uint clamptedFromY = ref Unsafe.Add(ref clampedFromRef, x);
                ref uint clamptedToY = ref Unsafe.Add(ref clampedToRef, x);
                ref uint textureXIncr = ref Unsafe.Add(ref textureYLocationRef, x);
                ref uint textureXPos = ref Unsafe.Add(ref textureXPosRef, x);
                ref uint textureYPos = ref Unsafe.Add(ref textureXLocationRef, x);
                CalculateAndCacheWallColumn(buffer, ref wallTexturePtr, (int)textureYPos, flipY);

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
                            ref textureXPos,
                            ref textureXIncr,
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
                            ref textureXPos,
                            ref textureXIncr,
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
                            ref textureXPos,
                            ref textureXIncr,
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
                    textureXPos,
                    textureXIncr,
                    ref screenPtr,
                    ref buffer.Pointer);
            }

            return true;
        }

        private bool DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall)
        {
            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<float> distance = RenderWindowHelper.Distance;

            int width = PixelWidth;
            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            TextureInfo wallTexture = wall.MiddleTexture!;
            ref uint wallTextureUintPtr = ref wallTexture.Texture.GetBinaryRef<uint>(false, 0);
            ref float angleCachePtr = ref memoryPool.GetBucketRef<float>(MemoryPoolBucket.AngleCache);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    distance[x] = MathFormulas.CalculateDistance2(cameraRay, t1, d2y, d2x);
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                (int clamptedFromY, int clamptedToY) = RenderWindowHelper.GetClampedWallFromTo(x);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, clamptedToY * width + x);

                float fromToYdist = MathFormulas.CalculateDistance2(cameraRay, t1, d2y, d2x);

                RenderSkyboxLine(player,
                    x,
                    wallTexture,
                    ref wallTextureUintPtr,
                    ref angleCachePtr,
                    ref screenIndexPtr,
                    ref screenIndexPtrEnd);

                distance[x] = fromToYdist;
                status[x] = RenderColumnStatus.FinishedRendering;
            }

            return true;
        }

        #region Render Line

        private void RenderSkyboxLine(PortalPlayerSnapshot player,
            int x,
            TextureInfo upperTexture,
            ref uint upperTextureUintPtr,
            ref float angleCachePtr,
            ref uint screenIndexPtr,
            ref readonly uint screenIndexPtrEnd)
        {
            const float oneOverTwoPi = 1f / (2 * MathF.PI);

            int width = PixelWidth;
            int height = PixelHeight;

            float viewAngle = player.Angle;

            int textureWidth = upperTexture.Width;
            int textureHeight = upperTexture.Height;

            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;
            float yTextureIncr = (1f / height) * textureHeight;

            // calculate angle between 0 to 2 PI
            float angleX = Unsafe.Add(ref angleCachePtr, x) - viewAngle;
            angleX = MathFormulas.ClampAngle(angleX);

            int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) % textureWidth;

            int wallStart = RenderWindowHelper.WallStart[x];
            int ceilingStart = RenderWindowHelper.CeilingStart[x];
            int floorEnd = RenderWindowHelper.FloorEnd[x];
            int fromYClamped = Math.Clamp(wallStart, ceilingStart, floorEnd);
            float vScreen = fromYClamped * yTextureIncr;

            ref uint textureColumnPtr = ref Unsafe.Add(ref upperTextureUintPtr, texX);

            for (;
                    Unsafe.IsAddressGreaterThan(in screenIndexPtrEnd, in screenIndexPtr);
                    vScreen += yTextureIncr, screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width)
                )
            {
                int index = textureWidth * float.ConvertToIntegerNative<int>(vScreen);
                screenIndexPtr = Unsafe.Add(ref textureColumnPtr, index);
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

        private static void RenderMultipleWallLinesT(
            uint width,
            uint x,
            int textureHeight,
            Vector<uint> startYV,
            Vector<uint> endYV,
            Vector<uint> textureXPos_uV,
            Vector<uint> textureXIncr_uV,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
            )
        {
            // cache lane count
            uint min_t = uint.MaxValue, max_t = 0;
            uint min_b = uint.MaxValue, max_b = 0;

            // compute per-lane tops/bottoms
            for (int i = 0; i < Vector<uint>.Count; i++)
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
                var max_tv = Vector.Create(max_t);
                textureXPos_uV += textureXIncr_uV * (max_tv - startYV);

                for (int i = 0; i < Vector<uint>.Count; i++)
                {
                    uint top = startYV[i];

                    if (top < max_t)
                    {
                        uint incr = textureXIncr_uV[i];
                        uint topTexturePosition = textureXPos_uV[i];
                        uint xi = x + (uint)i;
                        RenderWallLine2(width, xi, textureHeight, top, max_t, topTexturePosition, incr, ref screenPtr, ref textureBuffer);
                    }
                }
            }

            // render bottoms where there is no shared window
            if (min_b != max_b)
            {
                for (int i = 0; i < Vector<uint>.Count; i++)
                {
                    uint bottom = endYV[i];

                    if (bottom > min_b)
                    {
                        uint textureXPos = textureXPos_uV[i];
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
            //Vector<uint> textureXPos_uV = Vector.LoadUnsafe(ref textureXPos_u);
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, (int)(max_t * width + x));
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, (int)(min_b * width + x));
            Vector<uint> textureMaskV = Vector.Create((uint)(textureHeight - 1));

            // cache base ref for texture buffer and precompute step
            ref uint textureBufferRef = ref textureBuffer;
            uint widthMinusLanes = width - (uint)Vector<uint>.Count;

            // go down the column set
            while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
            {
                Vector<uint> texelIndexV = (textureXPos_uV >> 16) & textureMaskV;

                // horizontally draw the texture (keeps per-lane behavior but with cached refs)
                for (int i = 0; i < Vector<uint>.Count; i++)
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

        #endregion

        #region Pre Calculate

        private void PrecalculateBasicWallDistance(RenderablePortalWall renderableWall, Sector sector)
        {
            RenderableWall wall = renderableWall.Wall;

            PrecalculateWallDistanceShared2(renderableWall, sector, wall.MiddleTexture!, memoryPool.GetBucket<int>(MemoryPoolBucket.TopTextureXLocation), memoryPool.GetBucket<int>(MemoryPoolBucket.TopTextureYLocation));
        }

        private void PrecalculateWallDistanceShared2(
            RenderablePortalWall renderableWall, Sector sector, TextureInfo textureInfo,
            scoped Span<int> xLocation, scoped Span<int> yLocation)
        {
            Span<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);
            Span<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            Span<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);

            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            Span<int> clampedFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.ClampedFrom);
            Span<int> clampedTo = memoryPool.GetBucket<int>(MemoryPoolBucket.ClampedTo);
            Span<int> textureXPos = memoryPool.GetBucket<int>(MemoryPoolBucket.TextureXPos);

            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;
            bool flipX = textureInfo.RenderingOptions.IsFlippedX;
            flipX = wall.Flipped ? !flipX : flipX;
            float xOffset = textureInfo.XOffset;

            (int textureWidth, int textureHeight, float xScale, float scaledTextureWidth) = CalculateScale(sector, wall, textureInfo);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            float rX = flipX ? wall.R2.X : wall.R1.X;
            float rY = flipX ? wall.R2.Y : wall.R1.Y;

            int length = (wallToX - wallFromX);
            int textureStart = textureInfo.YOffset << 16;

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                Vector<int> canRenderWallMaskV = Vector.Create((int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall));

                Span<int> statusInt = MemoryMarshal.Cast<RenderColumnStatus, int>(status);

                // Vector<int> widthV = Vector.Create(width);
                Vector<float> t1V = Vector.Create(t1);
                Vector<float> d2yV = Vector.Create(d2y);
                Vector<float> d2xV = Vector.Create(d2x);
                Vector<float> rXV = Vector.Create(rX);
                Vector<float> rYV = Vector.Create(rY);

                Vector<float> xScaleV = Vector.Create(xScale);
                Vector<float> xOffsetV = Vector.Create(xOffset);
                Vector<float> scaledTextureWidthV = Vector.Create(scaledTextureWidth);
                Vector<int> textureWidthV = Vector.Create(textureWidth);

                Vector<int> textureStartV = Vector.Create(textureStart);
                Vector<int> finishedRendering = Vector.Create((int)RenderColumnStatus.FinishedRendering);

                Vector<float> cameraRayV = Vector.CreateSequence(cameraRay, cameraWidthIncr);
                Vector<float> cameraWidthIncrV = Vector.Create(cameraWidthIncr * Vector<float>.Count);

                int rem = (wallToX - wallFromX) % Vector<float>.Count;
                wallToX -= rem;

                bool textureHeightEven = SharedHelpers.IsPowerOfTwo(textureHeight);
                Vector<int> heightMask = textureHeightEven ? Vector.Create(textureHeight - 1) : default;

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> columnStatusV = Vector.LoadUnsafe(ref statusInt[x]) & canRenderWallMaskV;

                    if (columnStatusV == Vector<int>.Zero)
                    {
                        // walls can't be rendered, exit loop
                        Vector.StoreUnsafe(finishedRendering, ref statusInt[x]);
                        continue;
                    }

                    (Vector<float> fromToXdist, Vector<float> fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRayV, t1V, d2yV, d2xV);
                    Vector<float> distX = rXV - fromToXdist;
                    Vector<float> distY = rYV - fromToYdist;

                    Vector<int> wallStartV = Vector.LoadUnsafe(ref wallStart[x]);
                    Vector<int> wallEndV = Vector.LoadUnsafe(ref wallEnd[x]);

                    Vector<float> textureDist = Vector.SquareRoot(distX * distX + distY * distY);
                    Vector<int> topXLocationV = Vector.ConvertToInt32Native(Vector.FusedMultiplyAdd(textureDist, xScaleV, xOffsetV));
                    Vector<int> topYLocationV = Vector.ConvertToInt32Native(scaledTextureWidthV / Vector.ConvertToSingle(wallEndV - wallStartV));

                    Vector.StoreUnsafe(fromToYdist, ref distance[x]);
                    Vector.StoreUnsafe(topYLocationV, ref yLocation[x]);

                    if (textureHeightEven)
                    {
                        topXLocationV = (topXLocationV & heightMask) * textureWidthV;
                        Vector.StoreUnsafe(topXLocationV, ref xLocation[x]);
                    }
                    else
                    {
                        for (int i = 0; i < Vector<float>.Count; i++)
                        {
                            xLocation[x + i] = (topXLocationV[i] % textureHeight) * textureWidth;
                        }
                    }

                    Vector<int> ceilingStartYV = Vector.LoadUnsafe(ref ceilingStart[x]);
                    Vector<int> floorEndYV = Vector.LoadUnsafe(ref floorEnd[x]);

                    Vector<int> clamptedFromYV = Vector.Clamp(wallStartV, ceilingStartYV, floorEndYV);
                    Vector<int> clamptedToYV = Vector.Clamp(wallEndV, ceilingStartYV, floorEndYV);
                    Vector<int> textureXPosV = textureStartV - topYLocationV * (wallStartV - clamptedFromYV);
                    textureXPosV = SharedHelpers.EnsureOffsetIsPositive(textureWidthV << 16, textureXPosV);

                    // clamptedFromYV *= widthV;
                    // clamptedToYV *= widthV;

                    Vector.StoreUnsafe(clamptedFromYV, ref clampedFrom[x]);
                    Vector.StoreUnsafe(clamptedToYV, ref clampedTo[x]);
                    Vector.StoreUnsafe(textureXPosV, ref textureXPos[x]);

                    // if after clamping there is nothing that can be rendered,
                    // set those columns as finished
                    for (int i = 0; i < Vector<int>.Count; i++)
                    {
                        int clamptedFromY = clamptedFromYV[i];
                        int clamptedToY = clamptedToYV[i];
                        if (clamptedFromY >= clamptedToY)
                        {
                            statusInt[x + i] = (int)RenderColumnStatus.FinishedRendering;
                        }
                    }

                    cameraRayV += cameraWidthIncrV;
                }

                wallFromX = wallToX;
                wallToX += rem;
                cameraRay = cameraRayV[0];
            }

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                (float fromToXdist, float fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];

                float distX = rX - fromToXdist;
                float distY = rY - fromToYdist;
                float textureDist = MathF.Sqrt(distX * distX + distY * distY);
                int textureXIncr = float.ConvertToIntegerNative<int>(scaledTextureWidth / (wallEndY - wallStartY));

                distance[x] = fromToYdist;
                xLocation[x] = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, xScale, xOffset));
                yLocation[x] = textureXIncr;
                xLocation[x] = (xLocation[x] % textureHeight) * textureWidth;

                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);
                int textureXPosY = textureStart - textureXIncr * (wallStartY - clamptedFromY);
                textureXPosY = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPosY);

                clampedFrom[x] = clamptedFromY;
                clampedTo[x] = clamptedToY;
                textureXPos[x] = textureXPosY;

                if (clamptedFromY >= clamptedToY)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                }
            }
        }


        private static (int Width, int Height, float XScale, float ScaledTextureWidth) CalculateScale(
            Sector sector,
            RenderableWall wall,
            TextureInfo wallTexture)
        {
            int textureWidth = wallTexture.Height;
            int textureHeight = wallTexture.Width;

            if (wallTexture.XScale is float xScale)
            {
                float wallLength = wall.Length;
                xScale = xScale / wallLength * textureHeight;
            }
            else
            {
                xScale = 1f;
            }

            float scaledTextureWidth;

            if (wallTexture.YScale is float yScale)
            {
                yScale = (sector.Ceil - sector.Floor) * yScale;
                scaledTextureWidth = ((textureWidth << 16) * yScale);
            }
            else
            {
                scaledTextureWidth = (sector.Ceil - sector.Floor) << 16;
            }

            return (textureWidth, textureHeight, xScale, scaledTextureWidth);
        }

        #endregion

        #region Calculation Helpers

        private static (int SectorHeight, int CeilingOffset, int FloorOffset) CalculatePortalOffsets(ReadOnlySpan<Sector> sectors, RenderableWall wall)
        {
            Sector sector = wall.Sector;
            Sector neighborSector = sectors[wall.Neighbor];
            int sectorHeight = sector.Ceil - sector.Floor;
            int floorOffset = neighborSector.Floor - sector.Floor;
            int ceilOffset = neighborSector.Ceil - sector.Ceil;

            if (floorOffset < 0)
            {
                floorOffset = 0;
            }

            if (ceilOffset > 0)
            {
                ceilOffset = 0;
            }

            // don't draw beyond the bounds
            if (ceilOffset < -sectorHeight)
            {
                ceilOffset = -sectorHeight;
            }

            if (floorOffset > sectorHeight)
            {
                floorOffset = sectorHeight;
            }

            return (sectorHeight, ceilOffset, floorOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateAndCacheWallColumn(
            TempBuffer<uint> tempBuffer,
            scoped ref uint wallTexturePtr,
            int textureYPos, bool flipY)
        {
            // reuse the cached column
            if (tempBuffer.Index == textureYPos)
            {
                return;
            }

            tempBuffer.Index = textureYPos;

            Span<uint> buffer = tempBuffer.Span;
            ref uint columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);

            if (flipY)
            {
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    buffer[i] = columnPtr;
                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = columnPtr;
                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool IsSkybox, bool FlipX, bool FlipY) GetFlags(TextureInfo textureInfo)
        {
            if (textureInfo is null)
            {
                return (false, false, false);
            }

            TextureRenderingOptions options = textureInfo.RenderingOptions;

            bool skyBox = options.HasFlag(TextureRenderingOptions.Skybox);
            bool flipX = options.HasFlag(TextureRenderingOptions.FlipX);
            bool flipY = options.HasFlag(TextureRenderingOptions.FlipY);

            return (skyBox, flipX, flipY);
        }

        #endregion
    }
}