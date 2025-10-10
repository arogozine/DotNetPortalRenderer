using RenderingEngine.Models;
using RenderingEngine.TextureManagement;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private bool DrawPortalWall(
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            Span<BGRA> screen,
            TextureInfo wallTexture,
            RenderableWall renderableWall)
        {
            var line = renderableWall.Wall.Line;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            var wall = renderableWall.Wall;

            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);

            if (line.MiddleTexture is not null)
            {
                wallTexture = TextureCache.GetTexture(line.MiddleTexture, true);
            }

            TextureInfo upperTexture = wallTexture;
            TextureInfo lowerTexture = wallTexture;

            if (line.LowerTexture is not null)
            {
                lowerTexture = TextureCache.GetTexture(line.LowerTexture, true);
            }

            if (line.UpperTexture is not null)
            {
                upperTexture = TextureCache.GetTexture(line.UpperTexture, true);
            }
            

            // wall plane
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            int width = PixelWidth;
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            // for player camera position / ray
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
            float sectorHeight = 1f / (sector.Ceil - sector.Floor);
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;

            if (floorOffset == 0 && ceilOffset == 0)
            {
                return true;
            }

            ref BGRA wallTexturePtr = ref wallTexture.Texture;
            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderWindow result = RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (!result.Calculated)
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

                float distX = rx1 - fromToXDist;
                float distY = ry1 - fromToYDist;
                float distance = MathF.Sqrt(distX * distX + distY * distY);

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * fromToYDist;

                {
                    int textureWidth = upperTexture.Width;
                    int textureHeight = upperTexture.Height;
                    wallTexturePtr = ref upperTexture.Texture;

                    float pixelsPerHeight = (wallEndYInt - wallStartYInt) * sectorHeight;
                    int floorPixelOffset = (int)(pixelsPerHeight * floorOffset);
                    int ceilPixelOffset = (int)(pixelsPerHeight * ceilOffset);

                    // neighbor ?
                    int fromYN = wallStartYInt - ceilPixelOffset;
                    int toYN = wallEndYInt - floorPixelOffset;
                    fromYN = Math.Clamp(fromYN, portalFromY, portalToY);
                    toYN = Math.Clamp(toYN, portalFromY, portalToY);

                    portalFromY = Math.Max(fromYN, clamptedFromY);
                    portalToY = Math.Min(toYN, clamptedToY);

                    float textureXIncr = (float)textureWidth / (wallEndYInt - wallStartYInt);
                    int textureYPos = ((int) distance) % textureHeight;

                    ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * PixelWidth + x);
                    float textureXPos = textureXIncr * Math.Abs(wallStartYInt - clamptedFromY);
                    int textureYPosI = textureYPos * textureWidth;

                    uint shaded = default;
                    int textureXPosIOld = -1;

                    for (int y = clamptedFromY; y < portalFromY; ++y)
                    {
                        int textureXPosI = (int)textureXPos;

                        if (textureXPosI != textureXPosIOld)
                        {
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
                    wallTexturePtr = ref lowerTexture.Texture;

                    textureXIncr = (float)textureWidth / (wallEndYInt - wallStartYInt);
                    textureYPos = ((int)distance) % textureHeight;
                    textureYPosI = textureYPos * textureWidth;

                    screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToY * PixelWidth + x);
                    textureXPos = textureXIncr * Math.Abs(wallStartYInt - portalToY);
                    shaded = default;
                    textureXPosIOld = -1;

                    for (int y = portalToY; y < clamptedToY; ++y)
                    {
                        int textureXPosI = (int)textureXPos;

                        if (textureXPosI != textureXPosIOld)
                        {
                            textureXPosIOld = textureXPosI;
                            ref BGRA textureIndexPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPosI + textureXPosI);
                            shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                        }

                        screenIndexPtr = shaded;
                        screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth);
                        textureXPos += textureXIncr;
                    }
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
            TextureInfo wallTexture,
            RenderableWall renderableWall)
        {
            var line = renderableWall.Wall.Line;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(renderableWall.Wall, wallFromXOffset);

            if (line.MiddleTexture is not null)
            {
                wallTexture = TextureCache.GetTexture(line.MiddleTexture, true);
            }

            var wall = renderableWall.Wall;

            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            int width = PixelWidth;
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            // for player camera position / ray
            float rx1 = wall.X1;
            float ry1 = wall.Y1;
            float rx2 = wall.X2;
            float ry2 = wall.Y2;
            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;
            float t1 = rx1 * d2y - ry1 * d2x;
            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * wallFromX;

            ref BGRA wallTexturePtr = ref wallTexture.Texture;
            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            int textureWidth = wallTexture.Width;
            int textureHeight = wallTexture.Height;

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderWindow result = RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (!result.Calculated)
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

                float distX = rx1 - fromToXDist;
                float distY = ry1 - fromToYDist;
                float distance = MathF.Sqrt(distX * distX + distY * distY);

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * fromToYDist;

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * PixelWidth + x);
                int textureYPos = ((int) distance) % textureHeight;
                float textureXIncr = (float)textureWidth / (wallEndYInt - wallStartYInt);
                float textureXPos = textureXIncr * Math.Abs(wallStartYInt - clamptedFromY);
                int textureYPosI = textureYPos * textureWidth;

                uint shaded = default;
                int textureXPosIOld = -1;

                for (int y = clamptedFromY; y <= clamptedToY; ++y)
                {
                    int textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
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
