using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        // pre-computed texture buffers
        private int columnABufferIndex = -1;
        private readonly uint[] columnA = new uint[512];
        private int columnBBufferIndex = -1;
        private readonly uint[] columnB = new uint[512];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetScreenPtr<T>()
            where T : struct
        {
            return ref Unsafe.As<BGRA, T>(ref MemoryMarshal.GetReference(this.buffer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref uint GetBufferA(int size, out Span<uint> buffer)
        {
            buffer = columnA.AsSpan(..size);
            return ref MemoryMarshal.GetReference(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref uint GetBufferB(int size, out Span<uint> buffer)
        {
            buffer = columnB.AsSpan(..size);
            return ref MemoryMarshal.GetReference(buffer);
        }

        private void CalculateDistance(RenderablePortalWall renderableWall)
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
            RenderablePortalWall renderableWall)
        {
            int sectorHeight = float.ConvertToIntegerNative<int>(sector.Ceil - sector.Floor);
            RenderableWall wall = renderableWall.Wall;

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
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            TextureInfo upperTextureInfo = wall.UpperTexture!;
            TextureInfo lowerTextureInfo = wall.LowerTexture!;

            (bool upperSkybox, bool upperFlipX, bool upperFlipY) = GetFlags(upperTextureInfo);
            (bool lowerSkybox, bool lowerFlipX, bool lowerFlipY) = GetFlags(lowerTextureInfo);

            (float? upperXScale, float? upperYScale) = (upperTextureInfo.XScale, upperTextureInfo.YScale);
            (float? lowerXScale, float? lowerYScale) = (lowerTextureInfo.XScale, lowerTextureInfo.YScale);

            Texture upperTexture = TextureCache.GetTexture(upperTextureInfo);
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetArrayDataReference(upperSkybox ? upperTexture.Data : upperTexture.Rotated);
            ref uint upperTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref upperTexturePtr);

            Texture lowerTexture = TextureCache.GetTexture(lowerTextureInfo);
            ref BGRA lowerTexturePtr = ref MemoryMarshal.GetArrayDataReference(lowerTexture.Rotated);

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ref uint lowerTextureBufferPtr = ref GetBufferA(lowerTexture.Height, out Span<uint> lowerTextureBuffer);
            ref uint upperTextureBufferPtr = ref GetBufferB(upperTexture.Height, out Span<uint> upperTextureBuffer);

            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            float wallLength = wall.Length;

            int lowerTextureStart = lowerTextureInfo.YOffset << 16;
            int lowerXOffset = lowerTextureInfo.XOffset;

            int upperTextureStart = upperTextureInfo.YOffset << 16;
            int upperXOffset = upperTextureInfo.XOffset;

            if (upperYScale is float)
            {
                upperYScale = (sector.Ceil - sector.Floor) * upperYScale.Value;
            }

            if (upperXScale is float)
            {
                upperXScale = upperXScale.Value / wallLength * upperTexture.Width;
            }

            if (lowerYScale is float)
            {
                lowerYScale = (sector.Ceil - sector.Floor) * lowerYScale.Value;
            }

            if (lowerXScale is float)
            {
                lowerXScale = lowerXScale.Value / wallLength * lowerTexture.Width;
            }

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderWall)
                {
                    renderWindow.SetFinished(CalculateDistance2(cameraRay, t1, d2y, d2x));
                    continue;
                }

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
                float fromToYdist = CalculateDistance2(cameraRay, t1, d2y, d2x);

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
                        int textureXIncr;

                        if (upperYScale is float)
                        {
                            textureXIncr = float.ConvertToIntegerNative<int>(((upperTexture.Height << 16) * upperYScale.Value) / (wallEndY - wallStartY));
                        }
                        else
                        {
                            textureXIncr = (sectorHeight << 16) / (wallEndY - wallStartY);
                        }

                        (int distance, fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x, upperFlipX);

                        // Calculate Upper  Texture Position
                        int textureWidth = upperTexture.Height;
                        int textureHeight = upperTexture.Width;

                        int textureYPos;
                        if (upperXScale is float scale)
                        {
                            textureYPos = float.ConvertToIntegerNative<int>(distance * scale);
                        }
                        else
                        {
                            textureYPos = distance;
                        }

                        textureYPos = ((textureYPos + upperXOffset) % textureHeight) * textureWidth;
                        int textureXPos = upperTextureStart - textureXIncr * (wallStartY - fromYClamped);

                        textureXPos = EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                        CalculateAndCacheWallColumn(upperTextureBuffer, ref columnABufferIndex, ref upperTexturePtr, textureYPos, lightLevel, upperFlipY);

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
                        int textureXIncr;

                        if (lowerYScale is float)
                        {
                            textureXIncr = float.ConvertToIntegerNative<int>(((lowerTexture.Height << 16) * lowerYScale.Value) / (wallEndY - wallStartY));
                        }
                        else
                        {
                            textureXIncr = (sectorHeight << 16) / (wallEndY - wallStartY);
                        }

                        (int distance, fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x, lowerFlipX);


                        int textureWidth = lowerTexture.Height;
                        int textureHeight = lowerTexture.Width;

                        int textureYPos;

                        if (lowerXScale is float scale)
                        {
                            textureYPos = float.ConvertToIntegerNative<int>(distance * scale);
                        }
                        else
                        {
                            textureYPos = distance;
                        }

                        textureYPos = ((textureYPos + lowerXOffset) % textureHeight) * textureWidth;
                        int textureXPos = textureXIncr * (portalToYClamped - portalToY) + lowerTextureStart;

                        textureXPos = EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                        CalculateAndCacheWallColumn(lowerTextureBuffer, ref columnBBufferIndex, ref lowerTexturePtr, textureYPos, lightLevel, lowerFlipY);

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
            RenderablePortalWall renderableWall)
        {
            // separate path for skybox rendering
            RenderableWall wall = renderableWall.Wall;
            TextureInfo textureInfo = wall.MiddleTexture!;

            (bool skybox, bool flipX, bool flipY) = GetFlags(textureInfo);

            if (skybox)
            {
                return DrawBasicSkyboxWall(player, renderableWall);
            }

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int sectorHeight = float.ConvertToIntegerNative<int>(sector.Ceil - sector.Floor);

            byte lightLevel = sector.LightLevel;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            Texture wallTexture = TextureCache.GetTexture(textureInfo);
            ref BGRA wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(wallTexture.Rotated);
            int textureWidth = wallTexture.Height;
            int textureHeight = wallTexture.Width;

            int textureStart = textureInfo.YOffset;
            int xOffset = textureInfo.XOffset;

            ref uint columnBufferPtr = ref GetBufferA(textureWidth, out Span<uint> columnBuffer);

            sectorHeight <<= 16;
            textureStart <<= 16;

            float wallLength = wall.Length;


            (float? xScale, float? yScale) = (textureInfo.XScale, textureInfo.YScale);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);
            float scaledTextureWidth = default;

            if (yScale is float)
            {
                yScale = (sector.Ceil - sector.Floor) * yScale.Value;
                scaledTextureWidth = ((textureWidth << 16) * yScale.Value);
            }

            if (xScale is float)
            {
                xScale = xScale.Value / wallLength * textureHeight;
            }

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderWall)
                {
                    renderWindow.SetFinished(CalculateDistance2(cameraRay, t1, d2y, d2x));
                    continue;
                }

                (int distance, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x, flipX);
                (int clamptedFromY, int clamptedToY) = RenderWindowHelper.GetClampedWallFromTo(ref renderWindow);

                int wallStartY = renderWindow.WallStart;
                int wallEndY = renderWindow.WallEnd;

                // texture is rotated - y position is x position in texture
                int textureYPos;

                if (xScale is float scale)
                {
                    textureYPos = float.ConvertToIntegerNative<int>(distance * scale);
                }
                else
                {
                    textureYPos = distance;
                }

                textureYPos = ((textureYPos + xOffset) % textureHeight) * textureWidth;

                int textureXIncr, textureXPos;

                if (yScale is float)
                {
                    textureXIncr = float.ConvertToIntegerNative<int>(scaledTextureWidth / (wallEndY - wallStartY));
                }
                else
                {
                    textureXIncr = sectorHeight / (wallEndY - wallStartY);
                }

                textureXPos = textureStart - textureXIncr * (wallStartY - clamptedFromY);

                CalculateAndCacheWallColumn(columnBuffer, ref columnABufferIndex, ref wallTexturePtr, textureYPos, lightLevel, flipY);

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
            RenderablePortalWall renderableWall)
        {
            int width = PixelWidth;
            var wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            Texture wallTexture = TextureCache.GetTexture(wall.MiddleTexture);
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

        #region Render Line

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

            // % is slower than the bitwise &
            // thus we have two paths to render a wall line
            // depending if texture is power of two or not
            if (SharedHelpers.IsPowerOfTwo(textureHeight))
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

        #endregion

        #region Calculation Helpers

        private static int EnsureOffsetIsPositive(int textureHeight, int offset)
        {
            offset %= textureHeight;

            if (offset < 0)
            {
                offset = textureHeight + offset;
            }

            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateAndCacheWallColumn(
            scoped Span<uint> buffer,
            ref int bufferIndex,
            scoped ref BGRA wallTexturePtr,
            int textureYPos, byte brightness, bool flipY)
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

            if (flipY)
            {
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    uint b = columnPtr.B * scale >> 8;
                    uint g = columnPtr.G * scale >> 8 << 8;
                    uint r = columnPtr.R * scale >> 8 << 16;
                    buffer[i] = b | g | r | Alpha;

                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    uint b = columnPtr.B * scale >> 8;
                    uint g = columnPtr.G * scale >> 8 << 8;
                    uint r = columnPtr.R * scale >> 8 << 16;
                    buffer[i] = b | g | r | Alpha;

                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RenderablePlaneInfo CalculateLeftWallYPlaneInfo(RenderableWall wall, int wallFromXOffset)
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

            return new RenderablePlaneInfo(wallStartY, wallEndY, ceilDistIncr, floorDistIncr);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RenderablePlaneInfo CalculateLeftWallYPlaneInfo(RenderableSprite sprite, int wallFromXOffset)
        {
            wallFromXOffset = 0;

            float wallLengthX = sprite.XRight - sprite.XLeft;
            float wallStartY = sprite.YLeftCeil;
            float ceilDistIncr = (sprite.YRightCeil - wallStartY) / wallLengthX;

            float wallEndY = sprite.YLeftFloor;
            float floorDistIncr = (sprite.YRightFloor - wallEndY) / wallLengthX;

            if (wallFromXOffset != 0)
            {
                wallEndY += wallFromXOffset * floorDistIncr;
                wallStartY += wallFromXOffset * ceilDistIncr;
            }

            return new RenderablePlaneInfo(wallStartY, wallEndY, ceilDistIncr, floorDistIncr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (float CameraRay, float CameraRayIncr, float t1, float d2y, float d2x) CalculateCameraRay(RenderableWall wall, int width, int wallFromX)
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
        private static (float CameraRay, float CameraRayIncr, float t1, float d2y, float d2x) CalculateCameraRay(RenderableSprite sprite, int width, int wallFromX)
        {
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;
            float rx1 = sprite.R1.X;
            float ry1 = sprite.R1.Y;
            float d2x = sprite.R2.X - rx1;
            float d2y = sprite.R2.Y - ry1;
            float t1 = rx1 * d2y - ry1 * d2x;
            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * wallFromX;

            return (cameraRay, cameraWidthIncr, t1, d2y, d2x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int TextureLocation, float FromToYDist) CalculateDistance(
            RenderableWall wall,
            float cameraRay,
            float t1, float d2y, float d2x,
            bool flipX)
        {
            bool flipped = flipX ? !wall.Flipped : wall.Flipped;

            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;
            float fromToXDist = fromToYDist * cameraRay;

            float distX = flipped ? (wall.R2.X - fromToXDist) : (fromToXDist - wall.R1.X);
            float distY = flipped ? (wall.R2.Y - fromToYDist) : (fromToYDist - wall.R1.Y);

            float textureXLocation = MathF.Sqrt(distX * distX + distY * distY);

            return (float.ConvertToIntegerNative<int>(textureXLocation), fromToYDist);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (float TextureLocation, float FromToYDist) CalculateDistance(
            RenderableSprite sprite,
            float cameraRay,
            float t1, float d2y, float d2x,
            bool flipX)
        {
            bool flipped = flipX ? !sprite.Flipped : sprite.Flipped;

            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;
            float fromToXDist = fromToYDist * cameraRay;

            float distX = flipped ? (sprite.R2.X - fromToXDist) : (fromToXDist - sprite.R1.X);
            float distY = flipped ? (sprite.R2.Y - fromToYDist) : (fromToYDist - sprite.R1.Y);

            float textureXLocation = MathF.Sqrt(distX * distX + distY * distY);

            return (textureXLocation, fromToYDist);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CalculateDistance2(float cameraRay, float t1, float d2y, float d2x)
        {
            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;

            return fromToYDist;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool IsSkybox, bool FlipX, bool FlipY) GetFlags(TextureInfo textureInfo)
        {
            if (textureInfo is null)
            {
                return (false, false, false);
            }

            TextureRenderingOptions options = textureInfo.RenderingOptions;

            bool skyBox = options.HasFlag(TextureRenderingOptions.Skybox);
            bool flipX = options.HasFlag(TextureRenderingOptions.FlipX);
            bool flipY = options.HasFlag(TextureRenderingOptions.FlipY);

            return (skyBox, flipX, flipY);
        }

        #endregion
    }
}