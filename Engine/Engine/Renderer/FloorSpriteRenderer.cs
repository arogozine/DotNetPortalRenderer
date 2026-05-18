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

            Span<float> xMapPosMultiplierCache = memoryPool.GetBucket<float>(MemoryPoolBucket.XMapPosMultiplierCache);

            int width = PixelWidth;

            int bufferOffset = PixelWidth * renderableWall.Depth;

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<int> wallStartSpan = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEndSpan = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            Span<int> spriteWindowTop = TempBuffer<int>.GetBuffer(width);
            Span<int> spriteWindowBottom = TempBuffer<int>.GetBuffer(width);

            spriteWindowTop[from..to].Fill(int.MaxValue);
            spriteWindowBottom[from..to].Fill(int.MinValue);

            TextureInfo texture = sprite.Texture;
            bool translucent = texture.RenderingOptions.HasFlag(TextureRenderingOptions.Translucent);

            int textureWidth = texture.Width;

            ref uint floorTexturePtr = ref texture.Texture.GetBinaryRef<uint>(sprite.Shade ?? sector.FloorShade,
                TextureTransform.Normal | TextureTransform.FlippedX);
            uint* screenPtr = (uint*)buffer;

            Vector<float> yFloorV = Vector.Create(yFloor);
            Vector<int> textureHeightMaskV = Vector.Create(texture.Height - 1);
            Vector<int> textureWidthMaskV = Vector.Create(texture.Width - 1);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

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

            Vector<int> xOffSetV = Vector.Create(xOffset);
            Vector<int> yOffSetV = Vector.Create(yOffset);

            Vector<float> xScaleV = Vector.Create(1f / xScale);
            Vector<float> yScaleV = Vector.Create(1f / yScale);

            for (int x = from; x < to; x++)
            {
                int wallStart = wallStartSpan[x];
                int wallEnd = wallEndSpan[x];

                if (wallEnd <= wallStart)
                {
                    continue;
                }

                int spriteFromY = spriteWindowTop[x];
                int spriteToY = spriteWindowBottom[x];

                int clamptedFromY = Math.Clamp(spriteFromY, wallStart, wallEnd);
                int clamptedToY = Math.Clamp(spriteToY, wallStart, wallEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    continue;
                }

                int screenIndex = clamptedFromY * width + x;

                float xMapPosMultiplier = xMapPosMultiplierCache[x];

                RenderFloorOrCeilingSpriteColumn(screenPtr, ref floorTexturePtr, screenIndex, clamptedToY, clamptedFromY, width,
                    x, yFloorV, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV, rotated, rSinV, rCosV, alignXV, alignYV, xScaleV, yScaleV, translucent);
            }
        }

        private unsafe void RenderFloorOrCeilingSpriteColumn(
            uint* screenPtr,
            scoped ref uint texturePtr,
            int screenIndex,
            int floorToY,
            int floorFromY,
            int width,
            int x,
            Vector<float> cameraPositionV,
            float xMapPosMultiplier,
            Vector<int> yOffSetV,
            Vector<int> xOffSetV,
            Vector<int> textureWidthV,
            Vector<int> textureHeightMaskV,
            Vector<int> textureWidthMaskV,
            bool rotated,
            Vector<float> rSinV,
            Vector<float> rCosV,
            Vector<float> alignXV,
            Vector<float> alignYV,
            Vector<float> xScaleV,
            Vector<float> yScaleV,
            bool translucent
        )
        {
            Span<float> incrVectorCache = memoryPool.GetBucket<float>(MemoryPoolBucket.CameraHeightToMapYPos);
            Vector<float> incramentVector = Vector.LoadUnsafe(ref incrVectorCache[floorFromY]);

            int rem = (floorToY - floorFromY) % Vector<int>.Count;
            floorToY -= rem;

            Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

            uint* screenTex = screenPtr + screenIndex;
            uint* toScalePtr = screenPtr + floorToY * width + x;

            while (screenTex != toScalePtr)
            {
                Vector<int> textureIndex = GetXyFromScreenSpace(incramentVector, xMapPosMultiplierV);

                if (Avx2.IsSupported && Vector<int>.Count == Vector256<int>.Count)
                {
                    Vector256<uint> gathered = Avx2.GatherVector256(
                        (uint*)Unsafe.AsPointer(ref texturePtr),
                        textureIndex.AsVector256(),
                        scale: sizeof(uint)
                    );

                    if (translucent)
                    {
                        for (int i = 0; i < Vector256<int>.Count; i++, screenTex += width)
                        {
                            uint tex = gathered[i];

                            if (tex != 0U)
                            {
                                *screenTex = BlendBGRA(tex, *screenTex);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Vector256<int>.Count; i++, screenTex += width)
                        {
                            uint tex = gathered[i];

                            if (tex != 0U)
                            {
                                *screenTex = gathered[i];
                            }
                        }
                    }
                }
                else
                {
                    if (translucent)
                    {
                        for (int i = 0; i < Vector<int>.Count; i++, screenTex += width)
                        {
                            uint tex = Unsafe.Add(ref texturePtr, textureIndex[i]);

                            if (tex != 0U)
                            {
                                *screenTex = BlendBGRA(tex, *screenTex);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Vector<int>.Count; i++, screenTex += width)
                        {
                            uint tex = Unsafe.Add(ref texturePtr, textureIndex[i]);

                            if (tex != 0U)
                            {
                                *screenTex = tex;
                            }
                        }
                    }
                }

                floorFromY += Vector<float>.Count;
                incramentVector = Vector.LoadUnsafe(ref incrVectorCache[floorFromY]);
            }

            if (rem > 0)
            {
                Vector<int> textureIndex = GetXyFromScreenSpace(incramentVector, xMapPosMultiplierV);

                if (translucent)
                {
                    for (int i = 0; i < rem; i++, screenTex += width)
                    {
                        uint tex = Unsafe.Add(ref texturePtr, textureIndex[i]);

                        if (tex != 0U)
                        {
                            *screenTex = BlendBGRA(tex, *screenTex);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < rem; i++, screenTex += width)
                    {
                        uint tex = Unsafe.Add(ref texturePtr, textureIndex[i]);
                        uint screen = *screenTex;

                        if (tex != 0U)
                        {
                            *screenTex = tex;
                        }
                    }
                }
            }

            return;

            static uint BlendBGRA(uint bgraDstU, uint bgraSrcU)
            {
                BGRA bgraDst = Unsafe.As<uint, BGRA>(ref bgraDstU);
                BGRA bgraSrc = Unsafe.As<uint, BGRA>(ref bgraSrcU);

                const uint a = 127;
                const uint aInv = 128;
                const uint Alpha = (uint)byte.MaxValue << 24;

                uint bDst = bgraDst.B;
                uint gDst = bgraDst.G;
                uint rDst = bgraDst.R;

                uint bSrc = bgraSrc.B;
                uint gSrc = bgraSrc.G;
                uint rSrc = bgraSrc.R;

                uint bOut = (bSrc * a + bDst * aInv) >> 8;
                uint gOut = (gSrc * a + gDst * aInv) >> 8;
                uint rOut = (rSrc * a + rDst * aInv) >> 8;

                return (Alpha | (rOut << 16) | (gOut << 8) | bOut);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Vector<int> GetXyFromScreenSpace(
                    Vector<float> incramentVector,
                    Vector<float> xMapPosMultiplierV
                )
            {
                Vector<float> yMapPosR = cameraPositionV * incramentVector;
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
        }

        private void LimitToDepth(
            Vector<float> yCeilV,
            RenderableFloorSprite sprite,
            Span<int> spriteWindowTop,
            Span<int> spriteWindowBottom,
            ReadOnlySpan<float> depth)
        {
            Span<float> incrVectorCache = memoryPool.GetBucket<float>(MemoryPoolBucket.CameraHeightToMapYPos);

            bool next;

            for (int x = sprite.XLeft; x < sprite.XRight; x++)
            {
                next = false;

                int spriteFromY = spriteWindowTop[x];
                int spriteToY = spriteWindowBottom[x];

                if (spriteFromY >= spriteToY)
                {
                    continue;
                }

                float y = depth[x];

                Vector<float> incramentVector = Vector.LoadUnsafe(ref incrVectorCache[spriteFromY]);

                // compare Y position of pixel to depth
                while (spriteFromY < spriteToY)
                {
                    Vector<float> yMapPosR = yCeilV * incramentVector;

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

                    incramentVector = Vector.LoadUnsafe(ref incrVectorCache[spriteFromY]);
                }

                spriteWindowTop[x] = spriteFromY;
            }
        }

        private void PopulateFloorTextureBounds(
            Span<int> spriteWindowTop,
            Span<int> spriteWindowBottom,
            RenderableFloorSprite sprite)
        {
            FloorSpriteWallInfo[] allWalls = [sprite.Wall1!, sprite.Wall2!, sprite.Wall3!, sprite.Wall4!];

            int maxHeight = PixelHeight - 1;

            bool hasNonIntersecting = false;
            
            for (int w = 0; w < allWalls.Length; w++)
            {
                FloorSpriteWallInfo spriteBound = allWalls[w];

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
