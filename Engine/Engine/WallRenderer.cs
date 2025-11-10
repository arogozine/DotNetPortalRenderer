using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        // pre-computed texture buffers
        private int columnABufferIndex = -1;
        private readonly uint[] columnA = new uint[256];
        private int columnBBufferIndex = -1;
        private readonly uint[] columnB = new uint[256];

        private bool DrawPortalWall(
            Span<BGRA> screen,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            RenderableWall renderableWall)
        {
            float sectorHeight = sector.Ceil - sector.Floor;
            var wall = renderableWall.Wall;

            Sector neighborSector = sectors[wall.Neighbor];
            float oneOverSectorHeight = 1f / sectorHeight;
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;
            byte lightLevel = sector.LightLevel;

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

            int width = PixelWidth;
            var line = wall.Line;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int yOffset = line.YOffset;
            int xOffset = line.XOffset;
            bool lowerUnpegged = line.LowerUnpegged;
            bool upperUnpegged = line.UpperUnpegged;

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            ref Texture lowerTexture = ref TextureCache.GetTexture(line.LowerTexture ?? line.MiddleTexture);
            ref BGRA lowerTexturePtr = ref MemoryMarshal.GetArrayDataReference(lowerTexture.Rotated);

            int lowerTextureStart = yOffset + lowerTexture.Height;

            if (lowerUnpegged)
            {
                lowerTextureStart += (int)(sectorHeight - floorOffset);
                lowerTextureStart %= lowerTexture.Height;
            }

            ref Texture upperTexture = ref TextureCache.GetTexture(line.UpperTexture ?? line.MiddleTexture);

            int upperTextureStart = upperTexture.Height + yOffset;

            if (!upperUnpegged) {
                int sectorHeightI = (int)sectorHeight;
                if (sectorHeightI < upperTexture.Height)
                    upperTextureStart = upperTextureStart - (int)sectorHeight % upperTexture.Height;
                else
                    upperTextureStart = 0;
            }

            ref BGRA upperTexturePtr = ref MemoryMarshal.GetArrayDataReference(upperTexture.Rotated);

            Span<uint> lowerTextureBuffer = this.columnA.AsSpan(..lowerTexture.Height);
            ref uint lowerTextureBufferPtr = ref MemoryMarshal.GetReference(lowerTextureBuffer);

            Span<uint> upperTextureBuffer = this.columnB.AsSpan(..upperTexture.Height);
            ref uint upperTextureBufferPtr = ref MemoryMarshal.GetReference(upperTextureBuffer);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (Unsafe.IsNullRef(ref renderWindow))
                {
                    continue;
                }

                (int distance, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                float wallStartY = renderWindow.WallStart;
                float wallEndY = renderWindow.WallEnd;

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Wall Calculation
                int fromYClamped = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int toYClamped = Math.Clamp(renderWindow.WallEnd, renderWindow.CeilingStart, renderWindow.FloorEnd);

                // Portal Calculation
                float floorPixelOffset = pixelsPerHeight * floorOffset;
                float ceilPixelOffset = pixelsPerHeight * ceilOffset;
                float portalFromY = wallStartY - ceilPixelOffset;
                float portalToY = wallEndY - floorPixelOffset;
                int portalFromYClamped = Math.Clamp((int)portalFromY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int portalToYClamped = Math.Clamp((int)portalToY, renderWindow.CeilingStart, renderWindow.FloorEnd);

                float textureXIncr = (float)((sectorHeight - 1) / (wallEndY - wallStartY));

                if (ceilOffset != 0)
                {
                    ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, fromYClamped * width + x);
                    ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, portalFromYClamped * width + x);

                    // Calculate Upper  Texture Position
                    int textureWidth = upperTexture.Height;
                    int textureHeight = upperTexture.Width;
                    int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                    float textureXPos = upperTextureStart - textureXIncr * (wallStartY - fromYClamped);
                    textureXPos %= textureWidth;

                    uint shaded = default;
                    int textureXPosIOld = -1;

                    CalculateAndCacheWallColumn(upperTextureBuffer, ref columnABufferIndex, ref upperTexturePtr, textureYPos, lightLevel);

                    for (;
                        Unsafe.IsAddressGreaterThan(ref screenIndexPtrEnd, ref screenIndexPtr);
                        textureXPos += textureXIncr, screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width)
                        )
                    {
                        int textureXPosI = (int)textureXPos;

                        if (textureXPosI != textureXPosIOld)
                        {
                            // wrap the texture
                            if (textureXPosI >= textureWidth)
                            {
                                textureXPosI -= textureWidth;
                                textureXPos -= textureWidth;
                            }

                            textureXPosIOld = textureXPosI;
                            shaded = Unsafe.Add(ref upperTextureBufferPtr, textureXPosI);
                        }

                        screenIndexPtr = shaded;
                    }
                }

                if (floorOffset != 0)
                {
                    ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToYClamped * PixelWidth + x);
                    ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, toYClamped * width + x);

                    int textureWidth = lowerTexture.Height;
                    int textureHeight = lowerTexture.Width;
                    int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                    float textureXPos = lowerTextureStart + textureXIncr * (portalToYClamped - portalToY);
                    textureXPos %= textureWidth;

                    uint shaded = default;
                    int textureXPosIOld = -1;

                    CalculateAndCacheWallColumn(lowerTextureBuffer, ref columnBBufferIndex, ref lowerTexturePtr, textureYPos, lightLevel);

                    int textureXPosI = -1;

                    for (;
                        Unsafe.IsAddressGreaterThan(ref screenIndexPtrEnd, ref screenIndexPtr);
                        textureXPos += textureXIncr, screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width)
                        )
                    {
                        textureXPosI = (int)textureXPos;

                        if (textureXPosI != textureXPosIOld)
                        {
                            // wrap the texture
                            if (textureXPosI >= textureWidth)
                            {
                                textureXPosI -= textureWidth;
                                textureXPos -= textureWidth;
                            }

                            textureXPosIOld = textureXPosI;

                            shaded = Unsafe.Add(ref lowerTextureBufferPtr, textureXPosI);
                        }

                        screenIndexPtr = shaded;
                    }
                }

                renderWindow.Distance = fromToYdist;
                renderWindow.Calculated = false;
                renderWindow.CeilingStart = portalFromYClamped;
                renderWindow.FloorEnd = portalToYClamped;
                renderWindow.WallStart = portalFromYClamped;
                renderWindow.WallEnd = portalToYClamped;
            }

            columnABufferIndex = EngineConstants.Unset;
            columnBBufferIndex = EngineConstants.Unset;
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
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            float sectorHeight = sector.Ceil - sector.Floor;
            int yOffset = line.YOffset;
            int xOffset = line.XOffset;
            bool lowerUnpegged = line.LowerUnpegged;
            bool upperUnpegged = line.UpperUnpegged;
            byte lightLevel = sector.LightLevel;

            float oneOverSectorHeight = 1f / sectorHeight;

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            ref Texture wallTexture = ref TextureCache.GetTexture(line.MiddleTexture);
            ref BGRA wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(wallTexture.Rotated);
            int textureWidth = wallTexture.Height;
            int textureHeight = wallTexture.Width;

            int textueStart = yOffset + textureWidth;

            if (lowerUnpegged)
            {
                textueStart = textureWidth - (int)sectorHeight % textureWidth;
            }

            Span<uint> columnBuffer = this.columnA.AsSpan(..textureWidth);
            ref uint columnBufferPtr = ref MemoryMarshal.GetReference(columnBuffer);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);
            
            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (Unsafe.IsNullRef(ref renderWindow))
                {
                    continue;
                }

                (int distance, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                int wallStartY = renderWindow.WallStart;
                int wallEndY = renderWindow.WallEnd;

                int clamptedFromY = Math.Clamp(wallStartY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int clamptedToY = Math.Clamp(wallEndY, renderWindow.CeilingStart, renderWindow.FloorEnd);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, clamptedToY * width + x);

                // texture is rotated - y position is x position in texture
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                float textureXIncr = (sectorHeight - 1) / (wallEndY - wallStartY);
                float textureXPos = textueStart - textureXIncr * (wallStartY - clamptedFromY);

                CalculateAndCacheWallColumn(columnBuffer, ref columnABufferIndex, ref wallTexturePtr, textureYPos, lightLevel);

                textureXPos %= textureWidth; // wrap the texture

                int textureXPosI = -1;
                int textureXPosIOld = -1;

                for (uint shaded = Unsafe.Add(ref columnBufferPtr, textureXPosI);
                     Unsafe.IsAddressGreaterThan(ref screenIndexPtrEnd, ref screenIndexPtr);
                     textureXPos += textureXIncr, screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width))
                {
                    textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
                        // wrap the texture
                        if (textureXPosI >= textureWidth)
                        {
                            textureXPosI -= textureWidth;
                            textureXPos -= textureWidth;
                        }

                        shaded = Unsafe.Add(ref columnBufferPtr, textureXPosI);
                    }

                    screenIndexPtr = shaded;
                }

                renderWindow.Distance = fromToYdist;
                renderWindow.WallEnd = renderWindow.WallStart;
                renderWindow.FloorEnd = renderWindow.WallStart;
                renderWindow.Calculated = false;
            }

            columnABufferIndex = EngineConstants.Unset;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateAndCacheWallColumn(Span<uint> buffer, ref int bufferIndex, ref BGRA wallTexturePtr, int textureYPos, byte brightness)
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
                buffer.Fill(Alpha);
                return;
            }

            ref BGRA columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);
            uint scale = (uint)brightness;

            for (int i = 0; i < buffer.Length; i++)
            {
                unchecked
                {
                    uint b = columnPtr.B * scale >> 8;
                    uint g = columnPtr.G * scale >> 8 << 8;
                    uint r = columnPtr.R * scale >> 8 << 16;
                    buffer[i] = b | g | r | Alpha;
                }

                columnPtr = ref Unsafe.Add(ref columnPtr, 1);
            }

            return;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static WallYPlaneInfo CalculateLeftWallYPlaneInfo(Wall wall, int wallFromXOffset)
        {
            float wallLengthX = wall.XRight - wall.XLeft;
            float wallStartY = wall.YLeftCeil;
            float ceilDistIncr = (wall.YRightCeil - wallStartY) / wallLengthX;

            float wallEndY = wall.YLeftFloor;
            float floorDistIncr = (wall.YRightFloor - wallEndY) / wallLengthX;

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
        private static (int TextureLocation, float FromToYDist) CalculateDistance(Wall wall, float cameraRay, float t1, float d2y, float d2x)
        {
            bool flipped = wall.Flipped;

            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;
            float fromToXDist = fromToYDist * cameraRay;

            float distX = (flipped ? wall.R2.X : wall.R1.X) - fromToXDist;
            float distY = (flipped ? wall.R2.Y: wall.R1.Y) - fromToYDist;

            float textureXLocation = MathF.Sqrt(distX * distX + distY * distY);

            return ((int)textureXLocation, fromToYDist);
        }
    }
}
