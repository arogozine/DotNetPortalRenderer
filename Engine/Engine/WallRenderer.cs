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

            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
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

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (Unsafe.IsNullRef(ref renderWindow))
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                (int distance, float brightness) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;
                int portalFromY = renderWindow.CeilingStart;
                int portalToY = renderWindow.FloorEnd;
                int clamptedFromY = Math.Max(renderWindow.WallStart, wallStartYInt);
                int clamptedToY = Math.Min(renderWindow.WallEnd, wallEndYInt);

                int textureWidth = upperTexture.Height;
                int textureHeight = upperTexture.Width;

                float pixelsPerHeight = (wallEndYInt - wallStartYInt) * oneOverSectorHeight;
                int floorPixelOffset = (int)(pixelsPerHeight * floorOffset);
                int ceilPixelOffset = (int)(pixelsPerHeight * ceilOffset);

                // neighbor ?
                int fromYN = wallStartYInt - ceilPixelOffset;
                int toYN = wallEndYInt - floorPixelOffset;
                fromYN = Math.Clamp(fromYN, portalFromY, portalToY);
                toYN = Math.Clamp(toYN, portalFromY, portalToY);

                portalFromY = Math.Max(fromYN, clamptedFromY);
                portalToY = Math.Min(toYN, clamptedToY);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * PixelWidth + x);

                float textureXIncr = (float)sectorHeight / (wallEndYInt - wallStartYInt);

                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                float textureXPos = textureWidth + yOffset - textureXIncr * (wallStartYInt - clamptedFromY);

                uint shaded = default;
                int textureXPosIOld = -1;

                for (int y = clamptedFromY; y < portalFromY; ++y)
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

                textureWidth = lowerTexture.Height;
                textureHeight = lowerTexture.Width;

                screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToY * PixelWidth + x);

                textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                textureXPos = textureXIncr * (floorPixelOffset - wallEndYInt);
                textureXPos = textureXPos > 0f ? textureXPos : 0f;

                shaded = default;
                textureXPosIOld = -1;

                for (int y = portalToY; y < clamptedToY; ++y)
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
                
                renderWindow.Calculated = false;
                renderWindow.CeilingStart = portalFromY;
                renderWindow.FloorEnd = portalToY;
                renderWindow.WallStart = portalFromY;
                renderWindow.WallEnd = portalToY;

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

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            ref Texture wallTexture = ref TextureCache.GetTexture(line.MiddleTexture);
            ref BGRA wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(wallTexture.Rotated);
            int textureWidth = wallTexture.Height;
            int textureHeight = wallTexture.Width;

            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(renderableWall.Wall, wallFromXOffset);
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

                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;

                int clamptedFromY = renderWindow.WallStart;
                int clamptedToY = renderWindow.WallEnd;

                (int distance, float brightness) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);

                // texture is rotated - y position is x position in texture
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                float textureXIncr = sectorHeight / (wallEndYInt - wallStartYInt);
                float textureXPos = textureWidth + yOffset - textureXIncr * (wallStartYInt - clamptedFromY);

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

                renderWindow.WallEnd = renderWindow.WallStart;
                renderWindow.FloorEnd = renderWindow.WallStart;
                renderWindow.Calculated = false;

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (float CameraRay, float CameraRayIncr, float t1, float d2y, float d2x) CalculateCameraRay(Wall wall, int width, int wallFromX)
        {
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;
            float rx1 = wall.X1;
            float ry1 = wall.Y1;
            float d2x = wall.X2 - rx1;
            float d2y = wall.Y2 - ry1;
            float t1 = rx1 * d2y - ry1 * d2x;
            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * wallFromX;

            return (cameraRay, cameraWidthIncr, t1, d2y, d2x);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int Distance, float Brightness) CalculateDistance(Wall wall, float cameraRay, float t1, float d2y, float d2x)
        {
            bool flipped = wall.Flipped;

            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;
            float fromToXDist = fromToYDist * cameraRay;

            float distX = (flipped ? wall.X2 : wall.X1) - fromToXDist;
            float distY = (flipped ? wall.Y2 : wall.Y1) - fromToYDist;

            float distance = MathF.Sqrt(distX * distX + distY * distY);
            float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * fromToYDist;

            return ((int)distance, brightness);
        }
    }
}
