using RenderingEngine.Models;
using System.Buffers;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void DrawSprite(PortalPlayerSnapshot player, ReadOnlySpan<Sector> sectors, RenderableSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            TextureInfo textureInfo = sprite.Texture;

            if (textureInfo.RenderingOptions.IsWall)
            {
                DrawWallSprite(sectors, sprite, renderableWall);
                return;
            }

            if (textureInfo.RenderingOptions.IsFloor)
            {
                DrawFloorSprite(player, sectors, sprite, renderableWall);
                return;
            }

            Texture texture = TextureCache.GetTexture(textureInfo.Name);

            ref uint screenPtr = ref GetScreenPtr<uint>();
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);

            int width = PixelWidth;
            int textureWidth = texture.Height;

            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            Sector sector = sectors[sprite.SectorId];
            byte lightLevel = sector.LightLevel;

            float rx1 = sprite.R1.X;
            float rx2 = sprite.R2.X;

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteStartY = sprite.YLeftCeil;
            int spriteEndY = sprite.YLeftFloor;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            Span<int> floorEndArray = renderableWall.FloorEnd;
            Span<int> ceilingStartArray = renderableWall.CeilingStart;
            Span<float> distance = renderableWall.Distance;

            float fromToYDist = sprite.Distance;

            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * spriteFromX;

            ref uint columnBufferPtr = ref GetBufferA(textureWidth, out Span<uint> columnBuffer);

            int textureXIncr = (textureWidth << 16) / (spriteEndY - spriteStartY);

            bool flipY = sprite.Texture.RenderingOptions.IsFlippedY;
            bool flipX = sprite.Texture.RenderingOptions.IsFlippedX;

            float textureLen = texture.Width / sprite.Length;

            for (int x = spriteFromX; x < spriteToX; x++, cameraRay += cameraWidthIncr)
            {
                int ceilingStart = ceilingStartArray[x];
                int floorEnd = floorEndArray[x];

                if (floorEnd <= ceilingStart || distance[x] < fromToYDist)
                {
                    continue;
                }

                int clamptedFromY = Math.Clamp(spriteStartY, ceilingStart, floorEnd);
                int clamptedToY = Math.Clamp(spriteEndY, ceilingStart, floorEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    continue;
                }

                int textureXLocation = CalculateTextureXPosition(cameraRay);

                int textureYPos = textureXLocation * textureWidth;
                int textureXPos = (clamptedFromY - spriteStartY) * textureXIncr;

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel, flipY);

                DrawSpriteLine(width, x, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                    ref screenPtr, ref columnBufferPtr);
            }

            columnABufferIndex = EngineConstants.Unset;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int CalculateTextureXPosition(float cameraRay)
            {
                float fromToXDist = fromToYDist * cameraRay;
                float distX = flipX ? (rx2 - fromToXDist) : (fromToXDist - rx1);

                return float.ConvertToIntegerNative<int>(MathF.Abs(distX) * textureLen);
            }
        }

        private void DrawWallSprite(ReadOnlySpan<Sector> sectors, RenderableSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            TextureInfo textureInfo = sprite.Texture;

            Texture texture = TextureCache.GetTexture(textureInfo.Name);

            ref uint screenPtr = ref GetScreenPtr<uint>();
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);

            int width = PixelWidth;
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            Sector sector = sectors[sprite.SectorId];
            byte lightLevel = sector.LightLevel;

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(sprite, 0);
            float spriteStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float spriteEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            int xOffset = 0;

            ReadOnlySpan<int> floorEndArray = renderableWall.FloorEnd;
            ReadOnlySpan<int> ceilingStartArray = renderableWall.CeilingStart;
            ReadOnlySpan<float> distance = renderableWall.Distance;

            ref uint columnBufferPtr = ref GetBufferA(textureWidth, out Span<uint> columnBuffer);

            bool flipY = sprite.Texture.RenderingOptions.IsFlippedY;
            bool flipX = sprite.Texture.RenderingOptions.IsFlippedX;

            float xScale = texture.Width / sprite.Length;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(sprite, width, spriteFromX);

            for (int x = spriteFromX; x < spriteToX; x++, cameraRay += cameraWidthIncr, spriteStartY += ceilDistIncr, spriteEndY += floorDistIncr)
            {
                int ceilingStart = ceilingStartArray[x];
                int floorEnd = floorEndArray[x];

                if (floorEnd <= ceilingStart)
                {
                    continue;
                }

                int spriteStartY_Int = float.ConvertToIntegerNative<int>(spriteStartY);
                int spriteEndY_Int = float.ConvertToIntegerNative<int>(spriteEndY);
                int clamptedFromY = Math.Clamp(spriteStartY_Int, ceilingStart, floorEnd);
                int clamptedToY = Math.Clamp(spriteEndY_Int, ceilingStart, floorEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    continue;
                }

                (float textureXLocation, float fromToYdist) = MathFormulas.CalculateDistance(sprite, cameraRay, t1, d2y, d2x, flipX);

                if ((int)distance[x] < (int)fromToYdist)
                {
                    continue;
                }

                textureXLocation += xOffset;
                textureXLocation *= xScale;
                int textureXIncr = (textureWidth << 16) / (spriteEndY_Int - spriteStartY_Int);
                int textureYPos = (float.ConvertToIntegerNative<int>(textureXLocation) % textureHeight) * textureWidth;
                int textureXPos = (clamptedFromY - spriteStartY_Int) * textureXIncr;

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel, flipY);

                DrawSpriteLine(width, x, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                    ref screenPtr, ref columnBufferPtr);
            }

            columnABufferIndex = EngineConstants.Unset;
        }

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

            float xScale = sprite.Texture.XScale ?? 1f;
            float yScale = sprite.Texture.YScale ?? 1f;

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
            Vector<int> xOffSetV = Vector.Create(xOffset);
            Vector<int> yOffSetV = Vector.Create(yOffset);

            int halfHeightInt = height / 2;


            Unsafe.SkipInit(out Vector<float> rSinV);
            Unsafe.SkipInit(out Vector<float> rCosV);

            Unsafe.SkipInit(out Vector<float> incramentVector);
            ref float incramentVectorPtr = ref Unsafe.As<Vector<float>, float>(ref incramentVector);

            PopulateFloorTextureBounds(spriteWindowTop, spriteWindowBottom, topWall, rightWall, bottomWall, leftWall);

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
                    Vector<float> xMapPosSR, yMapPosSR;
                    xMapPosSR = xMapPos * rCosV - yMapPos * rSinV;
                    yMapPosSR = xMapPos * rSinV + yMapPos * rCosV;

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos * yScaleV);
                Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos * xScaleV);

                _y1 = (_y1 + xOffSetV) & textureHeightMaskV;
                _x1 = (_x1 + yOffSetV) & textureWidthMaskV;

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
                    Vector<float> xMapPosSR, yMapPosSR;
                    xMapPosSR = xMapPos * rCosV - yMapPos * rSinV;
                    yMapPosSR = xMapPos * rSinV + yMapPos * rCosV;

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos * yScaleV);
                Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos * xScaleV);

                _y1 = (_y1 + xOffSetV) & textureHeightMaskV;
                _x1 = (_x1 + yOffSetV) & textureWidthMaskV;

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
            params FloorSpriteWallInfo[] spriteBounds)
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

        private static (int from, int to) DetermineBounds(params FloorSpriteWallInfo[] spriteBounds)
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

        private static (Point TopLeft, Point TopRight, Point BottomLeft, Point BottomRight) GetSpriteBoundingBox(
            PortalPlayerSnapshot player,
            RenderableSprite sprite)
        {
            float pSin = player.Sin;
            float pCos = player.Cos;
            float px = player.X;
            float py = player.Y;

            float xScale = sprite.Texture.XScale ?? 1f;
            float yScale = sprite.Texture.YScale ?? 1f;

            TextureInfo textureInfo = sprite.Texture;
            Texture texture = TextureCache.GetTexture(textureInfo.Name);
            float halfWidth = texture.Height * 0.5f * xScale;
            float halfHeight = texture.Width * 0.5f * yScale;

            (float rx, float ry) = sprite.Location;
            float xFrom = rx - halfWidth;
            float xTo = rx + halfWidth;
            float yFrom = ry - halfHeight;
            float yTo = ry + halfHeight;

            Point a = SharedHelpers.RotateVertex(xFrom, yTo, pSin, pCos, px, py);
            Point b = SharedHelpers.RotateVertex(xTo, yTo, pSin, pCos, px, py);
            Point c = SharedHelpers.RotateVertex(xFrom, yFrom, pSin, pCos, px, py);
            Point d = SharedHelpers.RotateVertex(xTo, yFrom, pSin, pCos, px, py);

            return (a, b, c, d);
        }

        private void DrawTransparentWall(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo textureInfo = wall.MiddleTexture!;

            if (textureInfo.XScale is not null)
            {
                DrawTransparentWall_Build(sectors, renderableWall);
                return;
            }

            Sector sector = wall.Sector;
            int width = PixelWidth;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            float sectorHeight = sector.Ceil - sector.Floor;

            Texture texture = TextureCache.GetTexture(textureInfo);
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            ref uint columnBufferPtr = ref GetBufferA(textureWidth, out Span<uint> columnBuffer);

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            Sector neighborSector = sectors[wall.Neighbor];
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;

            float oneOverSectorHeight = 1f / sectorHeight;

            bool renderFromTop = textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromTop);
            byte lightLevel = sector.LightLevel;
            float alpha = Math.Clamp(textureInfo.Alpha, 0f, 1f);

            if (floorOffset < 0f)
            {
                floorOffset = 0f;
            }

            if (ceilOffset > 0f)
            {
                ceilOffset = 0f;
            }

            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset > sectorHeight ? textureInfo.YOffset - 65536 : textureInfo.YOffset;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus columnStatus = renderableWall.ColumnStatus[x];

                if (columnStatus.PortalRenderable)
                {
                    continue;
                }

                float buffer = renderableWall.Distance[x];
                int floorEnd = Math.Min(renderableWall.FloorEnd[x], renderableWall.WallEnd[x]);
                int ceilingStart = renderableWall.CeilingStart[x];

                (float distance, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x, false);

                if (fromToYdist > buffer)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                float pixelsPerUnit = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                float floorPixelOffset = pixelsPerUnit * floorOffset;
                float ceilPixelOffset = pixelsPerUnit * ceilOffset;
                float portalFromY = wallStartY - ceilPixelOffset;
                float portalToY = wallEndY - floorPixelOffset;

                float textureStartY = renderFromTop ? portalFromY : (portalToY - texture.Height * pixelsPerUnit);
                float textureEndY = renderFromTop ? (portalFromY + texture.Height * pixelsPerUnit) : portalToY;

                if (yOffset != 0)
                {
                    float yOffsetF = yOffset * pixelsPerUnit;

                    if (yOffset > 0)
                    {
                        textureStartY = renderFromTop ? textureStartY + yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY + yOffsetF : textureEndY - yOffsetF;
                    }
                    else
                    {
                        textureStartY = renderFromTop ? textureStartY - yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY - yOffsetF : textureEndY - yOffsetF;
                    }
                }

                int textureStartYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureStartY), ceilingStart, floorEnd);
                int textureEndYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureEndY), ceilingStart, floorEnd);

                if (textureStartYClamped >= textureEndYClamped)
                {
                    continue;
                }

                float offset = textureStartYClamped - textureStartY;

                // Calculate Middle Texture Position
                float textureXIncr = (float)(sectorHeight / (wallEndY - wallStartY));
                int textureYPos = ((float.ConvertToIntegerNative<int>(distance) + xOffset) % textureHeight) * textureWidth;

                float textureXPos = MathF.FusedMultiplyAdd(textureXIncr, offset, textureWidth);

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel, false);

                if (alpha == 1f)
                {
                    DrawTransparentWallLine(width, x,
                        textureStartYClamped, textureEndYClamped,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref columnBufferPtr);
                }
                else
                {
                    DrawTransparentWallLineWithAlpha(width, x,
                        textureStartYClamped, textureEndYClamped,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref columnBufferPtr,
                        alpha);
                }
            }

            columnABufferIndex = EngineConstants.Unset;
        }

        private void DrawTransparentWall_Build(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            ReadOnlySpan<int> floorEnd = renderableWall.FloorEnd;
            ReadOnlySpan<int> ceilingStart = renderableWall.CeilingStart;
            ReadOnlySpan<float> distance = renderableWall.Distance;
            ReadOnlySpan<RenderColumnStatus> columnStatus = renderableWall.ColumnStatus;

            int width = PixelWidth;
            RenderableWall wall = renderableWall.Wall;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Sector sector = wall.Sector;
            float sectorHeight = sector.Ceil - sector.Floor;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            TextureInfo textureInfo = wall.MiddleTexture!;
            Texture texture = TextureCache.GetTexture(textureInfo);
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            ref uint columnBufferPtr = ref GetBufferA(textureWidth, out Span<uint> columnBuffer);

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            Sector neighborSector = sectors[wall.Neighbor];
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;

            float oneOverSectorHeight = 1f / sectorHeight;

            byte lightLevel = sector.LightLevel;
            float alpha = Math.Clamp(textureInfo.Alpha, 0f, 1f);

            if (floorOffset < 0f)
            {
                floorOffset = 0f;
            }

            if (ceilOffset > 0f)
            {
                ceilOffset = 0f;
            }

            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;

            (_, bool flipX, bool flipY) = GetFlags(textureInfo);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            (float xScale, float yScale) = (textureInfo.XScale!.Value, textureInfo.YScale!.Value);
            yScale = (sector.Ceil - sector.Floor) * yScale;
            xScale = xScale / wall.Length * texture.Width;

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus columnStatusY = columnStatus[x];

                if (columnStatusY.PortalRenderable)
                {
                    continue;
                }

                float buffer = distance[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];

                (float distanceY, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x, flipX);

                if (fromToYdist > buffer)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                float pixelsPerUnit = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                float floorPixelOffset = pixelsPerUnit * floorOffset;
                float ceilPixelOffset = pixelsPerUnit * ceilOffset;
                float textureFromY = wallStartY - ceilPixelOffset;
                float textureToY = wallEndY - floorPixelOffset;

                // Clamp to View Window
                int clampedFromY = Math.Clamp(float.ConvertToIntegerNative<int>(textureFromY), ceilingStartY, floorEndY);
                int clampedToY = Math.Clamp(float.ConvertToIntegerNative<int>(textureToY), ceilingStartY, floorEndY);

                if (clampedFromY >= clampedToY)
                {
                    continue;
                }

                // Calculate Middle Texture Position
                int textureYPos = float.ConvertToIntegerNative<int>(distanceY * xScale);
                textureYPos = ((textureYPos + xOffset) % textureHeight) * textureWidth;

                float textureXIncr = (textureWidth * yScale) / (wallEndY - wallStartY);
                float textureXPos = yOffset - textureXIncr * (wallStartY - clampedFromY);

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel, flipY);

                if (alpha == 1f)
                {
                    DrawTransparentWallLine(width, x,
                        clampedFromY, clampedToY,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref columnBufferPtr);
                }
                else
                {
                    DrawTransparentWallLineWithAlpha(width, x,
                        clampedFromY, clampedToY,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref columnBufferPtr,
                        alpha);
                }
            }

            columnABufferIndex = EngineConstants.Unset;
        }

        private static void DrawTransparentWallLine(
            int width,
            int x,
            int textureStartYClamped, int textureEndYClamped,
            int textureHeight,
            float textureXPos,
            float textureXIncr,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
            )
        {
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, textureStartYClamped * width + x);
            ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, textureEndYClamped * width + x);
            uint textureXPos_u = float.ConvertToIntegerNative<uint>(textureXPos * (1 << 16));
            uint textureXIncr_u = float.ConvertToIntegerNative<uint>(textureXIncr * (1 << 16));
            uint textureHeight_u = (uint)textureHeight;

            while (Unsafe.IsAddressLessThan(ref screenIndexPtr, ref screenIndexPtrEnd))
            {
                uint texelIndex = (textureXPos_u >> 16) % textureHeight_u;
                uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                if (shaded != 0U)
                    screenIndexPtr = shaded;

                screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                textureXPos_u += textureXIncr_u;
            }
        }

        private static void DrawTransparentWallLineWithAlpha(
            int width,
            int x,
            int textureStartYClamped, int textureEndYClamped,
            int textureHeight,
            float textureXPos,
            float textureXIncr,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer,
            float alpha
            )
        {
            uint a = float.ConvertToIntegerNative<uint>(alpha * byte.MaxValue);
            uint aInv = byte.MaxValue - a;

            ref BGRA screenIndexPtrBgra = ref Unsafe.As<uint, BGRA>(ref screenPtr);
            ref BGRA screenIndexPtr = ref Unsafe.Add(ref screenIndexPtrBgra, textureStartYClamped * width + x);
            ref readonly BGRA screenIndexPtrEnd = ref Unsafe.Add(ref screenIndexPtrBgra, textureEndYClamped * width + x);

            uint textureXPos_u = float.ConvertToIntegerNative<uint>(textureXPos * (1 << 16));
            uint textureXIncr_u = float.ConvertToIntegerNative<uint>(textureXIncr * (1 << 16));
            uint textureHeight_u = (uint)textureHeight;

            while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
            {
                uint texelIndex = (textureXPos_u >> 16) % textureHeight_u;
                BGRA shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                if (shaded != 0U)
                {
                    screenIndexPtr = BlendBGRA(ref screenIndexPtr, ref shaded, a, aInv);
                }

                screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                textureXPos_u += textureXIncr_u;
            }
        }


        private static void DrawSpriteLine(
                int width,
                int x,
                int textureStartYClamped, int textureEndYClamped,
                int textureXPos,
                int textureXIncr,
                scoped ref uint screenPtr,
                scoped ref uint textureBuffer
                )
        {
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, textureStartYClamped * width + x);
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, textureEndYClamped * width + x);
            uint textureXPos_u = (uint)(textureXPos);
            uint textureXIncr_u = (uint)(textureXIncr);

            while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
            {
                uint texelIndex = textureXPos_u >> 16;
                uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                if (shaded != 0U)
                    screenIndexPtr = shaded;

                screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                textureXPos_u += textureXIncr_u;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint BlendBGRA(ref readonly BGRA bgraDst, ref readonly BGRA bgraSrc, uint a, uint aInv)
        {
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
        private static void CalculateSprite(
            scoped Span<uint> spriteTexturePtr,
            ref int bufferIndex,
            ref BGRA wallTexturePtr,
            int textureYPos,
            byte brightness,
            bool flipY)
        {
            // reuse the cached column
            if (bufferIndex == textureYPos)
            {
                return;
            }

            const uint Alpha = (uint)byte.MaxValue << 24;

            bufferIndex = textureYPos;

            ref BGRA columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);
            uint scale = (uint)brightness;

            if (flipY)
            {
                for (int i = spriteTexturePtr.Length - 1; i >= 0; i--)
                {
                    if (columnPtr.IsTransparent)
                    {
                        spriteTexturePtr[i] = default;
                    }
                    else
                    {
                        uint b = columnPtr.B * scale >> 8;
                        uint g = columnPtr.G * scale >> 8 << 8;
                        uint r = columnPtr.R * scale >> 8 << 16;
                        spriteTexturePtr[i] = b | g | r | Alpha;
                    }

                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
            else
            {
                for (int i = 0; i < spriteTexturePtr.Length; i++)
                {
                    if (columnPtr.IsTransparent)
                    {
                        spriteTexturePtr[i] = default;
                    }
                    else
                    {
                        uint b = columnPtr.B * scale >> 8;
                        uint g = columnPtr.G * scale >> 8 << 8;
                        uint r = columnPtr.R * scale >> 8 << 16;
                        spriteTexturePtr[i] = b | g | r | Alpha;
                    }

                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
        }
    }
}
