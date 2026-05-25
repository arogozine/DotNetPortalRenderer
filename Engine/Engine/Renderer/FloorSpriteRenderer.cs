using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private unsafe void DrawFloorSprite(
            PortalPlayerSnapshot player,
            ReadOnlySpan<Sector> sectors,
            RenderableFloorSprite sprite,
            RenderWindowSpriteSnapshot renderableWall)
        {
            Sector sector = sectors[sprite.SectorId];

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

            spriteWindowTop[from..to].Fill(int.MaxValue);
            spriteWindowBottom[from..to].Fill(int.MinValue);

            TextureInfo texture = sprite.Texture;
            bool translucent = texture.RenderingOptions.HasFlag(TextureRenderingOptions.Translucent);

            int textureWidth = texture.Width;

            ref uint floorTexturePtr = ref texture.Texture.GetBinaryRef<uint>(sprite.Shade ?? sector.FloorShade,
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

            PopulateFloorTextureBounds(spriteWindowTop, spriteWindowBottom, sprite);
            LimitToDepth(yFloorV, sprite, spriteWindowTop, spriteWindowBottom, distance);

            Vector<float> xScaleV = Vector.Create(1f / xScale);
            Vector<float> yScaleV = Vector.Create(1f / yScale);

            int length = to - from + 1;
            int* portalFromClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            int* portalToClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);
            ushort* repeatedCountPtr = this.memoryPool.GetBucketPtr<ushort>(MemoryPoolBucket.Temp);

            for (int x = from; x < to; x++)
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
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCountPtr, length);

            fixed (uint* texturePtr = &floorTexturePtr)
            {
                Sse.Prefetch2(texturePtr);

                if (translucent)
                {
                    RenderFloorOrCeilingSprite(new DrawAlphaPixel(), repeatedCountPtr, screenPtr, texturePtr, from, to,
                        portalFromClampedPtr, portalToClampedPtr,
                        width,
                        yFloor, yOffset, xOffset, textureWidth,
                        textureHeightMask, textureWidthMask, rotated, rSinV, rCosV, alignXV, alignYV, xScaleV, yScaleV);
                }
                else
                {
                    RenderFloorOrCeilingSprite(new DrawTransparentPixel(), repeatedCountPtr, screenPtr, texturePtr, from, to,
                        portalFromClampedPtr, portalToClampedPtr,
                        width,
                        yFloor, yOffset, xOffset, textureWidth,
                        textureHeightMask, textureWidthMask, rotated, rSinV, rCosV, alignXV, alignYV, xScaleV, yScaleV);
                }
            }
        }

        private unsafe void RenderFloorOrCeilingSprite<T>(
            T drawPixel,
            ushort* repeatedCount,
            uint* screenPtr,
            uint* texturePtr,
            int from, int to,
            int* fromYPtr,
            int* toYPtr,
            int width,
            float cameraPosition,
            int yOffset,
            int xOffset,
            int textureWidth,
            int textureHeightMask,
            int textureWidthMask,
            bool rotated,
            Vector<float> rSinV,
            Vector<float> rCosV,
            Vector<float> alignXV,
            Vector<float> alignYV,
            Vector<float> xScaleV,
            Vector<float> yScaleV
        )
            where T : IDrawPixel, allows ref struct
        {
            // Texture
            Vector<int> textureWidthV = Vector.Create(textureWidth);
            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> xOffSetV = Vector.Create(xOffset);
            Vector<int> yOffSetV = Vector.Create(yOffset);
            // Player height compared to ceiling/floor
            Vector<float> cameraPositionV = Vector.Create(cameraPosition);
            // Player Position
            Vector<float> pSinV = this.pSinV;
            Vector<float> pCosV = this.pCosV;
            Vector<float> pxV = this.pxV;
            Vector<float> pyV = this.pyV;
            // scalar
            float alignX = alignXV[0];
            float alignY = alignYV[0];
            float rSin = rSinV[0];
            float rCos = rCosV[0];
            float pSin = pSinV[0];
            float pCos = pCosV[0];
            float px = pxV[0];
            float py = pyV[0];

            float* xMapPosMultiplierCachePtr = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.XMapPosMultiplierCache);
            float* incrCachePtr = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.CameraHeightToMapYPos);

            for (int x = from; x <= to; )
            {
                int count = repeatedCount[x - from];

                if (count == 0)
                {
                    x++;
                    continue;
                }

                // Attempt horizontal rendering
                if (count >= Vector<int>.Count)
                {
                    (int min_t, int max_t, int min_b, int max_b) = CalculateLaneTopBottoms(x, fromYPtr, toYPtr);

                    if (min_b > max_t + 16)
                    {
                        RenderLine(drawPixel, x, fromYPtr + x, toYPtr + x,
                            min_t, max_t, min_b, max_b);

                        x += Vector<int>.Count;
                        continue;
                    }
                }

                while (count-- > 0)
                {
                    int clampedFromY = fromYPtr[x];
                    int clampedToY = toYPtr[x];
                    float xMapPosMultiplier = *(xMapPosMultiplierCachePtr + x);

                    RenderColumn(drawPixel, clampedFromY, clampedToY, x, xMapPosMultiplier);
                    x++;
                }
            }

            return;

            void RenderLine(
                T drawPixel,
                int x,
                int* to, int* from,
                int min_t, int max_t, int min_b, int max_b
                )
            {
                Vector<float> xMapPosMultiplierCacheV = Vector.Load(xMapPosMultiplierCachePtr + x);

                // render tops where there is no shared window
                if (min_t != max_t)
                {
                    RenderColumnAngleTop(drawPixel, min_t, max_t, from, x);
                }

                for (int y = max_t, screenIndex = y * width + x; y <= min_b; y++, screenIndex += width)
                {
                    Vector<float> incrementVector = Vector.Create(*(incrCachePtr + y));
                    Vector<int> textureIndex = GetXyFromScreenSpace(incrementVector, xMapPosMultiplierCacheV);

                    uint* screenTexPtr = screenPtr + screenIndex;

                    if (Avx2.IsSupported && Vector<int>.Count == Vector256<int>.Count)
                    {
                        Vector256<uint> gathered = Avx2.GatherVector256(texturePtr, textureIndex.AsVector256(), scale: sizeof(int));
                        drawPixel.DrawLine(screenTexPtr, gathered);
                    }
                    else
                    {
                        for (int i = 0; i < Vector<int>.Count; i++)
                        {
                            drawPixel.Draw(screenTexPtr + i, texturePtr[textureIndex[i]]);
                        }
                    }
                }

                // render bottoms where there is no shared window
                if (min_b != max_b)
                {
                    RenderColumnAngleBottom(drawPixel, min_b, max_b, to, x);
                }
            }

            void RenderColumnAngleBottom(
                T drawPixel,
                int floorFromY,
                int floorToY,
                int* to,
                int xStart)
            {
                uint* screenTexPtr = screenPtr + floorFromY * width + xStart;
                float* xMapPosMult = xMapPosMultiplierCachePtr + xStart;

                float* incr = incrCachePtr + floorFromY;

                for (int y = floorFromY; y < floorToY; y++)
                {
                    for (int i = 0; i < Vector<uint>.Count; i++)
                    {
                        if (to[i] > y)
                        {
                            float xMult = *(xMapPosMult + i);
                            int textureIndex = GetXyFromScreenSpaceScalar(*incr, xMult);
                            drawPixel.Draw(screenTexPtr + i, texturePtr[textureIndex]);
                        }
                    }

                    screenTexPtr += width;
                    incr++;
                }
            }

            void RenderColumnAngleTop(
                T drawPixel,
                int min_t,
                int max_t,
                int* from,
                int xStart)
            {
                uint* screenTexPtr = screenPtr + min_t * width + xStart;
                float* xMapPosMult = xMapPosMultiplierCachePtr + xStart;

                float* incr = incrCachePtr + min_t;

                for (int y = min_t; y < max_t; y++)
                {
                    for (int i = 0; i < Vector<uint>.Count; i++)
                    {
                        if (from[i] >= y)
                        {
                            continue;
                        }

                        float xMult = *(xMapPosMult + i);
                        int textureIndex = GetXyFromScreenSpaceScalar(*incr, xMult);
                        drawPixel.Draw(screenTexPtr + i, texturePtr[textureIndex]);
                    }

                    screenTexPtr += width;
                    incr++;
                }
            }

            void RenderColumn(T drawPixel, int floorFromY, int floorToY, int x, float xMapPosMultiplier)
            {
                int rem = (floorToY - floorFromY) & (Vector<int>.Count - 1);
                floorToY -= rem;

                Vector<float> incrementVector = Vector.Load(incrCachePtr + floorFromY);
                Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);
                Vector<int> textureIndex;
                
                uint* screenTex = screenPtr + floorFromY * width + x;
                uint* toScalePtr = screenPtr + floorToY * width + x;

                while (screenTex != toScalePtr)
                {
                    textureIndex = GetXyFromScreenSpace(incrementVector, xMapPosMultiplierV);

                    if (Avx2.IsSupported && Vector<int>.Count == Vector256<int>.Count)
                    {
                        Vector256<uint> gathered = Avx2.GatherVector256(
                            texturePtr,
                            textureIndex.AsVector256(),
                            scale: sizeof(uint)
                        );

                        for (int i = 0; i < Vector256<int>.Count; i++, screenTex += width)
                        {
                            uint tex = gathered[i];
                            drawPixel.Draw(screenTex, tex);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Vector<int>.Count; i++, screenTex += width)
                        {
                            uint tex = *(texturePtr + textureIndex[i]);

                            drawPixel.Draw(screenTex, tex);
                        }
                    }

                    floorFromY += Vector<float>.Count;
                    incrementVector = Vector.Load(incrCachePtr + floorFromY);
                }

                if (rem <= 0)
                {
                    return;
                }
                
                textureIndex = GetXyFromScreenSpace(incrementVector, xMapPosMultiplierV);

                for (int i = 0; i < rem; i++, screenTex += width)
                {
                    uint tex = *(texturePtr + textureIndex[i]);

                    drawPixel.Draw(screenTex, tex);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Vector<int> GetXyFromScreenSpace(
                    Vector<float> incrementVector,
                    Vector<float> xMapPosMultiplierV
                )
            {
                Vector<float> yMapPosR = cameraPositionV * incrementVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    xMapPos -= alignXV;
                    yMapPos -= alignYV;

                    Vector<float> xMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rCosV, -yMapPos * rSinV);
                    Vector<float> yMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rSinV, yMapPos * rCosV);

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos * yScaleV);
                Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos * xScaleV);

                _y1 = (_y1 + yOffSetV) & textureHeightMaskV;
                _x1 = (_x1 + xOffSetV) & textureWidthMaskV;

                return _y1 * textureWidthV + _x1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int GetXyFromScreenSpaceScalar(float increment, float xMapPosMultiplier)
            {
                float yMapPosR = cameraPosition * increment;
                float xMapPosR = yMapPosR * xMapPosMultiplier;

                (float xMapPos, float yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                if (rotated)
                {
                    xMapPos -= alignX;
                    yMapPos -= alignY;

                    float xMapPosSR = MathF.FusedMultiplyAdd(xMapPos, rCos, -yMapPos * rSin);
                    float yMapPosSR = MathF.FusedMultiplyAdd(xMapPos, rSin, yMapPos * rCos);

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                int _y1 = float.ConvertToIntegerNative<int>(yMapPos);
                int _x1 = float.ConvertToIntegerNative<int>(xMapPos);

                _y1 = (_y1 + yOffset) & textureHeightMask;
                _x1 = (_x1 + xOffset) & textureWidthMask;

                return _y1 * textureWidth + _x1;
            }
        }

        private unsafe void LimitToDepth(
            Vector<float> yCeilV,
            RenderableFloorSprite sprite,
            Span<int> spriteWindowTop,
            Span<int> spriteWindowBottom,
            ReadOnlySpan<float> depth)
        {
            float* incrVectorCache = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.CameraHeightToMapYPos);
            
            for (int x = sprite.XLeft; x < sprite.XRight; x++)
            {
                bool next = false;

                int spriteFromY = spriteWindowTop[x];
                int spriteToY = spriteWindowBottom[x];

                if (spriteFromY >= spriteToY)
                {
                    continue;
                }

                float y = depth[x];

                Vector<float> incrementVector = Vector.Load(incrVectorCache + spriteFromY);

                // compare Y position of pixel to depth
                while (spriteFromY < spriteToY)
                {
                    Vector<float> yMapPosR = yCeilV * incrementVector;

                    for (int i = 0; i < Vector<float>.Count; i++)
                    {
                        if (yMapPosR[i] < y)
                        {
                            next = true;
                            break;
                        }

                        spriteFromY++;
                    }

                    if (next)
                    {
                        break;
                    }

                    incrementVector = Vector.Load(incrVectorCache + spriteFromY);
                }

                spriteWindowTop[x] = spriteFromY;
            }
        }

        private void PopulateFloorTextureBounds(
            Span<int> spriteWindowTop,
            Span<int> spriteWindowBottom,
            RenderableFloorSprite sprite)
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

                for (int i = spriteBound.XLeft; i < spriteBound.XRight; i++, bottomLoc += bottomWallIncr)
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
                for (int i = 0; i < spriteWindowBottom.Length; i++)
                {
                    int yBottom = spriteWindowBottom[i];
                    int yTop = spriteWindowTop[i];

                    if (yTop != yBottom)
                    {
                        continue;
                    }

                    spriteWindowBottom[i] = PixelHeight - 1;
                }
            }
        }
    }
}
