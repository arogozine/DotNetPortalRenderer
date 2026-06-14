using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private readonly DynamicAlignedMemoryPool spriteCacheMemoryPool;

        private void FillDepthZero()
        {
            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[..PixelWidth];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[..PixelWidth];
            Span<RenderColumnStatus> renderStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[..PixelWidth];

            wallStart.Clear();
            wallEnd.Fill(PixelHeight - 1);
            renderStatus.Fill(RenderColumnStatus.NewRender);
        }

        private void CacheDepthAndStartEndBoundsForSpriteRendering(int depth)
        {
            int offset = depth * PixelWidth;

            ResizeCacheIfNeeded();

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[offset..];
            memoryPool.GetBucket<float>(MemoryPoolBucket.Distance).CopyTo(distance);

            Span<RenderColumnStatus> renderStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[offset..];
            memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus).CopyTo(renderStatus);

            offset += PixelWidth;

            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[offset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[offset..];

            memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped).CopyTo(wallStart);
            memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped).CopyTo(wallEnd);

            Clamp(wallStart, wallEnd);

            return;

            void ResizeCacheIfNeeded()
            {
                int minSize = offset + PixelWidth << 1;

                if (spriteCacheMemoryPool.GetBucketSize<float>() <= minSize)
                {
                    spriteCacheMemoryPool.ReAlloc(minSize);
                }
            }

            unsafe void Clamp(Span<int> wallStart, Span<int> wallEnd)
            {
                int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
                int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

                int start = 0;
                int length = PixelWidth;

                if (Vector.IsHardwareAccelerated && length > 128)
                {
                    int rem = length % Vector<int>.Count;
                    length -= rem;

                    Vector<int> zero = Vector<int>.Zero;
                    Vector<int> max = Vector.Create(PixelHeight - 1);
                    Vector<int> ceilingStartV, floorEndV;
                    Vector<int> wallStartV, wallEndV;

                    for (int i = 0; i < length; i += Vector<int>.Count)
                    {
                        ceilingStartV = Vector.LoadAligned(&ceilingStartPtr[i]);
                        floorEndV = Vector.LoadAligned(&floorEndPtr[i]);

                        ceilingStartV = Vector.ClampNative(ceilingStartV, zero, max);
                        floorEndV = Vector.ClampNative(floorEndV, zero, max);

                        wallStartV = Vector.LoadUnsafe(ref wallStart[i]);
                        wallStartV = Vector.ClampNative(wallStartV, ceilingStartV, floorEndV);
                        Vector.StoreUnsafe(wallStartV, ref wallStart[i]);

                        wallEndV = Vector.LoadUnsafe(ref wallEnd[i]);
                        wallEndV = Vector.ClampNative(wallEndV, ceilingStartV, floorEndV);
                        Vector.StoreUnsafe(wallEndV, ref wallEnd[i]);
                    }

                    start = length;
                    length += rem;
                }

                for (int i = start; i < length; i++)
                {
                    int ceilingStart = ceilingStartPtr[i];
                    int floorEnd = floorEndPtr[i];

                    ceilingStart = Math.Clamp(ceilingStart, 0, PixelHeight - 1);
                    floorEnd = Math.Clamp(floorEnd, 0, PixelHeight - 1);

                    wallStart[i] = Math.Clamp(wallStart[i], ceilingStart, floorEnd);
                    wallEnd[i] = Math.Clamp(wallEnd[i], ceilingStart, floorEnd);
                }
            }
        }

        private void DrawSprite(
            PortalPlayerSnapshot player,
            ReadOnlySpan<RenderableSector> sectors,
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

        private unsafe void DrawSprite(ReadOnlySpan<RenderableSector> sectors, RenderableBasicSprite sprite, RenderWindowSpriteSnapshot renderableWall)
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

            GameTextureInfo texture = sprite.Texture;

            int width = PixelWidth;
            int textureHeight = texture.Height;
            int textureWidth = texture.Width;

            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            RenderableSector sector = sectors[sprite.SectorId];

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

        private unsafe void DrawWallSprite(ReadOnlySpan<RenderableSector> sectors, RenderableWallSprite sprite, RenderWindowSpriteSnapshot renderableWall)
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

            GameTextureInfo texture = sprite.Texture;

            int width = PixelWidth;
            int textureHeight = texture.Height;
            int textureWidth = texture.Width;

            RenderableSector sector = sectors[sprite.SectorId];

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
            RenderableSector sector,
            IWallLike sprite,
            ushort* repeatedCount,
            int spriteFromX, int spriteToX,
            bool renderHorizontally,
            GameTextureInfo texture
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
                        if (isPowerOfTwo)
                        {
                            CoreRendererForPowTextures<DrawTransparentPixel>.RenderSpriteHorizontally(spriteFromX, spriteToX, width, repeatedCount, texturePtr, screenPtr, portalFromClampedPtr, portalToClampedPtr, textureXLocationPtr, textureYLocationPtr, textureYIncrementPtr);
                        }
                        else
                        {
                            CoreRendererForOddTextures<DrawTransparentPixel>.RenderSpriteHorizontally(spriteFromX, spriteToX, width, repeatedCount, texturePtr, screenPtr, portalFromClampedPtr, portalToClampedPtr, textureXLocationPtr, textureYLocationPtr, textureYIncrementPtr);

                        }
                    }
                    else
                    {
                        if (isPowerOfTwo)
                        {
                            CoreRendererForPowTextures<DrawAlphaPixel>.RenderSpriteHorizontally(spriteFromX, spriteToX, width, repeatedCount, texturePtr, screenPtr, portalFromClampedPtr, portalToClampedPtr, textureXLocationPtr, textureYLocationPtr, textureYIncrementPtr);
                        }
                        else
                        {
                            CoreRendererForOddTextures<DrawAlphaPixel>.RenderSpriteHorizontally(spriteFromX, spriteToX, width, repeatedCount, texturePtr, screenPtr, portalFromClampedPtr, portalToClampedPtr, textureXLocationPtr, textureYLocationPtr, textureYIncrementPtr);
                        }
                    }
                }
                else
                {
                    if (alpha == 1f)
                    {
                        if (isPowerOfTwo)
                        {
                            CoreRendererForPowTextures<DrawTransparentPixel>.RenderWall(spriteFromX, spriteToX, width, textureHeight, repeatedCount, texturePtr, screenPtr, portalFromClampedPtr, portalToClampedPtr, textureXLocationPtr, textureYLocationPtr, textureYIncrementPtr);
                        }
                        else
                        {
                            CoreRendererForUntiledTextures<DrawTransparentPixel>.RenderWall(spriteFromX, spriteToX, width, textureHeight, repeatedCount, texturePtr, screenPtr, portalFromClampedPtr, portalToClampedPtr, textureXLocationPtr, textureYLocationPtr, textureYIncrementPtr);
                        }
                    }
                    else
                    {
                        if (isPowerOfTwo)
                        {
                            CoreRendererForPowTextures<DrawAlphaPixel>.RenderWall(spriteFromX, spriteToX, width, textureHeight, repeatedCount, texturePtr, screenPtr, portalFromClampedPtr, portalToClampedPtr, textureXLocationPtr, textureYLocationPtr, textureYIncrementPtr);
                        }
                        else
                        {
                            CoreRendererForUntiledTextures<DrawAlphaPixel>.RenderWall(spriteFromX, spriteToX, width, textureHeight, repeatedCount, texturePtr, screenPtr, portalFromClampedPtr, portalToClampedPtr, textureXLocationPtr, textureYLocationPtr, textureYIncrementPtr);
                        }
                    }
                }
            }

            return;

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

                return ref texture.Texture.GetBinaryRef<uint>(texture.Palette, sprite.Shade ?? sector.FloorShade,
                    transform);
            }
        }

        private unsafe void DrawFloorSprite(
            PortalPlayerSnapshot player,
            ReadOnlySpan<RenderableSector> sectors,
            RenderableFloorSprite sprite,
            RenderWindowSpriteSnapshot renderableWall)
        {
            RenderableSector sector = sectors[sprite.SectorId];

            (float xScale, float yScale) = sprite.Texture.GetScale();
            (int from, int to) = (sprite.XLeft, sprite.XRight);

            float yFloor = sector.Floor - player.Z + sprite.Height;

            if (yFloor == 0f)
            {
                return;
            }

            int width = PixelWidth;

            Span<int> spriteWindowTop = memoryPool.GetBucket<int>(MemoryPoolBucket.TextureXLocation)[..width];
            Span<int> spriteWindowBottom = memoryPool.GetBucket<int>(MemoryPoolBucket.TextureYIncrement)[..width];

            int bufferOffset = PixelWidth * renderableWall.Depth;

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<int> wallStartSpan = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEndSpan = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            spriteWindowTop[from..(to + 1)].Fill(int.MaxValue);
            spriteWindowBottom[from..(to + 1)].Fill(int.MinValue);

            GameTextureInfo texture = sprite.Texture;
            bool translucent = texture.RenderingOptions.HasFlag(TextureRenderingOptions.Translucent);

            int textureWidth = texture.Width;

            ref uint floorTexturePtr = ref texture.Texture.GetBinaryRef<uint>(texture.Palette, sprite.Shade ?? sector.FloorShade,
                TextureTransform.Normal | TextureTransform.FlippedX);
            uint* screenPtr = (uint*)buffer;

            Vector<float> yFloorV = Vector.Create(yFloor);
            int textureHeightMask = texture.Height - 1;
            int textureWidthMask = texture.Width - 1;
            int xOffset = -texture.XOffset;
            int yOffset = texture.YOffset;

            bool rotated = false;
            Unsafe.SkipInit(out Vector<float> rSinV);
            Unsafe.SkipInit(out Vector<float> rCosV);
            Unsafe.SkipInit(out Vector<float> alignXV);
            Unsafe.SkipInit(out Vector<float> alignYV);

            if (sprite.Angle != 0f)
            {
                (float rSin, float rCos) = MathF.SinCos(sprite.Angle + MathF.PI * 0.5f);
                rSinV = Vector.Create(rSin);
                rCosV = Vector.Create(rCos);

                (float aX, float aY) = sprite.PointA;

                alignXV = Vector.Create(aX);
                alignYV = Vector.Create(aY);
                rotated = true;
            }

            PopulateFloorTextureBounds(spriteWindowTop, spriteWindowBottom, sprite, yFloor);
            LimitToDepth(yFloorV, sprite, spriteWindowTop, spriteWindowBottom, distance);

            Vector<float> xScaleV = Vector.Create(1f / xScale);
            Vector<float> yScaleV = Vector.Create(1f / yScale);

            int length = to - from + 1;
            int* portalFromClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            int* portalToClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);
            ushort* repeatedCountPtr = this.memoryPool.GetBucketPtr<ushort>(MemoryPoolBucket.Temp);

            for (int x = from; x <= to; x++)
            {
                int wallStart = wallStartSpan[x];
                int wallEnd = wallEndSpan[x];

                if (wallEnd <= wallStart)
                {
                    repeatedCountPtr[x - from] = 0;
                    continue;
                }

                int spriteFromY = spriteWindowTop[x];
                int spriteToY = spriteWindowBottom[x];

                int clampedFromY = Math.Clamp(spriteFromY, wallStart, wallEnd);
                int clampedToY = Math.Clamp(spriteToY, wallStart, wallEnd);

                if (clampedFromY >= clampedToY)
                {
                    repeatedCountPtr[x - from] = 0;
                    continue;
                }

                portalFromClampedPtr[x] = clampedFromY;
                portalToClampedPtr[x] = clampedToY;
                repeatedCountPtr[x - from] = (ushort)length;

                Debug.Assert(length > x - from);
                Debug.Assert(clampedFromY >= 0);
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCountPtr, length);

            fixed (uint* texturePtr = &floorTexturePtr)
            {
                float* xMapPosMultiplierCachePtr = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.XMapPosMultiplierCache);
                float* incrCachePtr = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.CameraHeightToMapYPos);

                if (translucent)
                {
                    ICoreRenderer<DrawAlphaPixel>.RenderFloorOrCeilingSprite(xMapPosMultiplierCachePtr, incrCachePtr, repeatedCountPtr, screenPtr, texturePtr, from, to,
                        portalFromClampedPtr, portalToClampedPtr,
                        width,
                        yFloor, yOffset, xOffset, textureWidth,
                        textureHeightMask, textureWidthMask, rotated, rSinV, rCosV, alignXV, alignYV, xScaleV, yScaleV,
                        pSinV, pCosV, pxV, pyV);
                }
                else
                {
                    ICoreRenderer<DrawTransparentPixel>.RenderFloorOrCeilingSprite(xMapPosMultiplierCachePtr, incrCachePtr, repeatedCountPtr, screenPtr, texturePtr, from, to,
                        portalFromClampedPtr, portalToClampedPtr,
                        width,
                        yFloor, yOffset, xOffset, textureWidth,
                        textureHeightMask, textureWidthMask, rotated, rSinV, rCosV, alignXV, alignYV, xScaleV, yScaleV,
                        pSinV, pCosV, pxV, pyV);
                }
            }
        }

        private unsafe void LimitToDepth(
            Vector<float> yOffsetV,
            RenderableFloorSprite sprite,
            Span<int> spriteWindowTop,
            Span<int> spriteWindowBottom,
            ReadOnlySpan<float> depth)
        {
            float* incrVectorCache = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.CameraHeightToMapYPos);

            for (int x = sprite.XLeft; x <= sprite.XRight; x++)
            {
                int spriteFromY = spriteWindowTop[x];
                int spriteToY = spriteWindowBottom[x];

                if (spriteFromY >= spriteToY)
                {
                    continue;
                }
                float depthAtX = depth[x];

                float yOffset = yOffsetV[0];
                float incrFromCache = *(incrVectorCache + spriteFromY);
                float incrToCache = *(incrVectorCache + spriteToY);
                float fromDepth = incrFromCache * yOffset;
                float toDepth = incrToCache * yOffset;

                while (fromDepth > depthAtX && spriteFromY < spriteToY)
                {
                    spriteFromY++;
                    incrFromCache = *(incrVectorCache + spriteFromY);
                    fromDepth = incrFromCache * yOffset;
                }

                while (toDepth > depthAtX && spriteToY > spriteFromY)
                {
                    spriteToY--;
                    incrToCache = *(incrVectorCache + spriteToY);
                    toDepth = incrToCache * yOffset;
                }

                spriteWindowTop[x] = spriteFromY;
                spriteWindowBottom[x] = spriteToY;
            }
        }

        // AI Assisted
        private void PopulateFloorTextureBounds(
            Span<int> spriteWindowTop,
            Span<int> spriteWindowBottom,
            RenderableFloorSprite sprite,
            float yFloor)
        {
            int maxHeight = PixelHeight - 1;

            bool hasNonIntersecting = false;

            for (int w = 0; w < 4; w++)
            {
                FloorSpriteWallInfo spriteBound = w switch
                {
                    0 => sprite.Wall1!,
                    1 => sprite.Wall2!,
                    2 => sprite.Wall3!,
                    3 => sprite.Wall4!,
                    _ => throw new NotSupportedException(),
                };

                if (!spriteBound.IntersectsView)
                {
                    hasNonIntersecting = true;
                    continue;
                }

                float bottomWallIncr = (spriteBound.YRightFloor - spriteBound.YLeftFloor) / (float)(spriteBound.XRight - spriteBound.XLeft);
                float bottomLoc = spriteBound.YLeftFloor;

                for (int i = spriteBound.XLeft; i <= spriteBound.XRight; i++, bottomLoc += bottomWallIncr)
                {
                    int yBottom = spriteWindowBottom[i];
                    int yTop = spriteWindowTop[i];
                    int loc = float.ConvertToIntegerNative<int>(bottomLoc);

                    spriteWindowBottom[i] = Math.Min(maxHeight, Math.Max(yBottom, loc));
                    spriteWindowTop[i] = Math.Max(0, Math.Min(yTop, loc));
                }
            }

            if (!hasNonIntersecting)
            {
                return;
            }

            if (sprite.R1.Y < 0f || sprite.R2.Y < 0f || sprite.R3.Y < 0f || sprite.R4.Y < 0f)
            {
                // The near plane cuts through the quad, creating an implicit boundary that
                // projects to ±infinity. Columns where only one edge wrote (top == bottom)
                // are on that cut; open them toward whichever screen edge yFloor projects to.
                bool nearPlaneAtTop = yFloor > 0f;

                for (int i = sprite.XLeft; i <= sprite.XRight; i++)
                {
                    int yTop = spriteWindowTop[i];
                    int yBottom = spriteWindowBottom[i];

                    if (yTop != yBottom)
                    {
                        continue;
                    }

                    if (nearPlaneAtTop)
                    {
                        spriteWindowTop[i] = 0;
                    }
                    else
                    {
                        spriteWindowBottom[i] = maxHeight;
                    }
                }
            }
        }
    }
}
