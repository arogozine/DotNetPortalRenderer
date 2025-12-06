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

        private void CalculateDistance(RenderableWall renderableWall, float oneOverSectorHeight, float floorOffset, float ceilOffset)
        {
            int width = PixelWidth;
            var wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (!renderWindow.Calculated)
                {
                    continue;
                }

                float wallStartY = renderWindow.WallStart;
                float wallEndY = renderWindow.WallEnd;

                // Portal Calculation
                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;
                float floorPixelOffset = pixelsPerHeight * floorOffset;
                float ceilPixelOffset = pixelsPerHeight * ceilOffset;
                float portalFromY = wallStartY - ceilPixelOffset;
                float portalToY = wallEndY - floorPixelOffset;
                int portalFromYClamped = Math.Clamp((int)portalFromY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int portalToYClamped = Math.Clamp((int)portalToY, renderWindow.CeilingStart, renderWindow.FloorEnd);

                renderWindow.Distance = CalculateDistance2(cameraRay, t1, d2y, d2x);
                renderWindow.CeilingStart = portalFromYClamped;
                renderWindow.FloorEnd = portalToYClamped;
                renderWindow.WallEnd = renderWindow.WallStart;
            }
        }

        private bool DrawPortalWall(
            PortalPlayerSnapshot player,
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

            // ceiling and floor of the sector are the same
            // so no wall is drawn
            if (floorOffset == 0 && ceilOffset == 0)
            {
                CalculateDistance(renderableWall, oneOverSectorHeight, floorOffset, ceilOffset);
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
            int height = PixelHeight;
            var line = wall.Line;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            TextureInfo upperTextureInfo = line.UpperTexture!;
            TextureInfo lowerTextureInfo = line.LowerTexture!;
            ref Texture upperTexture = ref TextureCache.GetTexture(upperTextureInfo);
            ref Texture lowerTexture = ref TextureCache.GetTexture(lowerTextureInfo);

            bool upperSkybox = upperTextureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            ref BGRA lowerTexturePtr = ref MemoryMarshal.GetArrayDataReference(lowerTexture.Rotated);

            int lowerTextureStart = DetermineLowerTextureYOffset(sector, (int)floorOffset, (int)ceilOffset, lowerTextureInfo, ref lowerTexture);
            int lowerXOffset = DetermineXOffset(lowerTextureInfo, ref lowerTexture);

            int upperTextureStart = DetermineUpperTextureYOffset(sector, (int)ceilOffset, upperTextureInfo, ref upperTexture);
            int upperXOffset = DetermineXOffset(upperTextureInfo, ref upperTexture);

            ref BGRA upperTexturePtr = ref MemoryMarshal.GetArrayDataReference(upperSkybox ? upperTexture.Data : upperTexture.Rotated);
            ref uint upperTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref upperTexturePtr);

            Span<uint> lowerTextureBuffer = this.columnA.AsSpan(..lowerTexture.Height);
            ref uint lowerTextureBufferPtr = ref MemoryMarshal.GetReference(lowerTextureBuffer);

            Span<uint> upperTextureBuffer = this.columnB.AsSpan(..upperTexture.Height);
            ref uint upperTextureBufferPtr = ref MemoryMarshal.GetReference(upperTextureBuffer);

            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (renderWindow.FloorEnd < renderWindow.CeilingStart)
                {
                    renderWindow.Distance = CalculateDistance2(cameraRay, t1, d2y, d2x);
                    renderWindow.WallEnd = renderWindow.WallStart;
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

                float textureXIncr = (float)(sectorHeight / (wallEndY - wallStartY));

                // draw upper wall / upper skybox
                if (ceilOffset != 0 && fromYClamped < portalFromYClamped)
                {
                    int ceilingStart = fromYClamped * width + x;
                    ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, ceilingStart);
                    ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, portalFromYClamped * width + x);

                    if (upperSkybox)
                    {
                        RenderSkyboxLine(player,
                            x,
                            ref upperTexture,
                            ref upperTextureUintPtr,
                            ref angleCachePtr,
                            ref renderWindow,
                            ref screenIndexPtr,
                            ref screenIndexPtrEnd);
                    }
                    else
                    {
                        // Calculate Upper  Texture Position
                        int textureWidth = upperTexture.Height;
                        int textureHeight = upperTexture.Width;
                        int textureYPos = ((distance + upperXOffset) % textureHeight) * textureWidth;
                        float textureXPos = upperTextureStart - textureXIncr * (wallStartY - fromYClamped);
                        textureXPos %= textureWidth;

                        uint shaded = default;

                        CalculateAndCacheWallColumn(upperTextureBuffer, ref columnABufferIndex, ref upperTexturePtr, textureYPos, lightLevel);

                        for (
                            int textureXPosI = 0, textureXPosIOld = -1;
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
                                shaded = Unsafe.Add(ref upperTextureBufferPtr, textureXPosI);
                            }

                            screenIndexPtr = shaded;
                        }
                    }
                }

                // draw lower wall
                if (floorOffset != 0 && portalToYClamped < toYClamped)
                {
                    ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToYClamped * width + x);
                    ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, toYClamped * width + x);

                    int textureWidth = lowerTexture.Height;
                    int textureHeight = lowerTexture.Width;
                    int textureYPos = ((distance + lowerXOffset) % textureHeight) * textureWidth;
                    float textureXPos = MathF.FusedMultiplyAdd(textureXIncr, (portalToYClamped - portalToY), lowerTextureStart);
                    textureXPos %= textureWidth;

                    uint shaded = default;

                    CalculateAndCacheWallColumn(lowerTextureBuffer, ref columnBBufferIndex, ref lowerTexturePtr, textureYPos, lightLevel);

                    for (
                        int textureXPosI = 0, textureXPosIOld = -1;
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
                renderWindow.CeilingStart = portalFromYClamped;
                renderWindow.FloorEnd = portalToYClamped;
            }

            columnABufferIndex = EngineConstants.Unset;
            columnBBufferIndex = EngineConstants.Unset;

            // treat as a basic wall?
            return sectorHeight != floorOffset && sectorHeight != -ceilOffset;
        }

        private bool DrawBasicWall(
            PortalPlayerSnapshot player,
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

            TextureInfo textureInfo = line.MiddleTexture!;
            bool renderFromBottom = textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromBottom);

            if (textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox))
            {
                return DrawBasicSkyboxWall(player, screen, renderableWall);
            }

            byte lightLevel = sector.LightLevel;

            float oneOverSectorHeight = 1f / sectorHeight;

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            ref Texture wallTexture = ref TextureCache.GetTexture(line.MiddleTexture);
            ref BGRA wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(wallTexture.Rotated);
            ref uint wallTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetArrayDataReference(wallTexture.Data));
            int textureWidth = wallTexture.Height;
            int textureHeight = wallTexture.Width;

            int textureStart = DetermineTextureYOffset(sector, textureInfo, ref wallTexture);
            int xOffset = DetermineXOffset(textureInfo, ref wallTexture);

            Span<uint> columnBuffer = this.columnA.AsSpan(..textureWidth);
            ref uint columnBufferPtr = ref MemoryMarshal.GetReference(columnBuffer);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (renderWindow.FloorEnd < renderWindow.CeilingStart)
                {
                    renderWindow.Distance = CalculateDistance2(cameraRay, t1, d2y, d2x);
                    renderWindow.WallEnd = renderWindow.WallStart;
                    continue;
                }

                int wallStartY = renderWindow.WallStart;
                int wallEndY = renderWindow.WallEnd;

                int clamptedFromY = Math.Clamp(wallStartY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int clamptedToY = Math.Clamp(wallEndY, renderWindow.CeilingStart, renderWindow.FloorEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    renderWindow.Distance = CalculateDistance2(cameraRay, t1, d2y, d2x);
                    renderWindow.WallEnd = renderWindow.WallStart;
                    continue;
                }

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, clamptedToY * width + x);

                (int distance, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                // texture is rotated - y position is x position in texture
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                float textureXIncr = sectorHeight / (wallEndY - wallStartY);
                float textureXPos = textureStart - textureXIncr * (wallStartY - clamptedFromY);

                CalculateAndCacheWallColumn(columnBuffer, ref columnABufferIndex, ref wallTexturePtr, textureYPos, lightLevel);

                textureXPos %= textureWidth; // wrap the texture

                int textureXPosI = (int)textureXPos;
                int textureXPosIOld = textureXPosI;

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

                        textureXPosIOld = textureXPosI;
                        shaded = Unsafe.Add(ref columnBufferPtr, textureXPosI);
                    }

                    screenIndexPtr = shaded;
                }

                renderWindow.Distance = fromToYdist;
                renderWindow.WallEnd = 0;
                renderWindow.WallStart = 0;

            }

            columnABufferIndex = EngineConstants.Unset;

            return true;
        }

        private bool DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            Span<BGRA> screen,
            RenderableWall renderableWall)
        {
            int width = PixelWidth;
            var wall = renderableWall.Wall;
            var line = wall.Line;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            ref Texture wallTexture = ref TextureCache.GetTexture(line.MiddleTexture);
            ref uint wallTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetArrayDataReference(wallTexture.Data));
            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.TryGetRenderableDimensionsForX2(x);

                if (renderWindow.FloorEnd < renderWindow.CeilingStart)
                {
                    renderWindow.Distance = CalculateDistance2(cameraRay, t1, d2y, d2x);
                    renderWindow.WallEnd = renderWindow.WallStart;
                    continue;
                }

                int wallStartY = renderWindow.WallStart;
                int wallEndY = renderWindow.WallEnd;

                int clamptedFromY = Math.Clamp(wallStartY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int clamptedToY = Math.Clamp(wallEndY, renderWindow.CeilingStart, renderWindow.FloorEnd);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, clamptedToY * width + x);

                float fromToYdist = CalculateDistance2(cameraRay, t1, d2y, d2x);

                RenderSkyboxLine(player,
                    x,
                    ref wallTexture,
                    ref wallTextureUintPtr,
                    ref angleCachePtr,
                    ref renderWindow,
                    ref screenIndexPtr,
                    ref screenIndexPtrEnd);

                renderWindow.Distance = fromToYdist;
                renderWindow.WallEnd = renderWindow.WallStart;
            }

            return true;
        }

        private void RenderSkyboxLine(PortalPlayerSnapshot player,
            int x,
            ref Texture upperTexture,
            ref uint upperTextureUintPtr,
            ref float angleCachePtr,
            ref RenderWindow renderWindow,
            ref uint screenIndexPtr,
            ref uint screenIndexPtrEnd)
        {
            const float twoPi = 2 * MathF.PI;
            const float oneOverTwoPi = 1f / (2 * MathF.PI);

            int width = PixelWidth;
            int height = PixelHeight;

            float viewAngle = player.Angle;

            int textureWidth = upperTexture.Width;
            int textureHeight = upperTexture.Height;

            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;
            float yTextureIncr = (1f / height) * textureHeight;

            // calculate angle between 0 to 2 PI
            float angleX = Unsafe.Add(ref angleCachePtr, x) - viewAngle;
            if (angleX > twoPi)
            {
                angleX = angleX - twoPi;
            }
            else if (angleX < 0f)
            {
                angleX = twoPi + angleX;
            }

            int texX = (int)(textureWidth4 * angleX) % textureWidth;

            int fromYClamped = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);
            float vScreen = (float)fromYClamped * yTextureIncr;

            ref uint textureColumnPtr = ref Unsafe.Add(ref upperTextureUintPtr, texX);

            for (;
                    Unsafe.IsAddressGreaterThan(ref screenIndexPtrEnd, ref screenIndexPtr);
                    vScreen += yTextureIncr, screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width)
                )
            {
                int index = textureWidth * (int)(vScreen);
                screenIndexPtr = Unsafe.Add(ref textureColumnPtr, index);
            }
        }

        private static int DetermineXOffset(TextureInfo textureInfo, ref Texture wallTexture)
        {
            int offset = textureInfo.XOffset;
            int textureWidth = wallTexture.Width;

            if (offset < 0)
            {
                offset = textureWidth - offset;
            }

            return offset;
        }

        private static int DetermineLowerTextureYOffset(
            Sector sector,
            int floorOffset,
            int ceilingOffset,
            TextureInfo textureInfo,
            ref Texture wallTexture)
        {
            int offset = textureInfo.YOffset;
            TextureRenderingOptions renderingOptions = textureInfo.RenderingOptions;
            int textureHeight = wallTexture.Height;
            int sectorHeight = floorOffset; //(int)(sector.Ceil - sector.Floor);

            if (renderingOptions.HasFlag(TextureRenderingOptions.FromBottom))
            {
                if (sectorHeight >= textureHeight)
                {
                    // texture fits into sector (possibly multiple times)
                    // skip first (sectorHeight % textureHeight) rows
                    offset = (sectorHeight % textureHeight) - offset;
                }
                else
                {
                    // texture can't fit into sector
                    // skip first (textureHeight - sectorHeight) rows
                    offset = (textureHeight - sectorHeight) - offset;
                }
            }

            return textureHeight + offset;
        }

        private static int DetermineUpperTextureYOffset(
            Sector sector,
            int ceilingOffset,
            TextureInfo textureInfo,
            ref Texture wallTexture)
        {
            int offset = textureInfo.YOffset;
            TextureRenderingOptions renderingOptions = textureInfo.RenderingOptions;
            int textureHeight = wallTexture.Height;
            int sectorHeight = -ceilingOffset; //(int)(sector.Ceil - sector.Floor);

            if (renderingOptions.HasFlag(TextureRenderingOptions.FromBottom))
            {
                offset = offset % textureHeight;
                /*
                if (offset < 0)
                {
                    offset = textureHeight % (-offset);
                }
                */

                if (sectorHeight >= textureHeight)
                {
                    // texture fits into sector (possibly multiple times)
                    // skip first (sectorHeight % textureHeight) rows
                    offset = (sectorHeight % textureHeight) - offset;
                }
                else
                {
                    // texture can't fit into sector
                    // skip first (textureHeight - sectorHeight) rows
                    offset = (textureHeight - sectorHeight) + offset;
                }

                return textureHeight + offset;
            }

            if (offset < 0)
            {
                offset = textureHeight % (-offset);
                return offset;
            }

            return textureHeight - offset;
        }

        private static int DetermineTextureYOffset(
            Sector sector,
            TextureInfo textureInfo,
            ref Texture wallTexture)
        {
            int offset = textureInfo.YOffset;
            TextureRenderingOptions renderingOptions = textureInfo.RenderingOptions;
            int textureHeight = wallTexture.Height;
            int sectorHeight = (int)(sector.Ceil - sector.Floor);

            if (renderingOptions.HasFlag(TextureRenderingOptions.FromBottom) || renderingOptions.HasFlag(TextureRenderingOptions.FromSectorBottom))
            {
                offset = offset % textureHeight;

                if (sectorHeight >= textureHeight)
                {
                    // texture fits into sector (possibly multiple times)
                    // skip first (sectorHeight % textureHeight) rows
                    offset = (sectorHeight % textureHeight) - offset;
                }
                else
                {
                    // texture can't fit into sector
                    // skip first (textureHeight - sectorHeight) rows
                    offset = (textureHeight - sectorHeight) + offset;
                }

                return textureHeight + offset;
            }

            if (offset < 0)
            {
                offset = textureHeight % (-offset);
            }

            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateAndCacheWallColumn(scoped Span<uint> buffer, ref int bufferIndex, ref BGRA wallTexturePtr, int textureYPos, byte brightness)
        {
            // reuse the cached column
            if (bufferIndex == textureYPos)
            {
                return;
            }

            const uint Alpha = (uint)byte.MaxValue << 24;

            bufferIndex = textureYPos;

            // avoid calculating if too far away (all black)
            if (brightness == byte.MinValue)
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
            float distY = (flipped ? wall.R2.Y : wall.R1.Y) - fromToYDist;

            float textureXLocation = MathF.Sqrt(distX * distX + distY * distY);

            return ((int)textureXLocation, fromToYDist);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CalculateDistance2(float cameraRay, float t1, float d2y, float d2x)
        {
            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;

            return fromToYDist;
        }
    }
}
