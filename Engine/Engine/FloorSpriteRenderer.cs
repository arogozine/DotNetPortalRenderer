using RenderingEngine.Models;
using System.Buffers;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void DrawFloorSprite(
            PortalPlayerSnapshot player,
            ReadOnlySpan<Sector> sectors,
            RenderableSprite sprite,
            RenderWindowSpriteSnapshot renderableWall)
        {
            int height = PixelHeight;
            int width = PixelWidth;

            ReadOnlySpan<int> floorEndArray = renderableWall.FloorEnd;
            ReadOnlySpan<int> ceilingStartArray = renderableWall.CeilingStart;
            ReadOnlySpan<float> distance = renderableWall.Distance;

            Sector sector = sectors[sprite.SectorId];

            (Point topLeft, Point topRight, Point bottomLeft, Point bottomRight) = GetSpriteBoundingBox(player, sprite);

            (float xScale, float yScale) = sprite.Texture.GetScale();

            float yCeil = sector.Ceil - player.Z;
            float yFloor = sector.Floor - player.Z + sprite.Height;
            float yaw = player.Yaw;

            FloorSpriteWallInfo topWall = CalculateWallPlane(topLeft, topRight, yCeil, yFloor, yaw);
            FloorSpriteWallInfo rightWall = CalculateWallPlane(topRight, bottomRight, yCeil, yFloor, yaw);
            FloorSpriteWallInfo bottomWall = CalculateWallPlane(bottomRight, bottomLeft, yCeil, yFloor, yaw);
            FloorSpriteWallInfo leftWall = CalculateWallPlane(bottomLeft, topLeft, yCeil, yFloor, yaw);

            if (!topWall.IntersectsView && !rightWall.IntersectsView && !bottomWall.IntersectsView && !leftWall.IntersectsView)
            {
                return;
            }


            int[] spriteWindowTop = ArrayPool<int>.Shared.Rent(width);
            int[] spriteWindowBottom = ArrayPool<int>.Shared.Rent(width);

            (int from, int to) = DetermineBounds(topWall, rightWall, bottomWall, leftWall);

            spriteWindowTop.AsSpan(from..to).Fill(int.MaxValue);
            spriteWindowBottom.AsSpan(from..to).Fill(int.MinValue);

            TextureInfo textureInfo = sprite.Texture;
            Texture texture = TextureCache.GetTexture(textureInfo.Name);

            int textureWidth = texture.Width;

            int widthDiv2 = width / 2;

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Data);
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();
            byte lightLevel = sector.LightLevel;

            Vector<float> yFloorV = Vector.Create(yFloor);
            Vector<int> textureHeightMaskV = Vector.Create(texture.Height - 1);
            Vector<int> textureWidthMaskV = Vector.Create(texture.Width - 1);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            int xOffset = -textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;

            int halfHeightInt = height / 2;


            Unsafe.SkipInit(out Vector<float> rSinV);
            Unsafe.SkipInit(out Vector<float> rCosV);
            Vector<float> incramentVector;

            PopulateFloorTextureBounds(spriteWindowTop, spriteWindowBottom, topWall, rightWall, bottomWall, leftWall);

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

                int spriteFromY = spriteWindowTop[x];
                int spriteToY = spriteWindowBottom[x];

                int clamptedFromY = Math.Clamp(spriteFromY, ceilingStart, floorEnd);
                int clamptedToY = Math.Clamp(spriteToY, ceilingStart, floorEnd);

                if (clamptedFromY == clamptedToY)
                {
                    continue;
                }

                int screenIndex = clamptedFromY * width + x;

                float xMapPosMultiplier = (widthDiv2 - x) * xPosIncr;

                incramentVector = Vector.CreateSequence(halfHeightInt - clamptedFromY, -1f);
                incramentVector = Vector.FusedMultiplyAdd(incramentVector, oneOverHeightV, yawV);

                RenderFloorOrCeilingSpriteColumn(ref screenPtr, ref floorTexturePtr, screenIndex, clamptedToY, clamptedFromY, width,
                    x, lightLevel, yFloorV, incramentVector, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV, rotated, rSinV, rCosV, flipY, flipX, swapXy, xScaleV, yScaleV);
            }

            ArrayPool<int>.Shared.Return(spriteWindowTop);
            ArrayPool<int>.Shared.Return(spriteWindowBottom);
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
            Vector<float> incramentVector,
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
            int rem = (floorToY - floorFromY) % Vector<int>.Count;
            floorToY -= rem;

            Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

            ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
            ref readonly BGRA toScalePtr = ref Unsafe.Add(ref screenPtr, floorToY * width + x);

            while (!Unsafe.AreSame(in screenTex, in toScalePtr))
            {
                Vector<float> yMapPosR = yCeilV / incramentVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    Vector<float> xMapPosSR = xMapPos * rCosV - yMapPos * rSinV;
                    Vector<float> yMapPosSR = xMapPos * rSinV + yMapPos * rCosV;

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

                incramentVector -= ivIncrF;
            }

            if (rem > 0)
            {
                Vector<float> yMapPosR = yCeilV / incramentVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    Vector<float> xMapPosSR = xMapPos * rCosV - yMapPos * rSinV;
                    Vector<float> yMapPosSR = xMapPos * rSinV + yMapPos * rCosV;

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

        private static void PopulateFloorTextureBounds(
            Span<int> spriteWindowTop,
            Span<int> spriteWindowBottom,
            params ReadOnlySpan<FloorSpriteWallInfo> spriteBounds)
        {
            for (int s = 0; s < spriteBounds.Length; s++)
            {
                FloorSpriteWallInfo spriteBound = spriteBounds[s];

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

                    spriteWindowBottom[i] = Math.Max(yBottom, loc);
                    spriteWindowTop[i] = Math.Min(yTop, loc);
                }
            }
        }

        private static (int from, int to) DetermineBounds(params ReadOnlySpan<FloorSpriteWallInfo> spriteBounds)
        {
            int from = int.MaxValue;
            int to = int.MinValue;

            for (int i = 0; i < spriteBounds.Length; i++)
            {
                var spriteBound = spriteBounds[i];

                if (spriteBound.IntersectsView)
                {
                    from = Math.Min(from, spriteBound.XLeft);
                    to = Math.Max(to, spriteBound.XRight);
                }
            }

            return (from, to);
        }

        private sealed class FloorSpriteWallInfo
        {
            public required bool IntersectsView { get; set; }
            public int XLeft { get; set; }
            public int XRight { get; set; }
            public int YLeftFloor { get; set; }
            public int YRightFloor { get; set; }
        }

        private FloorSpriteWallInfo CalculateWallPlane(
            Point r1, Point r2,
            float yCeil, float yFloor, float yaw)
        {
            int height = this.PixelHeight;
            int width = this.PixelWidth;

            // calculate the x, y for the wall on the screen for both points
            (float rx1, float ry1) = r1;
            (float rx2, float ry2) = r2;

            float xLeft, xRight, yLeftFloor, yRightFloor;
            float scale = width * -EngineConstants.HeightToWidthRatio;
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            xLeft = halfWidth - rx1 / ry1 * scale;
            xRight = halfWidth - rx2 / ry2 * scale;

            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                // (wall.R1, wall.R2) = (wall.R2, wall.R1);
            }

            var spriteWallInfo = new FloorSpriteWallInfo { IntersectsView = false };

            // part of the wall is in the back
            if (ry1 <= 0f || ry2 <= 0f)
            {
                float d2x = rx2 - rx1;
                float d2y = ry2 - ry1;

                bool intersectsL = MathFormulas.TryGetSegmentIntersectionZero2(-EngineConstants.CameraPlaneX, rx1, ry1, d2x, d2y,
                    out float xDistanceL, out float yDistanceL);

                bool intersectsR = MathFormulas.TryGetSegmentIntersectionZero2(EngineConstants.CameraPlaneX, rx2, ry2, -d2x, -d2y,
                    out float xDistanceR, out float yDistanceR);

                if (intersectsL && intersectsR)
                {
                    rx1 = xDistanceL;
                    ry1 = yDistanceL;

                    rx2 = xDistanceR;
                    ry2 = yDistanceR;

                    xLeft = 0;
                    xRight = width - 1;

                    // wall.IntersectsView = true;
                    // wall.Flipped = true;
                    spriteWallInfo.IntersectsView = true;
                }
                else if (intersectsL || intersectsR)
                {
                    float xDistance = intersectsL ? xDistanceL : xDistanceR;
                    float yDistance = intersectsL ? yDistanceL : yDistanceR;

                    if (ry1 <= 0f)
                    {
                        rx1 = xDistance;
                        ry1 = yDistance;
                        xLeft = halfWidth - rx1 / ry1 * scale;
                    }
                    else
                    {
                        rx2 = xDistance;
                        ry2 = yDistance;
                        xRight = halfWidth - rx2 / ry2 * scale;
                    }

                    // wall.Flipped = true;
                }
                else
                {
                    // despite one the wall going behind the player's view
                    // player's view doesn't intersect at corners
                    // we assume wall can't be drawn
                    // wall.IntersectsView = false;
                    return spriteWallInfo;
                }
            }

            if (xLeft == xRight)
            {
                // wall.IntersectsView = false;
                return spriteWallInfo;
            }

            Clamp(ref xLeft, ref xRight);

            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                // (wall.R1, wall.R2) = (wall.R2, wall.R1);
                // wall.Flipped = !wall.Flipped;
            }

            spriteWallInfo.IntersectsView |= MathFormulas.CalculatePlaneIntersectionsForWall(width, xLeft, xRight, ref rx1, ref ry1, ref rx2, ref ry2);

            if (spriteWallInfo.IntersectsView)
            {
                yLeftFloor = halfHeight - (yFloor / ry1 - yaw) * height;
                yRightFloor = halfHeight - (yFloor / ry2 - yaw) * height;

                // wall.C1 = new(rx1, ry1);
                // wall.C2 = new(rx2, ry2);

                spriteWallInfo.XLeft = float.ConvertToIntegerNative<int>(xLeft);
                spriteWallInfo.XRight = float.ConvertToIntegerNative<int>(xRight);
                spriteWallInfo.YLeftFloor = float.ConvertToIntegerNative<int>(yLeftFloor);
                spriteWallInfo.YRightFloor = float.ConvertToIntegerNative<int>(yRightFloor);

            }

            return spriteWallInfo;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Clamp(ref float xLeft, ref float xRight)
            {
                xLeft = Math.Clamp(xLeft, 0f, width - 1f);
                xRight = Math.Clamp(xRight, 0f, width - 1f);
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

        private static (Point TopLeft, Point TopRight, Point BottomLeft, Point BottomRight) GetSpriteBoundingBox(
            PortalPlayerSnapshot player,
            RenderableSprite sprite)
        {
            float pSin = player.Sin;
            float pCos = player.Cos;
            float px = player.X;
            float py = player.Y;

            Point a = SharedHelpers.RotateVertex(sprite.PointA, pSin, pCos, px, py);
            Point b = SharedHelpers.RotateVertex(sprite.PointB, pSin, pCos, px, py);
            Point c = SharedHelpers.RotateVertex(sprite.PointC, pSin, pCos, px, py);
            Point d = SharedHelpers.RotateVertex(sprite.PointD, pSin, pCos, px, py);

            return (a, b, c, d);
        }

    }
}
