using RenderingEngine.Models;
using RenderingEngine.Models.Json;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void DrawTransparentWall(
            Span<BGRA> screen,
            ReadOnlySpan<Sector> sectors,
            RenderableWall renderableWall)
        {
            int width = PixelWidth;
            int height = PixelHeight;
            Wall wall = renderableWall.Wall;
            Line line = wall.Line;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Sector sector = renderableWall.Sector;
            float sectorHeight = sector.Ceil - sector.Floor;
            int yOffset = line.YOffset;
            int xOffset = line.XOffset;
            Span<RenderWindow> window = renderableWall.RenderWindow!;

            ref Texture texture = ref TextureCache.GetTexture(line.MiddleTexture);
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);

            WallYPlaneInfo yPlaneInfo = CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            Sector neighborSector = sectors[wall.Neighbor];
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;

            float oneOverSectorHeight = 1f / sectorHeight;

            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            if (floorOffset < 0f)
            {
                floorOffset = 0f;
            }

            if (ceilOffset > 0f)
            {
                ceilOffset = 0f;
            }

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref window[x];

                if (renderWindow.Calculated)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                float buffer = renderWindow.Distance;

                (int distance, float brightness, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                if (fromToYdist > buffer)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                float pixelsPerUnit = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Wall Calculation
                int fromYClamped = renderWindow.WallStart;
                int toYClamped = renderWindow.WallEnd;

                // Portal Calculation
                float floorPixelOffset = pixelsPerUnit * floorOffset;
                float ceilPixelOffset = pixelsPerUnit * ceilOffset;
                float portalFromY = wallStartY - ceilPixelOffset;
                float portalToY = wallEndY - floorPixelOffset;

                float textureStartY = portalToY - texture.Height * pixelsPerUnit;

                // clamp to view window
                int portalFromYClamped = (int)Math.Clamp(portalFromY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int portalToYClamped = (int)Math.Clamp(portalToY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int textureStartYClamped = (int)Math.Clamp(textureStartY, renderWindow.CeilingStart, renderWindow.FloorEnd);

                float offset = textureStartYClamped - textureStartY;

                // Calculate Middle Texture Position
                float textureXIncr = (float)((sectorHeight - 1) / (wallEndY - wallStartY));
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                float textureXPos = textureXIncr * offset;

                uint shaded = default;
                int textureXPosIOld = -1;

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, textureStartYClamped * PixelWidth + x);
                ref BGRA textureIndexPtr = ref texturePtr;

                for (int y = textureStartYClamped; y < portalToYClamped; y++, textureXPos += textureXIncr)
                {
                    int textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
                        textureXPosI %= textureWidth;
                        textureXPosIOld = textureXPosI;
                        textureIndexPtr = ref Unsafe.Add(ref texturePtr, textureYPos + textureXPosI);
                        shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                    }

                    if (!textureIndexPtr.IsTransparent)
                    {
                        screenIndexPtr = shaded;
                    }

                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth);
                }

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }
        }
    }
}
