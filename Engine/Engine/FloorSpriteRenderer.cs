using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void DrawFloorSprite(
            PortalPlayerSnapshot player,
            ReadOnlySpan<Sector> sectors,
            RenderableFloorSprite sprite,
            RenderWindowSpriteSnapshot renderableWall)
        {
            int height = PixelHeight;
            int width = PixelWidth;

            ReadOnlySpan<int> floorEndArray = renderableWall.FloorEnd;
            ReadOnlySpan<int> ceilingStartArray = renderableWall.CeilingStart;
            ReadOnlySpan<float> distance = RenderWindowHelper.Distance; // renderableWall.Distance;

            Sector sector = sectors[sprite.SectorId];

            (float xScale, float yScale) = sprite.Texture.GetScale();
            (int from, int to) = (sprite.XLeft, sprite.XRight);

            float yFloor = sector.Floor - player.Z + sprite.Height;

            using var spriteWindowTop = TempBuffer<int>.GetBuffer(width);
            using var spriteWindowBottom = TempBuffer<int>.GetBuffer(width);

            spriteWindowTop.Span[from..to].Fill(int.MaxValue);
            spriteWindowBottom.Span[from..to].Fill(int.MinValue);

            TextureInfo texture = sprite.Texture;

            int textureWidth = texture.Width;

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Data);
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();
            byte lightLevel = sector.LightLevel;

            Vector<float> yFloorV = Vector.Create(yFloor);
            Vector<int> textureHeightMaskV = Vector.Create(texture.Height - 1);
            Vector<int> textureWidthMaskV = Vector.Create(texture.Width - 1);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            int xOffset = -texture.XOffset;
            int yOffset = texture.YOffset;

            int halfHeightInt = height / 2;

            Unsafe.SkipInit(out Vector<float> rSinV);
            Unsafe.SkipInit(out Vector<float> rCosV);

            PopulateFloorTextureBounds(spriteWindowTop, spriteWindowBottom, sprite);
            LimitToDepth(yFloorV, sprite, spriteWindowTop, spriteWindowBottom, distance);

            (int a, int b) = DetermineOffset(sprite);
            xOffset += a;
            yOffset += b;
            Vector<int> xOffSetV = Vector.Create(xOffset);
            Vector<int> yOffSetV = Vector.Create(yOffset);

            bool rotated = false;
            bool flipY = false;
            bool flipX = false;
            bool swapXy = false;

            Vector<float> xScaleV = Vector.Create(1f / xScale);
            Vector<float> yScaleV = Vector.Create(1f / yScale);

            for (int x = from; x < to; x++)
            {
                int ceilingStart = ceilingStartArray[x];
                int floorEnd = floorEndArray[x];

                if (floorEnd <= ceilingStart)
                {
                    continue;
                }

                int spriteFromY = spriteWindowTop.Span[x];
                int spriteToY = spriteWindowBottom.Span[x];

                int clamptedFromY = Math.Clamp(spriteFromY, ceilingStart, floorEnd);
                int clamptedToY = Math.Clamp(spriteToY, ceilingStart, floorEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    continue;
                }

                int screenIndex = clamptedFromY * width + x;

                float xMapPosMultiplier = this.xMapPosMultiplierCache[x];

                RenderFloorOrCeilingSpriteColumn(ref screenPtr, ref floorTexturePtr, screenIndex, clamptedToY, clamptedFromY, width,
                    x, lightLevel, yFloorV, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV, rotated, rSinV, rCosV, flipY, flipX, swapXy, xScaleV, yScaleV);
            }
        }

        private void RenderFloorOrCeilingSpriteColumn(
            scoped ref BGRA screenPtr,
            scoped ref BGRA texturePtr,
            int screenIndex,
            int floorToY,
            int floorFromY,
            int width,
            int x,
            uint lightLevel,
            Vector<float> yCeilV,
            float xMapPosMultiplier,
            Vector<int> yOffSetV,
            Vector<int> xOffSetV,
            Vector<int> textureWidthV,
            Vector<int> textureHeightMaskV,
            Vector<int> textureWidthMaskV,
            bool rotated,
            Vector<float> rSinV,
            Vector<float> rCosV,
            bool flipY,
            bool flipX,
            bool swapXy,
            Vector<float> xScaleV,
            Vector<float> yScaleV
        )
        {
            Span<float> incrVectorCache = this.cameraHeightToMapYPos;
            Vector<float> incramentVector = Vector.LoadUnsafe(ref incrVectorCache[floorFromY]);

            int rem = (floorToY - floorFromY) % Vector<int>.Count;
            floorToY -= rem;

            Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

            ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
            ref readonly BGRA toScalePtr = ref Unsafe.Add(ref screenPtr, floorToY * width + x);

            while (!Unsafe.AreSame(in screenTex, in toScalePtr))
            {
                Vector<float> yMapPosR = yCeilV * incramentVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    Vector<float> xMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rCosV, -yMapPos * rSinV);
                    Vector<float> yMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rSinV, yMapPos * rCosV);

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos * yScaleV);
                Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos * xScaleV);

                _y1 = (_y1 + yOffSetV) & textureHeightMaskV;
                _x1 = (_x1 + xOffSetV) & textureWidthMaskV;

                if (flipY)
                {
                    _y1 = textureHeightMaskV - _y1;
                }

                if (flipX)
                {
                    _x1 = textureWidthMaskV - _x1;
                }

                if (swapXy)
                {
                    (_y1, _x1) = (_x1, _y1);
                }

                Vector<int> textureIndex = _y1 * textureWidthV + _x1;

                ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                for (int i = 0; i < Vector<int>.Count; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                {
                    ref BGRA tex = ref Unsafe.Add(ref texturePtr, Unsafe.Add(ref textureIndexPtr, i));
                    if (tex.Value != 0U)
                    {
                        ShadeByPrecalc(in tex, ref screenTex, lightLevel);
                    }
                }

                floorFromY += Vector<float>.Count;
                incramentVector = Vector.LoadUnsafe(ref incrVectorCache[floorFromY]);
            }

            if (rem > 0)
            {
                Vector<float> yMapPosR = yCeilV * incramentVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    Vector<float> xMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rCosV, -yMapPos * rSinV);
                    Vector<float> yMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rSinV, yMapPos * rCosV);

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos * yScaleV);
                Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos * xScaleV);

                _y1 = (_y1 + yOffSetV) & textureHeightMaskV;
                _x1 = (_x1 + xOffSetV) & textureWidthMaskV;

                if (flipY)
                {
                    _y1 = textureHeightMaskV - _y1;
                }

                if (flipX)
                {
                    _x1 = textureWidthMaskV - _x1;
                }

                if (swapXy)
                {
                    (_y1, _x1) = (_x1, _y1);
                }

                Vector<int> textureIndex = _y1 * textureWidthV + _x1;

                ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                for (int i = 0; i < rem; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                {
                    ref BGRA tex = ref Unsafe.Add(ref texturePtr, Unsafe.Add(ref textureIndexPtr, i));
                    if (tex.Value != 0U)
                    {
                        ShadeByPrecalc(in tex, ref screenTex, lightLevel);
                    }
                }
            }
        }

        private void LimitToDepth(
            Vector<float> yCeilV,
            RenderableFloorSprite sprite,
            Span<int> spriteWindowTop,
            Span<int> spriteWindowBottom,
            ReadOnlySpan<float> depth)
        {
            Span<float> incrVectorCache = this.cameraHeightToMapYPos;

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
            int maxHeight = PixelHeight - 1;

            foreach (FloorSpriteWallInfo spriteBound in new[] { sprite.Wall1!, sprite.Wall2!, sprite.Wall3!, sprite.Wall4! })
            {
                if (!spriteBound.IntersectsView)
                {
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
        }

        private static (int xOffset, int yOffset) DetermineOffset(RenderableSprite sprite)
        {
            TextureInfo texture = sprite.Texture;

            (float xScale, float yScale) = texture.GetScale();

            (float xFrom, float yTo) = sprite.PointA;

            xFrom *= (1f/xScale);
            yTo *= (1f/yScale);

            int xOffset = float.ConvertToIntegerNative<int>(xFrom % texture.Width);
            int yOffset = float.ConvertToIntegerNative<int>(yTo % texture.Height);

            xOffset = SharedHelpers.EnsureOffsetIsPositive(texture.Width, xOffset);
            yOffset = texture.Height - SharedHelpers.EnsureOffsetIsPositive(texture.Height, yOffset);

            return (-xOffset, yOffset);
        }
    }
}
