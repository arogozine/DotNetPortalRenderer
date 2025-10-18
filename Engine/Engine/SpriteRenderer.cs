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

            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
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

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                (int distance, float brightness) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                /*
                if (distance > zBuffer[x] && zBuffer[x] != 0)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }
                */
                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;

                float pixelsPerUnit = (wallEndYInt - wallStartYInt) * oneOverSectorHeight;
                // int floorPixelOffset = (int)(pixelsPerUnit * floorOffset);
                // int ceilPixelOffset = (int)(pixelsPerUnit * ceilOffset);

                // wallStartYInt = wallStartYInt - ceilPixelOffset;
                // wallEndYInt = wallEndYInt - floorPixelOffset;

                // re-calculate un-clamped middle portal
                int fromY = (int)(wallEndYInt - texture.Height * pixelsPerUnit);
                int toY = wallEndYInt;

                int offset = fromY < 0 ? -fromY : 0;

                // clamp to render window
                ref RenderWindow renderWindow = ref window[x];
                fromY = Math.Max(renderWindow.WallStart, fromY);
                toY = Math.Min(renderWindow.WallEnd, toY);

                // texture
                float textureXIncr = (float)sectorHeight / (wallEndYInt - wallStartYInt);
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                float textureXPos = textureXIncr * offset;

                uint shaded = default;
                int textureXPosIOld = -1;

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, fromY * PixelWidth + x);
                ref BGRA textureIndexPtr = ref texturePtr;

                for (int y = fromY; y < toY; y++, textureXPos += textureXIncr)
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
