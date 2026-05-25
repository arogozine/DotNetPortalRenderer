using RenderingEngine.Models;
using RenderingEngine.Models.Rendering;
using RenderingEngine.Tooling;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void DrawSprite(
            PortalPlayerSnapshot player,
            ReadOnlySpan<Sector> sectors,
            RenderableSprite sprite,
            RenderWindowSpriteSnapshot renderableWall)
        {
            switch (sprite)
            {
                case RenderableWallSprite renderableWallSprite:
                    DrawWallSprite(sectors, renderableWallSprite, renderableWall);
                    break;
                case RenderableFloorSprite renderableFloorSprite:
                    DrawFloorSprite(player, sectors, renderableFloorSprite, renderableWall);
                    break;
                case RenderableBasicSprite renderableSprite:
                    DrawSprite(sectors, renderableSprite, renderableWall);
                    break;
            }
        }

        private unsafe void DrawSprite(ReadOnlySpan<Sector> sectors, RenderableBasicSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            uint* textureXLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncrementPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);
            uint* portalFromClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalFromClamped);
            uint* portalToClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalToClamped);
            ushort* repeatedCountPtr = this.memoryPool.GetBucketPtr<ushort>(MemoryPoolBucket.Temp);

            int bufferOffset = PixelWidth * renderableWall.Depth;

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<int> wallStartSpan = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEndSpan = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            TextureInfo texture = sprite.Texture;

            int width = PixelWidth;
            int textureHeight = texture.Height;
            int textureWidth = texture.Width;

            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            Sector sector = sectors[sprite.SectorId];

            float rx1 = sprite.R1.X;

            int spriteFromX = sprite.XLeft;
            int spriteToX = sprite.XRight;

            int spriteStartY = sprite.YLeftCeil;
            int spriteEndY = sprite.YLeftFloor;

            float fromToYDist = sprite.DistanceMin;

            float cameraRay = -1f * EngineConstants.CameraPlaneX + cameraWidthIncr * spriteFromX;

            Debug.Assert(renderableWall.XLeft <= spriteFromX);

            float textureLen = texture.Width / sprite.Length;
            int length = spriteToX - spriteFromX;

            for (int x = spriteFromX; x <= spriteToX; x++, cameraRay += cameraWidthIncr)
            {
                int ceilingStart = wallStartSpan[x];
                int floorEnd = wallEndSpan[x];

                if (floorEnd <= ceilingStart || distance[x] < fromToYDist)
                {
                    repeatedCountPtr[x - spriteFromX] = 0;
                    continue;
                }

                int spriteStartY_Int = float.ConvertToIntegerNative<int>(spriteStartY);
                int spriteEndY_Int = float.ConvertToIntegerNative<int>(spriteEndY);
                int clampedFromY = Math.Clamp(spriteStartY_Int, ceilingStart, floorEnd);
                int clampedToY = Math.Clamp(spriteEndY_Int, ceilingStart, floorEnd);

                if (clampedFromY >= clampedToY)
                {
                    repeatedCountPtr[x - spriteFromX] = 0;
                    continue;
                }

                float distX = MathF.FusedMultiplyAdd(fromToYDist, cameraRay, -rx1);
                int textureXLocation = float.ConvertToIntegerNative<int>(MathF.Abs(distX) * textureLen);

                if (textureXLocation >= textureWidth)
                {
                    repeatedCountPtr[x - spriteFromX] = 0;
                    continue;
                }

                int textureYIncr = (textureHeight << 16) / (spriteEndY_Int - spriteStartY_Int);
                textureXLocation *= textureHeight;

                int textureYPos = (clampedFromY - spriteStartY_Int) * textureYIncr;

                portalFromClampedPtr[x] = (uint)clampedFromY;
                portalToClampedPtr[x] = (uint)clampedToY;

                textureXLocationPtr[x] = (uint)textureXLocation;
                textureYLocationPtr[x] = float.ConvertToIntegerNative<uint>(textureYPos);
                textureYIncrementPtr[x] = float.ConvertToIntegerNative<uint>(textureYIncr);
                repeatedCountPtr[x - spriteFromX] = (ushort)length;
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCountPtr, length + 1);

            DrawSpriteShared(sector, sprite, repeatedCountPtr, spriteFromX, spriteToX, true, texture);
        }

        private unsafe void DrawWallSprite(ReadOnlySpan<Sector> sectors, RenderableWallSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            uint* textureXLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncrementPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);
            uint* portalFromClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalFromClamped);
            uint* portalToClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalToClamped);
            ushort* repeatedCountPtr = this.memoryPool.GetBucketPtr<ushort>(MemoryPoolBucket.Temp);

            int bufferOffset = PixelWidth * renderableWall.Depth;

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            TextureInfo texture = sprite.Texture;

            int width = PixelWidth;
            int textureHeight = texture.Height;
            int textureWidth = texture.Width;

            Sector sector = sectors[sprite.SectorId];

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(sprite, 0);
            float spriteStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float spriteEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            Debug.Assert(renderableWall.XLeft <= spriteFromX);

            int xOffset = 0;

            float xScale = texture.Width / sprite.Length;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(sprite, width, spriteFromX);

            int length = spriteToX - spriteFromX;

            for (int x = spriteFromX; x <= spriteToX; x++, cameraRay += cameraWidthIncr, spriteStartY += ceilDistIncr, spriteEndY += floorDistIncr)
            {
                int ceilingStart = wallStart[x];
                int floorEnd = wallEnd[x];

                if (floorEnd <= ceilingStart)
                {
                    repeatedCountPtr[x - spriteFromX] = 0;
                    continue;
                }

                int spriteStartY_Int = float.ConvertToIntegerNative<int>(spriteStartY);
                int spriteEndY_Int = float.ConvertToIntegerNative<int>(spriteEndY);
                int clampedFromY = Math.Clamp(spriteStartY_Int, ceilingStart, floorEnd);
                int clampedToY = Math.Clamp(spriteEndY_Int, ceilingStart, floorEnd);

                if (clampedFromY >= clampedToY)
                {
                    repeatedCountPtr[x - spriteFromX] = 0;
                    continue;
                }

                (float textureXLocation, float fromToYdist) = MathFormulas.CalculateDistance(sprite, cameraRay, t1, d2y, d2x);

                if ((int)distance[x] < (int)fromToYdist)
                {
                    repeatedCountPtr[x - spriteFromX] = 0;
                    continue;
                }

                textureXLocation = MathF.FusedMultiplyAdd(textureXLocation, xScale, xOffset);

                if (textureXLocation >= textureWidth)
                {
                    repeatedCountPtr[x - spriteFromX] = 0;
                    continue;
                }

                int textureYIncr = (textureHeight << 16) / (spriteEndY_Int - spriteStartY_Int);
                int textureXPos = float.ConvertToIntegerNative<int>(textureXLocation);
                textureXPos *= textureHeight;

                int textureYPos = (clampedFromY - spriteStartY_Int) * textureYIncr;

                portalFromClampedPtr[x] = (uint)clampedFromY;
                portalToClampedPtr[x] = (uint)clampedToY;

                textureXLocationPtr[x] = (uint)textureXPos;
                textureYLocationPtr[x] = float.ConvertToIntegerNative<uint>(textureYPos);
                textureYIncrementPtr[x] = float.ConvertToIntegerNative<uint>(textureYIncr);
                repeatedCountPtr[x - spriteFromX] = (ushort)length;
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCountPtr, length + 1);

            DrawSpriteShared(sector, sprite, repeatedCountPtr, spriteFromX, spriteToX, false, texture);
        }

        private unsafe void DrawSpriteShared(
            Sector sector,
            IWallLike sprite,
            ushort* repeatedCount,
            int spriteFromX, int spriteToX,
            bool renderHorizontally,
            TextureInfo texture
            )
        {
            uint* textureXLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncrementPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);
            uint* portalFromClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalFromClamped);
            uint* portalToClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalToClamped);

            int textureHeight = texture.Height;

            uint* screenPtr = (uint*)buffer;
            uint width = (uint)PixelWidth;

            bool isPowerOfTwo = SharedHelpers.IsPowerOfTwo(textureHeight);
            float alpha = Math.Clamp(texture.Alpha, 0f, 1f);

            fixed (uint* texturePtr = &GetTextureRef())
            {
                if (renderHorizontally)
                {
                    if (alpha == 1f)
                    {
                        DrawHorizontally(new DrawTransparentPixel(), texturePtr);
                    }
                    else
                    {
                        DrawHorizontally(new DrawAlphaPixel(), texturePtr);
                    }
                }
                else
                {
                    if (alpha == 1f)
                    {
                        Draw(new DrawTransparentPixel(), texturePtr);
                    }
                    else
                    {
                        Draw(new DrawAlphaPixel(), texturePtr);
                    }
                }
            }

            return;

            void DrawHorizontally<T>(T drawPixel, uint* texturePtr)
                where T : IDrawPixel, allows ref struct
            {
                for (int x = spriteFromX; x <= spriteToX;)
                {
                    ushort count = repeatedCount[x - spriteFromX];

                    if (count == 0)
                    {
                        x++;
                        continue;
                    }

                    uint* clampedFromY = portalFromClampedPtr + x;
                    uint* clampedToY = portalToClampedPtr + x;
                    uint* textureXPos = textureXLocationPtr + x;
                    uint* textureYPos = textureYLocationPtr + x;
                    uint* textureYIncr = textureYIncrementPtr + x;

                    (uint min_t, uint max_t) = GetMinMaxValue(clampedFromY, count);
                    (uint min_b, uint max_b) = GetMinMaxValue(clampedToY, count);

                    RenderMultipleHorizontalLines(drawPixel, count, width, (uint)x, clampedFromY, clampedToY, min_t, max_t, min_b, max_b, textureYPos, textureYIncr, screenPtr, textureXPos, texturePtr);

                    x += count;
                }
            }

            void Draw<T>(T drawPixel, uint* texturePtr)
                where T : IDrawPixel, allows ref struct
            {
                for (int x = spriteFromX; x <= spriteToX;)
                {
                    ushort count = repeatedCount[x - spriteFromX];

                    if (count == 0)
                    {
                        x++;
                        continue;
                    }

                    uint* clampedFromY = portalFromClampedPtr + x;
                    uint* clampedToY = portalToClampedPtr + x;
                    uint* textureXPos = textureXLocationPtr + x;
                    uint* textureYPos = textureYLocationPtr + x;
                    uint* textureYIncr = textureYIncrementPtr + x;

                    Debug.Assert(*clampedFromY < *clampedToY);

                    if (Vector256.IsHardwareAccelerated && count >= Vector256<uint>.Count)
                    {
                        RenderMultipleWallLinesV256(
                            drawPixel,
                            isPowerOfTwo,
                            width,
                            (uint)x,
                            textureHeight,
                            clampedFromY,
                            clampedToY,
                            textureYPos,
                            textureYIncr,
                            screenPtr,
                            textureXPos,
                            texturePtr
                        );

                        x += Vector256<uint>.Count;
                        continue;
                    }

                    if (Vector128.IsHardwareAccelerated && count >= Vector128<uint>.Count)
                    {
                        RenderMultipleWallLinesV128(
                            drawPixel,
                            isPowerOfTwo,
                            width,
                            (uint)x,
                            textureHeight,
                            clampedFromY,
                            clampedToY,
                            textureYPos,
                            textureYIncr,
                            screenPtr,
                            textureXPos,
                            texturePtr
                        );

                        x += Vector128<uint>.Count;
                        continue;
                    }

                    RenderMultipleWallLines(
                        drawPixel,
                        isPowerOfTwo,
                        count,
                        width,
                        (uint)x,
                        textureHeight,
                        clampedFromY,
                        clampedToY,
                        textureYPos,
                        textureYIncr,
                        screenPtr,
                        textureXPos,
                        texturePtr
                    );

                    x += count;
                }

            }

            ref uint GetTextureRef()
            {
                bool flipY = texture.RenderingOptions.IsFlippedY;
                bool flipX = texture.RenderingOptions.IsFlippedX;

                var transform = TextureTransform.Rotated;

                if (flipY)
                {
                    transform |= TextureTransform.FlippedY;
                }

                if (flipX)
                {
                    transform |= TextureTransform.FlippedX;
                }

                if (sprite.Flipped)
                {
                    transform ^= TextureTransform.FlippedX;
                }

                return ref texture.Texture.GetBinaryRef<uint>(sprite.Shade ?? sector.FloorShade,
                    transform);
            }
        }

        #region Render Wall with Blend Mode

        private static unsafe void RenderMultipleWallLinesV256<T>(
            T drawPixel,
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
            where T : IDrawPixel, allows ref struct
        {
            Vector256<uint> startYV = Vector256.Load(startY);
            Vector256<uint> endYV = Vector256.Load(endY);
            Vector256<uint> textureXIncr_uV = Vector256.Load(textureYIncr_u);

            (uint min_t, uint max_t) = GetMinMaxValue(startYV);
            (uint min_b, uint max_b) = GetMinMaxValue(endYV);

            if (min_b <= max_t)
            {
                for (int i = 0; i < Vector256<uint>.Count; i++, x++)
                {
                    uint* textureYPos = textureYPos_u + i;
                    uint incr = textureXIncr_uV[i];

                    uint top = startYV[i];
                    uint bottom = endYV[i];

                    RenderWallColumn(drawPixel, isPowerOfTwo, width, x, textureHeight, top, bottom, *textureYPos, incr, screenPtr,
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

                    if (top >= max_t)
                    {
                        continue;
                    }

                    uint* textureYPos = textureYPos_u + i;
                    uint incr = textureXIncr_uV[i];
                    uint xi = x + (uint)i;

                    *textureYPos = RenderWallColumn2(drawPixel, isPowerOfTwo, width, xi, textureHeight, top, max_t, *textureYPos, incr, screenPtr,
                        textureBuffer + *(texturePos + i)
                    );
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

                            drawPixel.DrawLine(screenIndexPtr, gathered);

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
                                uint pixel = *(textureBuffer + texelIndexV[i]);

                                drawPixel.Draw(screenIndexPtr, pixel);

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
                            uint pixel = *(textureBuffer + textureXPosV[i] + (texelIndexV[i] % textureHeightMask));

                            drawPixel.Draw(screenIndexPtr, pixel);

                            screenIndexPtr++;
                        }

                        textureYPos_uV += textureXIncr_uV;
                        screenIndexPtr += widthMinusLanes;
                    }
                }
            }

            // render bottoms where there is no shared window
            if (min_b == max_b)
            {
                return;
            }

            for (int i = 0; i < Vector256<uint>.Count; i++)
            {
                uint bottom = endYV[i];

                if (bottom <= min_b)
                {
                    continue;
                }

                uint textureYPos = textureYPos_uV[i];
                uint incr = textureXIncr_uV[i];

                uint xi = x + (uint)i;

                RenderWallColumn(drawPixel, isPowerOfTwo, width, xi, textureHeight, min_b, bottom, textureYPos, incr, screenPtr,
                    textureBuffer + *(texturePos + i)
                );
            }
        }

        private static unsafe void RenderMultipleWallLinesV128<T>(
            T drawPixel,
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
            where T : IDrawPixel, allows ref struct
        {
            var startYV = Vector128.Load(startY);
            var endYV = Vector128.Load(endY);
            var textureXIncr_uV = Vector128.Load(textureYIncr_u);

            (uint min_t, uint max_t) = GetMinMaxValue(startYV);
            (uint min_b, uint max_b) = GetMinMaxValue(endYV);

            if (min_b <= max_t)
            {
                for (int i = 0; i < Vector128<uint>.Count; i++, x++)
                {
                    uint* textureYPos = textureYPos_u + i;
                    uint incr = textureXIncr_uV[i];

                    uint top = startYV[i];
                    uint bottom = endYV[i];

                    RenderWallColumn(drawPixel, isPowerOfTwo, width, x, textureHeight, top, bottom, *textureYPos, incr, screenPtr,
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

                    if (top >= max_t)
                    {
                        continue;
                    }

                    uint* textureYPos = textureYPos_u + i;
                    uint incr = textureXIncr_uV[i];
                    uint xi = x + (uint)i;

                    *textureYPos = RenderWallColumn2(drawPixel, isPowerOfTwo, width, xi, textureHeight, top, max_t, *textureYPos, incr, screenPtr,
                        textureBuffer + *(texturePos + i));
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

                    if (Avx2.IsSupported)
                    {
                        while (screenIndexPtr < screenIndexPtrEnd)
                        {
                            Vector128<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                            texelIndexV += textureXPosV;

                            Vector128<uint> gathered = Avx2.GatherVector128(
                                textureBuffer,
                                texelIndexV.AsInt32(),
                                scale: sizeof(uint)
                            );

                            drawPixel.DrawLine(screenIndexPtr, gathered);

                            textureYPos_uV += textureXIncr_uV;
                            screenIndexPtr += width;
                        }
                    }
                    else
                    {
                        while (screenIndexPtr < screenIndexPtrEnd)
                        {
                            Vector128<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                            texelIndexV += textureXPosV;

                            for (int i = 0; i < Vector128<uint>.Count; i++)
                            {
                                uint pixel = *(textureBuffer + texelIndexV[i]);

                                drawPixel.Draw(screenIndexPtr, pixel);

                                screenIndexPtr++;
                            }

                            textureYPos_uV += textureXIncr_uV;
                            screenIndexPtr += widthMinusLanes;
                        }
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
                            uint pixel = *(textureBuffer + textureXPosV[i] + (texelIndexV[i] % textureMask));

                            drawPixel.Draw(screenIndexPtr, pixel);

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

                        RenderWallColumn(drawPixel, isPowerOfTwo, width, xi, textureHeight, min_b, bottom, textureXPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i));
                    }
                }
            }
        }

        private static unsafe void RenderMultipleHorizontalLines<T>(
            T drawPixel,
            uint count,
            uint width,
            uint x,
            uint* startY,
            uint* endY,
            uint min_t, uint max_t,
            uint min_b, uint max_b,
            uint* textureYPos_u,
            uint* textureYIncr_u,
            uint* screenPtr,
            uint* texturePos,
            uint* textureBuffer
            )
            where T : IDrawPixel, allows ref struct
        {
            RenderTops(drawPixel);
            RenderHorizontal(drawPixel);
            RenderBottoms(drawPixel);

            return;

            void RenderHorizontal(T drawPixel)
            {
                uint* screenIndexPtr = screenPtr + max_t * width + x;
                uint* screenIndexPtrEnd = screenPtr + min_b * width + x;

                while (screenIndexPtr < screenIndexPtrEnd)
                {
                    int i = 0;
                    uint rem = count & ((uint)Vector<uint>.Count - 1U);
                    count -= rem;

                    for (; i < count; i += Vector<uint>.Count)
                    {
                        uint* textureYPos = textureYPos_u + i;

                        Vector<uint> textureYPosV = Vector.Load(textureYPos);
                        Vector<uint> texturePosV = Vector.Load(texturePos + i);
                        Vector<uint> texelIndexV = (textureYPosV >> 16) + texturePosV;

                        if (Avx2.IsSupported && Vector<uint>.Count == Vector256<uint>.Count)
                        {
                            Vector256<uint> gathered = Avx2.GatherVector256(
                                textureBuffer,
                                texelIndexV.AsVector256().AsInt32(),
                                scale: sizeof(uint)
                            );

                            drawPixel.DrawLine(screenIndexPtr, gathered);
                        }
                        else
                        {
                            for (int j = 0; j < Vector<uint>.Count; j++)
                            {
                                uint pixel = *(textureBuffer + texelIndexV[j]);
                                drawPixel.Draw(screenIndexPtr + j, pixel);
                            }
                        }

                        textureYPosV += Vector.Load(textureYIncr_u + i);
                        textureYPosV.Store(textureYPos);
                        screenIndexPtr += Vector<uint>.Count;
                    }

                    count += rem;

                    for (; i < count; i++)
                    {
                        uint* textureYPos = textureYPos_u + i;
                        uint texelIndex = (*textureYPos >> 16);
                        texelIndex += *(texturePos + i);

                        uint pixel = *(textureBuffer + texelIndex);

                        drawPixel.Draw(screenIndexPtr, pixel);

                        *textureYPos += *(textureYIncr_u + i);
                        screenIndexPtr++;
                    }

                    screenIndexPtr += width - count;
                }
            }

            void RenderTops(T drawPixel)
            {
                uint* screenIndexPtr = screenPtr + min_t * width + x;

                for (uint y = min_t; y < max_t; y++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        uint start = *(startY + i);

                        if (y <= start)
                            continue;

                        uint* textureXPos = textureYPos_u + i;
                        uint texelIndex = (*textureXPos >> 16);
                        texelIndex += *(texturePos + i);

                        uint pixel = *(textureBuffer + texelIndex);

                        drawPixel.Draw((screenIndexPtr + i), pixel);

                        *textureXPos += *(textureYIncr_u + i);
                    }

                    screenIndexPtr += width;
                }
            }

            void RenderBottoms(T drawPixel)
            {
                uint* screenIndexPtr = screenPtr + min_b * width + x;

                for (uint y = min_b; y < max_b; y++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        uint end = *(endY + i);

                        if (end <= y)
                            continue;

                        uint* textureXPos = textureYPos_u + i;
                        uint texelIndex = (*textureXPos >> 16);
                        texelIndex += *(texturePos + i);

                        uint pixel = *(textureBuffer + texelIndex);

                        drawPixel.Draw((screenIndexPtr + i), pixel);

                        *textureXPos += *(textureYIncr_u + i);
                    }

                    screenIndexPtr += width;
                }
            }
        }

        private static unsafe void RenderMultipleWallLines<T>(
            T drawPixel,
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
            where T : IDrawPixel, allows ref struct
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
                for (uint i = 0; i < count; i++, x++)
                {
                    uint textureYPos = *(textureYPos_u + i);
                    uint incr = *(textureYIncr_u + i);
                    uint top = *(startY + i);
                    uint bottom = *(endY + i);

                    RenderWallColumn(drawPixel, isPowerOfTwo, width, x, textureHeight, top, bottom, textureYPos, incr, screenPtr,
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

                    if (top >= max_t)
                    {
                        continue;
                    }

                    uint* textureYPos = textureYPos_u + i;
                    uint incr = *(textureYIncr_u + i);

                    uint xi = x + i;

                    *textureYPos = RenderWallColumn2(drawPixel, isPowerOfTwo, width, xi, textureHeight, top, max_t, *textureYPos, incr, screenPtr,
                        textureBuffer + *(texturePos + i));
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

                            drawPixel.Draw(screenIndexPtr, pixel);

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

                            drawPixel.Draw(screenIndexPtr, pixel);

                            *textureYPos += *(textureYIncr_u + i);
                            screenIndexPtr++;
                        }

                        screenIndexPtr += width - count;
                    }
                }
            }

            // render bottoms of each line where there is no shared window
            if (min_b >= max_b)
            {
                return;
            }

            for (uint i = 0; i < count; i++, x++)
            {
                uint bottom = *(endY + i);

                if (bottom <= min_b)
                {
                    continue;
                }

                uint textureYPos = *(textureYPos_u + i);
                uint incr = *(textureYIncr_u + i);

                RenderWallColumn(drawPixel, isPowerOfTwo, width, x, textureHeight, min_b, bottom, textureYPos, incr, screenPtr,
                    textureBuffer + *(texturePos + i));
            }
        }

        private static unsafe uint RenderWallColumn2<T>(
            T drawPixel,
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
            where T : IDrawPixel, allows ref struct
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

                    drawPixel.Draw(screenIndexPtr, pixel);

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

                    drawPixel.Draw(screenIndexPtr, pixel);

                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }

            return textureYPos_u;
        }

        private static unsafe void RenderWallColumn<T>(
            T drawPixel,
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
            where T : IDrawPixel, allows ref struct
        {
            Debug.Assert(endY >= startY);
            uint* screenIndexPtr = screenPtr + startY * width + x;
            uint* screenIndexPtrEnd = screenPtr + endY * width + x;

            if (isPowerOfTwo)
            {
                uint textureHeightMask = (uint)(textureHeight - 1);

                while (screenIndexPtr < screenIndexPtrEnd)
                {
                    uint texelIndex = (textureYPos_u >> 16) & textureHeightMask;
                    uint pixel = *(textureBuffer + texelIndex);

                    drawPixel.Draw(screenIndexPtr, pixel);

                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }
            else
            {
                uint textureHeightMask = (uint)textureHeight;

                while (screenIndexPtr < screenIndexPtrEnd)
                {
                    uint texelIndex = (textureYPos_u >> 16) % textureHeightMask;
                    uint pixel = *(textureBuffer + texelIndex);

                    drawPixel.Draw(screenIndexPtr, pixel);

                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }
        }

        #endregion
    }
}
