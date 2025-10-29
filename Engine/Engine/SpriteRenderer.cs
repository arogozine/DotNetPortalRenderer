using RenderingEngine.Models;

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
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            Span<uint> columnBuffer = this.columnA.AsSpan(..textureWidth);
            ref uint columnBufferPtr = ref MemoryMarshal.GetReference(columnBuffer);

            WallYPlaneInfo yPlaneInfo = CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            Sector neighborSector = sectors[wall.Neighbor];
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;

            float oneOverSectorHeight = 1f / sectorHeight;


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

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                ref RenderWindow renderWindow = ref window[x];

                if (renderWindow.Calculated || renderWindow.FloorEnd <= renderWindow.CeilingStart)
                {
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
                int portalFromYClamped = Math.Clamp((int)portalFromY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int portalToYClamped = Math.Clamp((int)portalToY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int textureStartYClamped = Math.Clamp((int)textureStartY, renderWindow.CeilingStart, renderWindow.FloorEnd);

                float offset = textureStartYClamped - textureStartY;

                // Calculate Middle Texture Position
                float textureXIncr = (float)((sectorHeight - 1) / (wallEndY - wallStartY));
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                float textureXPos = textureWidth + textureXIncr * offset;

                uint shaded = default;
                int textureXPosIOld = -1;

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, brightness);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, textureStartYClamped * PixelWidth + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, portalToYClamped * PixelWidth + x);

                for (;Unsafe.IsAddressGreaterThan(ref screenIndexPtrEnd, ref screenIndexPtr);
                    textureXPos += textureXIncr, screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth))
                {
                    int textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
                        if (textureXPosI >= textureWidth)
                        {
                            textureXPosI -= textureWidth;
                        }

                        textureXPosIOld = textureXPosI;
                        shaded = Unsafe.Add(ref columnBufferPtr, textureXPosI);
                    }

                    if (shaded != 0U)
                    {
                        screenIndexPtr = shaded;
                    }
                }
            }

            columnABufferIndex = EngineConstants.Unset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateSprite(Span<uint> spriteTexturePtr, ref int bufferIndex, ref BGRA wallTexturePtr, int textureYPos, float brightness)
        {
            // reuse the cached column
            if (bufferIndex == textureYPos)
            {
                return;
            }

            const uint Alpha = (uint)byte.MaxValue << 24;

            bufferIndex = textureYPos;

            // avoid calculating if too far away (all black)
            if (brightness <= 0)
            {
                spriteTexturePtr.Fill(Alpha);
                return;
            }

            ref BGRA columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);
            uint scale = (uint)(brightness * 255f);

            for (int i = 0; i < spriteTexturePtr.Length; i++)
            {
                if (columnPtr.IsTransparent)
                {
                    spriteTexturePtr[i] = default;
                }
                else
                {
                    unchecked
                    {
                        uint b = columnPtr.B * scale >> 8;
                        uint g = columnPtr.G * scale >> 8 << 8;
                        uint r = columnPtr.R * scale >> 8 << 16;
                        spriteTexturePtr[i] = b | g | r | Alpha;
                    }
                }

                columnPtr = ref Unsafe.Add(ref columnPtr, 1);
            }

            return;
        }
    }
}
