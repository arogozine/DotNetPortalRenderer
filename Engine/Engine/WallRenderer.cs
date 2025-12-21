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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetScreenPtr<T>()
            where T : struct
        {
            return ref Unsafe.As<BGRA, T>(ref MemoryMarshal.GetReference(this.buffer));
        }

        private void CalculateDistance(RenderableWall renderableWall)
        {
            int width = PixelWidth;
            var wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderWall)
                {
                    continue;
                }

                (int portalFromYClamped, int portalToYClamped) = RenderWindowHelper.GetClampedWallFromTo(ref renderWindow);

                renderWindow.Distance = CalculateDistance2(cameraRay, t1, d2y, d2x);
                renderWindow.CeilingStart = portalFromYClamped;
                renderWindow.FloorEnd = portalToYClamped;
                renderWindow.Status ^= RenderColumnStatus.CanRenderWall;
            }
        }

        private bool DrawPortalWall(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            RenderableWall renderableWall)
        {
            int sectorHeight = float.ConvertToIntegerNative<int>(sector.Ceil - sector.Floor);
            var wall = renderableWall.Wall;

            Sector neighborSector = sectors[wall.Neighbor];
            float oneOverSectorHeight = 1f / sectorHeight;
            int floorOffset = float.ConvertToIntegerNative<int>(neighborSector.Floor - sector.Floor);
            int ceilOffset = float.ConvertToIntegerNative<int>(neighborSector.Ceil - sector.Ceil);
            byte lightLevel = sector.LightLevel;

            if (floorOffset < 0)
            {
                floorOffset = 0;
            }

            if (ceilOffset > 0)
            {
                ceilOffset = 0;
            }

            // don't draw beyond the bounds
            if (ceilOffset < -sectorHeight)
            {
                ceilOffset = -sectorHeight;
            }

            if (floorOffset > sectorHeight)
            {
                floorOffset = sectorHeight;
            }

            // ceiling and floor of the sector are the same
            // so no wall is drawn
            if (floorOffset == 0 && ceilOffset == 0)
            {
                CalculateDistance(renderableWall);
                return true;
            }

            int width = PixelWidth;
            var line = wall.Line;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            TextureInfo upperTextureInfo = line.UpperTexture!;
            TextureInfo lowerTextureInfo = line.LowerTexture!;
            ref Texture upperTexture = ref TextureCache.GetTexture(upperTextureInfo);
            ref Texture lowerTexture = ref TextureCache.GetTexture(lowerTextureInfo);

            bool upperSkybox = upperTextureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);
            bool lowerSkybox = lowerTextureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ref BGRA lowerTexturePtr = ref MemoryMarshal.GetArrayDataReference(lowerTexture.Rotated);

            int lowerTextureStart = DetermineLowerTextureYOffset(floorOffset, lowerTextureInfo, ref lowerTexture);
            int lowerXOffset = DetermineXOffset(lowerTextureInfo, in lowerTexture);

            int upperTextureStart = DetermineUpperTextureYOffset(ceilOffset, upperTextureInfo, ref upperTexture);
            int upperXOffset = DetermineXOffset(upperTextureInfo, in upperTexture);

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
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderWall)
                {
                    renderWindow.SetFinished(CalculateDistance2(cameraRay, t1, d2y, d2x));
                    continue;
                }

                (int distance, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                int wallStartY = renderWindow.WallStart;
                int wallEndY = renderWindow.WallEnd;

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Wall Calculation
                (int fromYClamped, int toYClamped) = RenderWindowHelper.GetClampedWallFromTo(ref renderWindow);

                // Portal Calculation
                int floorPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * floorOffset);
                int ceilPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * ceilOffset);
                int portalFromY = wallStartY - ceilPixelOffset;
                int portalToY = wallEndY - floorPixelOffset;
                int portalFromYClamped = Math.Clamp(portalFromY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int portalToYClamped = Math.Clamp(portalToY, renderWindow.CeilingStart, renderWindow.FloorEnd);

                int textureXIncr = (sectorHeight << 16) / (wallEndY - wallStartY);

                // draw upper wall / upper skybox
                if (ceilOffset != 0 && fromYClamped < portalFromYClamped)
                {
                    if (upperSkybox)
                    {
                        ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, fromYClamped * width + x);
                        ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, portalFromYClamped * width + x);

                        RenderSkyboxLine(player,
                            x,
                            in upperTexture,
                            ref upperTextureUintPtr,
                            ref angleCachePtr,
                            in renderWindow,
                            ref screenIndexPtr,
                            ref screenIndexPtrEnd);
                    }
                    else
                    {
                        // Calculate Upper  Texture Position
                        int textureWidth = upperTexture.Height;
                        int textureHeight = upperTexture.Width;
                        int textureYPos = ((distance + upperXOffset) % textureHeight) * textureWidth;
                        int textureXPos = (upperTextureStart << 16) - textureXIncr * (wallStartY - fromYClamped);

                        textureXPos = EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                        CalculateAndCacheWallColumn(upperTextureBuffer, ref columnABufferIndex, ref upperTexturePtr, textureYPos, lightLevel);

                        RenderWallLine(
                            width,
                            x,
                            textureWidth,
                            fromYClamped,
                            portalFromYClamped,
                            (uint)textureXPos,
                            (uint)textureXIncr,
                            ref screenPtr,
                            ref upperTextureBufferPtr
                        );
                    }
                }

                // draw lower wall
                if (floorOffset != 0 && portalToYClamped < toYClamped)
                {
                    if (lowerSkybox)
                    {
                        ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToYClamped * width + x);
                        ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, toYClamped * width + x);

                        RenderSkyboxLine(player,
                            x,
                            in upperTexture,
                            ref upperTextureUintPtr,
                            ref angleCachePtr,
                            in renderWindow,
                            ref screenIndexPtr,
                            ref screenIndexPtrEnd);
                    }
                    else
                    {
                        int textureWidth = lowerTexture.Height;
                        int textureHeight = lowerTexture.Width;
                        int textureYPos = ((distance + lowerXOffset) % textureHeight) * textureWidth;
                        int textureXPos = textureXIncr * (portalToYClamped - portalToY) + (lowerTextureStart << 16);

                        textureXPos = EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                        CalculateAndCacheWallColumn(lowerTextureBuffer, ref columnBBufferIndex, ref lowerTexturePtr, textureYPos, lightLevel);

                        RenderWallLine(
                            width,
                            x,
                            textureWidth,
                            portalToYClamped,
                            toYClamped,
                            (uint)textureXPos,
                            (uint)textureXIncr,
                            ref screenPtr,
                            ref lowerTextureBufferPtr
                        );
                    }
                }

                renderWindow.Distance = fromToYdist;
                renderWindow.CeilingStart = portalFromYClamped;
                renderWindow.FloorEnd = portalToYClamped;
                renderWindow.Status ^= RenderColumnStatus.CanRenderWall;
            }

            columnABufferIndex = EngineConstants.Unset;
            columnBBufferIndex = EngineConstants.Unset;

            // if sector height matches top or bottom offset only top or bottom texture was drawn
            // no middle texture is possible, thus we can treat this as basic wall
            return !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);
        }

        private bool DrawBasicWall(
            PortalPlayerSnapshot player,
            Sector sector,
            RenderableWall renderableWall)
        {
            Wall wall = renderableWall.Wall;
            Line line = wall.Line;
            TextureInfo textureInfo = line.MiddleTexture!;

            if (textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox))
            {
                return DrawBasicSkyboxWall(player, renderableWall);
            }

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int sectorHeight = float.ConvertToIntegerNative<int>(sector.Ceil - sector.Floor);

            byte lightLevel = sector.LightLevel;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ref Texture wallTexture = ref TextureCache.GetTexture(line.MiddleTexture);
            ref BGRA wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(wallTexture.Rotated);
            int textureWidth = wallTexture.Height;
            int textureHeight = wallTexture.Width;

            int textureStart = DetermineTextureYOffset(sector, textureInfo, in wallTexture);
            int xOffset = DetermineXOffset(textureInfo, in wallTexture);

            Span<uint> columnBuffer = this.columnA.AsSpan(..textureWidth);
            ref uint columnBufferPtr = ref MemoryMarshal.GetReference(columnBuffer);

            textureStart <<= 16;
            sectorHeight <<= 16;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderWall)
                {
                    renderWindow.SetFinished(CalculateDistance2(cameraRay, t1, d2y, d2x));
                    continue;
                }

                (int distance, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                int wallStartY = renderWindow.WallStart;
                int wallEndY = renderWindow.WallEnd;

                (int clamptedFromY, int clamptedToY) = RenderWindowHelper.GetClampedWallFromTo(ref renderWindow);

                // texture is rotated - y position is x position in texture
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                int textureXIncr = sectorHeight / (wallEndY - wallStartY);
                int textureXPos = textureStart - textureXIncr * (wallStartY - clamptedFromY);

                CalculateAndCacheWallColumn(columnBuffer, ref columnABufferIndex, ref wallTexturePtr, textureYPos, lightLevel);

                textureXPos = EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                RenderWallLine(
                    width,
                    x,
                    textureWidth,
                    clamptedFromY,
                    clamptedToY,
                    (uint)textureXPos,
                    (uint)textureXIncr,
                    ref screenPtr,
                    ref columnBufferPtr
                );

                renderWindow.SetFinished(fromToYdist);
            }

            columnABufferIndex = EngineConstants.Unset;

            return true;
        }

        private bool DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderableWall renderableWall)
        {
            int width = PixelWidth;
            var wall = renderableWall.Wall;
            var line = wall.Line;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ref Texture wallTexture = ref TextureCache.GetTexture(line.MiddleTexture);
            ref uint wallTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetArrayDataReference(wallTexture.Data));
            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderWall)
                {
                    renderWindow.SetFinished(CalculateDistance2(cameraRay, t1, d2y, d2x));
                    continue;
                }

                (int clamptedFromY, int clamptedToY) = RenderWindowHelper.GetClampedWallFromTo(ref renderWindow);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, clamptedToY * width + x);

                float fromToYdist = CalculateDistance2(cameraRay, t1, d2y, d2x);

                RenderSkyboxLine(player,
                    x,
                    in wallTexture,
                    ref wallTextureUintPtr,
                    ref angleCachePtr,
                    in renderWindow,
                    ref screenIndexPtr,
                    ref screenIndexPtrEnd);

                renderWindow.SetFinished(fromToYdist);
            }

            return true;
        }

        private void RenderSkyboxLine(PortalPlayerSnapshot player,
            int x,
            in Texture upperTexture,
            ref uint upperTextureUintPtr,
            ref float angleCachePtr,
            in RenderWindow renderWindow,
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
                angleX -= twoPi;
            }
            else if (angleX < 0f)
            {
                angleX = twoPi + angleX;
            }

            int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) % textureWidth;

            int fromYClamped = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);
            float vScreen = (float)fromYClamped * yTextureIncr;

            ref uint textureColumnPtr = ref Unsafe.Add(ref upperTextureUintPtr, texX);

            for (;
                    Unsafe.IsAddressGreaterThan(ref screenIndexPtrEnd, ref screenIndexPtr);
                    vScreen += yTextureIncr, screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width)
                )
            {
                int index = textureWidth * float.ConvertToIntegerNative<int>(vScreen);
                screenIndexPtr = Unsafe.Add(ref textureColumnPtr, index);
            }
        }

        private static void RenderWallLine(
            int width,
            int x,
            int textureHeight,
            int startY,
            int endY,
            uint textureXPos_u,
            uint textureXIncr_u,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
            )
        {
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, startY * width + x);
            ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, endY * width + x);

            if (MathFormulas.IsPowerOfTwo(textureHeight))
            {
                uint textureMask = (uint)(textureHeight - 1);

                while (!Unsafe.AreSame(ref screenIndexPtr, ref screenIndexPtrEnd))
                {
                    uint texelIndex = (textureXPos_u >> 16) & textureMask;
                    uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos_u += textureXIncr_u;
                }
            }
            else
            {
                uint textureHeight_u = (uint)textureHeight;

                while (Unsafe.IsAddressLessThan(ref screenIndexPtr, ref screenIndexPtrEnd))
                {
                    uint texelIndex = (textureXPos_u >> 16) % textureHeight_u;
                    uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos_u += textureXIncr_u;
                }
            }
        }

        private static int DetermineXOffset(TextureInfo textureInfo, in Texture wallTexture)
        {
            int offset = textureInfo.XOffset;
            int textureWidth = wallTexture.Width;

            if (offset < 0)
            {
                offset = textureWidth + offset;
            }

            return offset;
        }

        private static int DetermineLowerTextureYOffset(
            int floorOffset,
            TextureInfo textureInfo,
            ref Texture wallTexture)
        {
            int offset = textureInfo.YOffset;
            TextureRenderingOptions renderingOptions = textureInfo.RenderingOptions;
            int textureHeight = wallTexture.Height;
            int sectorHeight = floorOffset;

            offset = EnsureOffsetIsPositive(textureHeight, offset);

            if (renderingOptions.HasFlag(TextureRenderingOptions.FromBottom))
            {
                int offsetFromBottom = DetermineTextureOffsetFromBottom(textureHeight, sectorHeight);
                offset = offsetFromBottom - offset;
            }

            return EnsureOffsetIsPositive(textureHeight, offset);
        }

        private static int DetermineUpperTextureYOffset(
            int ceilingOffset,
            TextureInfo textureInfo,
            ref Texture wallTexture)
        {
            int offset = textureInfo.YOffset;
            TextureRenderingOptions renderingOptions = textureInfo.RenderingOptions;
            int textureHeight = wallTexture.Height;
            int sectorHeight = -ceilingOffset;

            offset = EnsureOffsetIsPositive(textureHeight, offset);

            if (renderingOptions.HasFlag(TextureRenderingOptions.FromBottom))
            {
                int offsetFromBottom = DetermineTextureOffsetFromBottom(textureHeight, sectorHeight);
                offset = offsetFromBottom + offset;
            }

            return EnsureOffsetIsPositive(textureHeight, offset);
        }

        private static int DetermineTextureYOffset(
            Sector sector,
            TextureInfo textureInfo,
            in Texture wallTexture)
        {
            int offset = textureInfo.YOffset;
            TextureRenderingOptions renderingOptions = textureInfo.RenderingOptions;
            int textureHeight = wallTexture.Height;
            int sectorHeight = float.ConvertToIntegerNative<int>(sector.Ceil - sector.Floor);

            offset = EnsureOffsetIsPositive(textureHeight, offset);

            if (renderingOptions.HasFlag(TextureRenderingOptions.FromBottom) || renderingOptions.HasFlag(TextureRenderingOptions.FromSectorBottom))
            {
                int offsetFromBottom = DetermineTextureOffsetFromBottom(textureHeight, sectorHeight);
                offset = offsetFromBottom + offset;
            }

            return EnsureOffsetIsPositive(textureHeight, offset);
        }

        private static int EnsureOffsetIsPositive(int textureHeight, int offset)
        {
            offset %= textureHeight;

            if (offset < 0)
            {
                offset = textureHeight + offset;
            }

            return offset;
        }

        private static int DetermineTextureOffsetFromBottom(int textureHeight, int sectorHeight)
        {
            if (sectorHeight >= textureHeight)
            {
                return sectorHeight % textureHeight;
            }
            else
            {
                return textureHeight - sectorHeight;
            }
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

            float distX = flipped ? (wall.R2.X - fromToXDist) : (fromToXDist - wall.R1.X);
            float distY = flipped ? (wall.R2.Y - fromToYDist) : (fromToYDist - wall.R1.Y);

            float textureXLocation = MathF.Sqrt(distX * distX + distY * distY);

            return (float.ConvertToIntegerNative<int>(textureXLocation), fromToYDist);
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