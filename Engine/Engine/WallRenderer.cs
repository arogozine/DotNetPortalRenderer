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
            var wall = renderableWall.Wall;
            var line = wall.Line;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            float sectorHeight = sector.Ceil - sector.Floor;
            int yOffset = line.YOffset;
            int xOffset = line.XOffset;

            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);

            Texture lowerTexture = TextureCache.GetTexture(line.LowerTexture ?? line.MiddleTexture);
            Texture upperTexture = TextureCache.GetTexture(line.UpperTexture ?? line.MiddleTexture);

            // wall plane
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            int width = PixelWidth;
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            // for player camera position / ray
            bool flipped = wall.Flipped;
            float rx1 = wall.X1;
            float ry1 = wall.Y1;
            float rx2 = wall.X2;
            float ry2 = wall.Y2;
            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;
            float t1 = rx1 * d2y - ry1 * d2x;
            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * wallFromX;

            //
            Sector neighborSector = sectors[wall.Neighbor];
            float oneOverSectorHeight = 1f / sectorHeight;
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;

            if (floorOffset == 0 && ceilOffset == 0)
            {
                return true;
            }

            ref BGRA wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(upperTexture.Rotated);
            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow result = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (Unsafe.IsNullRef(ref result))
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;
                int portalFromY = result.CeilingStart;
                int portalToY = result.FloorEnd;
                int clamptedFromY = Math.Max(result.WallStart, wallStartYInt);
                int clamptedToY = Math.Min(result.WallEnd, wallEndYInt);

                float denominator = cameraRay * d2y - d2x;
                float fromToYDist = t1 / denominator;
                float fromToXDist = fromToYDist * cameraRay;

                float distX = (flipped ? rx2 : rx1) - fromToXDist;
                float distY = (flipped ? ry2 : ry1) - fromToYDist;
                float distance = MathF.Sqrt(distX * distX + distY * distY);

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * fromToYDist;

                int textureWidth = upperTexture.Height;
                int textureHeight = upperTexture.Width;

                wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(upperTexture.Rotated);

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
                // texture is rotated - y position is x position in texture
                int textureYPos = ((int)distance + xOffset) % textureHeight;

                float textureXPos = textureWidth + yOffset - textureXIncr * (wallStartYInt - clamptedFromY);
                int textureYPosI = textureYPos * textureWidth;

                uint shaded = default;
                int textureXPosIOld = -1;

                for (int y = clamptedFromY; y < portalFromY; ++y)
                {
                    int textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
                        textureXPosI %= textureWidth;

                        textureXPosIOld = textureXPosI;
                        ref BGRA textureIndexPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPosI + textureXPosI);
                        shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                    }

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth);
                    textureXPos += textureXIncr;
                }

                textureWidth = lowerTexture.Width;
                textureHeight = lowerTexture.Height;

                wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(lowerTexture.Rotated);

                screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToY * PixelWidth + x);

                textureYPos = ((int)distance + xOffset) % textureHeight;

                textureXPos = textureXIncr * (floorPixelOffset - wallEndYInt);
                textureXPos = textureXPos > 0f ? textureXPos : 0f;

                textureYPosI = textureYPos * textureWidth;

                shaded = default;
                textureXPosIOld = -1;

                for (int y = portalToY; y < clamptedToY; ++y)
                {
                    int textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
                        textureXPosI %= textureWidth;
                        textureXPosIOld = textureXPosI;
                        ref BGRA textureIndexPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPosI + textureXPosI);
                        shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                    }

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth);
                    textureXPos += textureXIncr;
                }
                
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];
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
            var wall = renderableWall.Wall;
            var line = wall.Line;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            float sectorHeight = sector.Ceil - sector.Floor;
            int yOffset = line.YOffset;
            int xOffset = line.XOffset;

            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(renderableWall.Wall, wallFromXOffset);
            Texture wallTexture = TextureCache.GetTexture(line.MiddleTexture);

            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            int width = PixelWidth;
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            // for player camera position / ray
            bool flipped = wall.Flipped;
            float rx1 = wall.X1;
            float ry1 = wall.Y1;
            float rx2 = wall.X2;
            float ry2 = wall.Y2;
            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;
            float t1 = rx1 * d2y - ry1 * d2x;
            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * wallFromX;

            ref BGRA wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(wallTexture.Rotated);
            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            int textureWidth = wallTexture.Height;
            int textureHeight = wallTexture.Width;

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow result = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (Unsafe.IsNullRef(ref result))
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;

                int clamptedFromY = result.WallStart;
                int clamptedToY = result.WallEnd;

                float denominator = cameraRay * d2y - d2x;
                float fromToYDist = t1 / denominator;
                float fromToXDist = fromToYDist * cameraRay;

                float distX = (flipped ? rx2 : rx1) - fromToXDist;
                float distY = (flipped ? ry2 : ry1) - fromToYDist;
                float distance = MathF.Sqrt(distX * distX + distY * distY);

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * fromToYDist;

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * PixelWidth + x);

                // texture is rotated - y position is x position in texture
                int textureYPos = ((int)distance + xOffset) % textureHeight;
                float textureXIncr = sectorHeight / (wallEndYInt - wallStartYInt);
                float textureXPos = textureWidth + yOffset - textureXIncr * (wallStartYInt - clamptedFromY);


                int textureYPosI = textureYPos * textureWidth;

                uint shaded = default;
                int textureXPosIOld = -1;
                int mask = textureWidth - 1;

                for (int y = clamptedFromY; y <= clamptedToY; ++y)
                {
                    int textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
                        textureXPosI %= textureWidth;
                        textureXPosIOld = textureXPosI;

                        ref BGRA textureIndexPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPosI + textureXPosI);
                        shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                    }

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth);
                    textureXPos += textureXIncr;   
                }

                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];
                renderWindow.WallEnd = renderWindow.WallStart;
                renderWindow.FloorEnd = renderWindow.WallStart;
                renderWindow.Calculated = false;

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }

            return true;
        }
    }
}
