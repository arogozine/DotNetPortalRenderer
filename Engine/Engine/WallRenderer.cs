using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private bool DrawPortalWall(
            Span<BGRA> screen,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            RenderableWall renderableWall)
        {
            int width = PixelWidth;
            var wall = renderableWall.Wall;
            var line = wall.Line;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            float sectorHeight = sector.Ceil - sector.Floor;
            int yOffset = line.YOffset;
            int xOffset = line.XOffset;

            WallYPlaneInfo yPlaneInfo = CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            ref Texture lowerTexture = ref TextureCache.GetTexture(line.LowerTexture ?? line.MiddleTexture);
            ref BGRA lowerTexturePtr = ref MemoryMarshal.GetArrayDataReference(lowerTexture.Rotated);

            ref Texture upperTexture = ref TextureCache.GetTexture(line.UpperTexture ?? line.MiddleTexture);
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetArrayDataReference(upperTexture.Rotated);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);
           
            Sector neighborSector = sectors[wall.Neighbor];
            float oneOverSectorHeight = 1f / sectorHeight;
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;

            if (floorOffset == 0 && ceilOffset == 0)
            {
                return true;
            }

            if (floorOffset < 0f)
            {
                floorOffset = 0f;
            }

            if (ceilOffset > 0f)
            {
                ceilOffset = 0f;
            }

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (Unsafe.IsNullRef(ref renderWindow))
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                (int distance, float brightness, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Wall Calculation
                int fromYClamped = renderWindow.WallStart; // (int)Math.Clamp(wallStartY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int toYClamped = renderWindow.WallEnd; // (int)Math.Clamp(wallEndY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                // Portal Calculation
                float floorPixelOffset = pixelsPerHeight * floorOffset;
                float ceilPixelOffset = pixelsPerHeight * ceilOffset;
                float portalFromY = wallStartY - ceilPixelOffset;
                float portalToY = wallEndY - floorPixelOffset;
                int portalFromYClamped = (int)Math.Clamp(portalFromY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int portalToYClamped = (int)Math.Clamp(portalToY, renderWindow.CeilingStart, renderWindow.FloorEnd);


                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, fromYClamped * PixelWidth + x);

                // Calculate Upper  Texture Position
                int textureWidth = upperTexture.Height;
                int textureHeight = upperTexture.Width;
                float textureXIncr = (float)((sectorHeight - 1) / (wallEndY - wallStartY));
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                float textureXPos = yOffset - textureXIncr * (wallStartY - fromYClamped);

                uint shaded = default;
                int textureXPosIOld = -1;

                for (int y = fromYClamped; y < portalFromYClamped; ++y)
                {
                    int textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
                        textureXPosI %= textureWidth;

                        textureXPosIOld = textureXPosI;
                        ref BGRA textureIndexPtr = ref Unsafe.Add(ref upperTexturePtr, textureYPos + textureXPosI);
                        shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                    }

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth);
                    textureXPos += textureXIncr;
                }

                screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToYClamped * PixelWidth + x);

                textureWidth = lowerTexture.Height;
                textureHeight = lowerTexture.Width;

                textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                textureXPos = textureXIncr * (portalToYClamped - portalToY);

                shaded = default;
                textureXPosIOld = -1;

                for (int y = portalToYClamped; y < toYClamped; ++y)
                {
                    int textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
                        textureXPosI %= textureWidth;
                        textureXPosIOld = textureXPosI;
                        ref BGRA textureIndexPtr = ref Unsafe.Add(ref lowerTexturePtr, textureYPos + textureXPosI);
                        shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                    }

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth);
                    textureXPos += textureXIncr;
                }

                renderWindow.Distance = fromToYdist;
                renderWindow.Calculated = false;
                renderWindow.CeilingStart = portalFromYClamped;
                renderWindow.FloorEnd = portalToYClamped;
                renderWindow.WallStart = portalFromYClamped;
                renderWindow.WallEnd = portalToYClamped;

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }

            return true;
        }

        private bool DrawBasicWall(
            Span<BGRA> screen,
            Sector sector,
            RenderableWall renderableWall)
        {
            int width = PixelWidth;
            var wall = renderableWall.Wall;
            var line = wall.Line;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            float sectorHeight = sector.Ceil - sector.Floor;
            int yOffset = line.YOffset;
            int xOffset = line.XOffset;

            float oneOverSectorHeight = 1f / sectorHeight;

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            ref Texture wallTexture = ref TextureCache.GetTexture(line.MiddleTexture);
            ref BGRA wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(wallTexture.Rotated);
            int textureWidth = wallTexture.Height;
            int textureHeight = wallTexture.Width;

            WallYPlaneInfo yPlaneInfo = CalculateLeftWallYPlaneInfo(renderableWall.Wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);
            
            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (Unsafe.IsNullRef(ref renderWindow))
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                (int distance, float brightness, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                int clamptedFromY = renderWindow.WallStart;
                int clamptedToY = renderWindow.WallEnd;

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);

                // texture is rotated - y position is x position in texture
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                float textureXIncr = (sectorHeight - 1) / (wallEndY - wallStartY);
                float textureXPos = yOffset - textureXIncr * (wallStartY - clamptedFromY);

                uint shaded = default;
                int textureXPosIOld = -1;

                for (int y = clamptedFromY; y <= clamptedToY; ++y)
                {
                    int textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
                        textureXPosI %= textureWidth;
                        textureXPosIOld = textureXPosI;

                        ref BGRA textureIndexPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos + textureXPosI);
                        shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                    }

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos += textureXIncr;   
                }

                renderWindow.Distance = fromToYdist;
                renderWindow.WallEnd = renderWindow.WallStart;
                renderWindow.FloorEnd = renderWindow.WallStart;
                renderWindow.Calculated = false;

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static WallYPlaneInfo CalculateLeftWallYPlaneInfo(Wall wall, int wallFromXOffset)
        {
            float wallStartY = wall.YLeftCeil;
            float ceilDistIncr = (wall.YRightCeil - (float)wall.YLeftCeil) / (wall.XRight - wall.XLeft);

            float wallEndY = wall.YLeftFloor;
            float floorDistIncr = (wall.YRightFloor - (float)wall.YLeftFloor) / (wall.XRight - wall.XLeft);

            if (wallFromXOffset != 0)
            {
                wallEndY += wallFromXOffset * floorDistIncr;
                wallStartY += wallFromXOffset * ceilDistIncr;
            }

            return new WallYPlaneInfo(wallStartY, wallEndY, ceilDistIncr, floorDistIncr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (float CameraRay, float CameraRayIncr, float t1, float d2y, float d2x) CalculateCameraRay(Wall wall, int width, int wallFromX)
        {
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;
            float rx1 = wall.R1.X;
            float ry1 = wall.R1.Y;
            float d2x = wall.R2.X - rx1;
            float d2y = wall.R2.Y - ry1;
            float t1 = rx1 * d2y - ry1 * d2x;
            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * wallFromX;

            return (cameraRay, cameraWidthIncr, t1, d2y, d2x);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int TextureLocation, float TextureBrightness, float FromToYDist) CalculateDistance(Wall wall, float cameraRay, float t1, float d2y, float d2x)
        {
            bool flipped = wall.Flipped;

            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;
            float fromToXDist = fromToYDist * cameraRay;

            float distX = (flipped ? wall.R2.X : wall.R1.X) - fromToXDist;
            float distY = (flipped ? wall.R2.Y: wall.R1.Y) - fromToYDist;

            float textureXLocation = MathF.Sqrt(distX * distX + distY * distY);
            float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * fromToYDist;

            return ((int)textureXLocation, brightness, fromToYDist);
        }
    }
}
