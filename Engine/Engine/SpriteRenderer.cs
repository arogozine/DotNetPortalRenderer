using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void DrawSprite(ReadOnlySpan<Sector> sectors, RenderableSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            TextureInfo textureInfo = sprite.Texture;

            if (textureInfo.RenderingOptions.IsWall)
            {
                DrawWallSprite(sectors, sprite, renderableWall);
                return;
            }

            Texture texture = TextureCache.GetTexture(textureInfo.Name);

            ref uint screenPtr = ref GetScreenPtr<uint>();
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);

            int width = PixelWidth;
            int textureWidth = texture.Height;

            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            Sector sector = sectors[sprite.SectorId];
            byte lightLevel = sector.LightLevel;

            float rx1 = sprite.R1.X;
            float rx2 = sprite.R2.X;

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteStartY = sprite.YLeftCeil;
            int spriteEndY = sprite.YLeftFloor;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            Span<int> floorEndArray = renderableWall.FloorEnd;
            Span<int> ceilingStartArray = renderableWall.CeilingStart;
            Span<float> distance = renderableWall.Distance;

            float fromToYDist = sprite.Distance;

            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * spriteFromX;

            ref uint columnBufferPtr = ref GetBufferA(textureWidth, out Span<uint> columnBuffer);

            float textureXIncr = ((float)textureWidth) / (spriteEndY - spriteStartY);

            bool flipY = sprite.Texture.RenderingOptions.IsFlippedY;
            bool flipX = sprite.Texture.RenderingOptions.IsFlippedX;

            float textureLen = texture.Width / sprite.Length;

            for (int x = spriteFromX; x < spriteToX; x++, cameraRay += cameraWidthIncr)
            {
                int ceilingStart = ceilingStartArray[x];
                int floorEnd = floorEndArray[x];

                if (floorEnd <= ceilingStart || distance[x] < fromToYDist)
                {
                    continue;
                }

                int clamptedFromY = Math.Clamp(spriteStartY, ceilingStart, floorEnd);
                int clamptedToY = Math.Clamp(spriteEndY, ceilingStart, floorEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    continue;
                }

                int textureXLocation = CalculateTextureXPosition(cameraRay);

                int textureYPos = textureXLocation * textureWidth;
                float textureXPos = (clamptedFromY - spriteStartY) * textureXIncr;

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel, flipY);

                DrawSpriteLine(width, x, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                    ref screenPtr, ref columnBufferPtr);
            }

            columnABufferIndex = EngineConstants.Unset;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int CalculateTextureXPosition(float cameraRay)
            {
                float fromToXDist = fromToYDist * cameraRay;
                float distX = flipX ? (rx2 - fromToXDist) : (fromToXDist - rx1);

                return float.ConvertToIntegerNative<int>(MathF.Abs(distX) * textureLen);
            }
        }

        private void DrawWallSprite(ReadOnlySpan<Sector> sectors, RenderableSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            TextureInfo textureInfo = sprite.Texture;

            Texture texture = TextureCache.GetTexture(textureInfo.Name);

            ref uint screenPtr = ref GetScreenPtr<uint>();
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);

            int width = PixelWidth;
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            Sector sector = sectors[sprite.SectorId];
            byte lightLevel = sector.LightLevel;

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            RenderablePlaneInfo yPlaneInfo = CalculateLeftWallYPlaneInfo(sprite, spriteFromX);
            float spriteStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float spriteEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            int xOffset = 0;

            ReadOnlySpan<int> floorEndArray = renderableWall.FloorEnd;
            ReadOnlySpan<int> ceilingStartArray = renderableWall.CeilingStart;
            ReadOnlySpan<float> distance = renderableWall.Distance;

            ref uint columnBufferPtr = ref GetBufferA(textureWidth, out Span<uint> columnBuffer);

            bool flipY = sprite.Texture.RenderingOptions.IsFlippedY;
            bool flipX = sprite.Texture.RenderingOptions.IsFlippedX;

            float xScale = texture.Width / sprite.Length;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(sprite, width, spriteFromX);

            for (int x = spriteFromX; x < spriteToX; x++, cameraRay += cameraWidthIncr, spriteStartY += ceilDistIncr, spriteEndY += floorDistIncr)
            {
                int ceilingStart = ceilingStartArray[x];
                int floorEnd = floorEndArray[x];

                if (floorEnd <= ceilingStart)
                {
                    continue;
                }

                float textureXIncr = (float)(textureWidth / (spriteEndY - spriteStartY));

                int clamptedFromY = Math.Clamp(float.ConvertToIntegerNative<int>(spriteStartY), ceilingStart, floorEnd);
                int clamptedToY = Math.Clamp(float.ConvertToIntegerNative<int>(spriteEndY), ceilingStart, floorEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    continue;
                }

                (float textureXLocation, float fromToYdist) = CalculateDistance(sprite, cameraRay, t1, d2y, d2x, flipX);

                if ((int)distance[x] < (int)fromToYdist)
                {
                    continue;
                }

                textureXLocation += xOffset;
                textureXLocation *= xScale;

                int textureYPos = (float.ConvertToIntegerNative<int>(textureXLocation) % textureHeight) * textureWidth;
                float textureXPos = (clamptedFromY - spriteStartY) * textureXIncr;

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel, flipY);

                DrawSpriteLine(width, x, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                    ref screenPtr, ref columnBufferPtr);
            }

            columnABufferIndex = EngineConstants.Unset;
        }

        private void DrawTransparentWall(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo textureInfo = wall.MiddleTexture!;

            if (textureInfo.XScale is not null)
            {
                DrawTransparentWall_Build(sectors, renderableWall);
                return;
            }

            Sector sector = wall.Sector;
            int width = PixelWidth;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            float sectorHeight = sector.Ceil - sector.Floor;

            Texture texture = TextureCache.GetTexture(textureInfo);
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            ref uint columnBufferPtr = ref GetBufferA(textureWidth, out Span<uint> columnBuffer);

            RenderablePlaneInfo yPlaneInfo = CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            Sector neighborSector = sectors[wall.Neighbor];
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;

            float oneOverSectorHeight = 1f / sectorHeight;

            bool renderFromTop = textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromTop);
            byte lightLevel = sector.LightLevel;
            float alpha = Math.Clamp(textureInfo.Alpha, 0f, 1f);

            if (floorOffset < 0f)
            {
                floorOffset = 0f;
            }

            if (ceilOffset > 0f)
            {
                ceilOffset = 0f;
            }

            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset > sectorHeight ? textureInfo.YOffset - 65536 : textureInfo.YOffset;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus columnStatus = renderableWall.ColumnStatus[x];

                if (columnStatus.PortalRenderable)
                {
                    continue;
                }

                float buffer = renderableWall.Distance[x];
                int floorEnd = Math.Min(renderableWall.FloorEnd[x], renderableWall.WallEnd[x]);
                int ceilingStart = renderableWall.CeilingStart[x];

                (int distance, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x, false);

                if (fromToYdist > buffer)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                float pixelsPerUnit = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                float floorPixelOffset = pixelsPerUnit * floorOffset;
                float ceilPixelOffset = pixelsPerUnit * ceilOffset;
                float portalFromY = wallStartY - ceilPixelOffset;
                float portalToY = wallEndY - floorPixelOffset;

                float textureStartY = renderFromTop ? portalFromY : (portalToY - texture.Height * pixelsPerUnit);
                float textureEndY = renderFromTop ? (portalFromY + texture.Height * pixelsPerUnit) : portalToY;

                if (yOffset != 0)
                {
                    float yOffsetF = yOffset * pixelsPerUnit;

                    if (yOffset > 0)
                    {
                        textureStartY = renderFromTop ? textureStartY + yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY + yOffsetF : textureEndY - yOffsetF;
                    }
                    else
                    {
                        textureStartY = renderFromTop ? textureStartY - yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY - yOffsetF : textureEndY - yOffsetF;
                    }
                }

                int textureStartYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureStartY), ceilingStart, floorEnd);
                int textureEndYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureEndY), ceilingStart, floorEnd);

                if (textureStartYClamped >= textureEndYClamped)
                {
                    continue;
                }

                float offset = textureStartYClamped - textureStartY;

                // Calculate Middle Texture Position
                float textureXIncr = (float)(sectorHeight / (wallEndY - wallStartY));
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;

                float textureXPos = MathF.FusedMultiplyAdd(textureXIncr, offset, textureWidth);

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel, false);

                if (alpha == 1f)
                {
                    DrawTransparentWallLine(width, x,
                        textureStartYClamped, textureEndYClamped,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref columnBufferPtr);
                }
                else
                {
                    DrawTransparentWallLineWithAlpha(width, x,
                        textureStartYClamped, textureEndYClamped,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref columnBufferPtr,
                        alpha);
                }
            }

            columnABufferIndex = EngineConstants.Unset;
        }


        private void DrawTransparentWall_Build(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            ReadOnlySpan<int> floorEnd = renderableWall.FloorEnd;
            ReadOnlySpan<int> ceilingStart = renderableWall.CeilingStart;
            ReadOnlySpan<float> distance = renderableWall.Distance;
            ReadOnlySpan<RenderColumnStatus> columnStatus = renderableWall.ColumnStatus;

            int width = PixelWidth;
            RenderableWall wall = renderableWall.Wall;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Sector sector = wall.Sector;
            float sectorHeight = sector.Ceil - sector.Floor;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            TextureInfo textureInfo = wall.MiddleTexture!;
            Texture texture = TextureCache.GetTexture(textureInfo);
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            ref uint columnBufferPtr = ref GetBufferA(textureWidth, out Span<uint> columnBuffer);

            RenderablePlaneInfo yPlaneInfo = CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            Sector neighborSector = sectors[wall.Neighbor];
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;

            float oneOverSectorHeight = 1f / sectorHeight;

            byte lightLevel = sector.LightLevel;
            float alpha = Math.Clamp(textureInfo.Alpha, 0f, 1f);

            if (floorOffset < 0f)
            {
                floorOffset = 0f;
            }

            if (ceilOffset > 0f)
            {
                ceilOffset = 0f;
            }

            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;

            (_, bool flipX, bool flipY) = GetFlags(textureInfo);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            (float xScale, float yScale) = (textureInfo.XScale!.Value, textureInfo.YScale!.Value);
            yScale = (sector.Ceil - sector.Floor) * yScale;
            xScale = xScale / wall.Length * texture.Width;

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus columnStatusY = columnStatus[x];

                if (columnStatusY.PortalRenderable)
                {
                    continue;
                }

                float buffer = distance[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];

                (int distanceY, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x, flipX);

                if (fromToYdist > buffer)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                float pixelsPerUnit = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                float floorPixelOffset = pixelsPerUnit * floorOffset;
                float ceilPixelOffset = pixelsPerUnit * ceilOffset;
                float textureFromY = wallStartY - ceilPixelOffset;
                float textureToY = wallEndY - floorPixelOffset;

                // Clamp to View Window
                int clampedFromY = Math.Clamp(float.ConvertToIntegerNative<int>(textureFromY), ceilingStartY, floorEndY);
                int clampedToY = Math.Clamp(float.ConvertToIntegerNative<int>(textureToY), ceilingStartY, floorEndY);

                if (clampedFromY >= clampedToY)
                {
                    continue;
                }

                // Calculate Middle Texture Position
                int textureYPos = float.ConvertToIntegerNative<int>(distanceY * xScale);
                textureYPos = ((textureYPos + xOffset) % textureHeight) * textureWidth;

                float textureXIncr = (textureWidth * yScale) / (wallEndY - wallStartY);
                float textureXPos = yOffset - textureXIncr * (wallStartY - clampedFromY);

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel, flipY);

                if (alpha == 1f)
                {
                    DrawTransparentWallLine(width, x,
                        clampedFromY, clampedToY,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref columnBufferPtr);
                }
                else
                {
                    DrawTransparentWallLineWithAlpha(width, x,
                        clampedFromY, clampedToY,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref columnBufferPtr,
                        alpha);
                }
            }

            columnABufferIndex = EngineConstants.Unset;
        }

        private static void DrawTransparentWallLine(
            int width,
            int x,
            int textureStartYClamped, int textureEndYClamped,
            int textureHeight,
            float textureXPos,
            float textureXIncr,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
            )
        {
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, textureStartYClamped * width + x);
            ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, textureEndYClamped * width + x);
            uint textureXPos_u = float.ConvertToIntegerNative<uint>(textureXPos * (1 << 16));
            uint textureXIncr_u = float.ConvertToIntegerNative<uint>(textureXIncr * (1 << 16));
            uint textureHeight_u = (uint)textureHeight;

            while (Unsafe.IsAddressLessThan(ref screenIndexPtr, ref screenIndexPtrEnd))
            {
                uint texelIndex = (textureXPos_u >> 16) % textureHeight_u;
                uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                if (shaded != 0U)
                    screenIndexPtr = shaded;

                screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                textureXPos_u += textureXIncr_u;
            }
        }

        private static void DrawTransparentWallLineWithAlpha(
            int width,
            int x,
            int textureStartYClamped, int textureEndYClamped,
            int textureHeight,
            float textureXPos,
            float textureXIncr,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer,
            float alpha
            )
        {
            uint a = float.ConvertToIntegerNative<uint>(alpha * byte.MaxValue);
            uint aInv = byte.MaxValue - a;

            ref BGRA screenIndexPtrBgra = ref Unsafe.As<uint, BGRA>(ref screenPtr);
            ref BGRA screenIndexPtr = ref Unsafe.Add(ref screenIndexPtrBgra, textureStartYClamped * width + x);
            ref BGRA screenIndexPtrEnd = ref Unsafe.Add(ref screenIndexPtrBgra, textureEndYClamped * width + x);

            uint textureXPos_u = float.ConvertToIntegerNative<uint>(textureXPos * (1 << 16));
            uint textureXIncr_u = float.ConvertToIntegerNative<uint>(textureXIncr * (1 << 16));
            uint textureHeight_u = (uint)textureHeight;

            while (Unsafe.IsAddressLessThan(ref screenIndexPtr, ref screenIndexPtrEnd))
            {
                uint texelIndex = (textureXPos_u >> 16) % textureHeight_u;
                BGRA shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                if (shaded != 0U)
                {
                    screenIndexPtr = BlendBGRA(ref screenIndexPtr, ref shaded, a, aInv);
                }

                screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                textureXPos_u += textureXIncr_u;
            }
        }


        private static void DrawSpriteLine(
                int width,
                int x,
                int textureStartYClamped, int textureEndYClamped,
                float textureXPos,
                float textureXIncr,
                scoped ref uint screenPtr,
                scoped ref uint textureBuffer
                )
        {
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, textureStartYClamped * width + x);
            ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, textureEndYClamped * width + x);
            uint textureXPos_u = float.ConvertToIntegerNative<uint>(textureXPos * (1 << 16));
            uint textureXIncr_u = float.ConvertToIntegerNative<uint>(textureXIncr * (1 << 16));

            while (Unsafe.IsAddressLessThan(ref screenIndexPtr, ref screenIndexPtrEnd))
            {
                uint texelIndex = textureXPos_u >> 16;
                uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                if (shaded != 0U)
                    screenIndexPtr = shaded;

                screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                textureXPos_u += textureXIncr_u;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint BlendBGRA(ref BGRA bgraDst, ref BGRA bgraSrc, uint a, uint aInv)
        {
            const uint Alpha = (uint)byte.MaxValue << 24;

            uint bDst = bgraDst.B;
            uint gDst = bgraDst.G;
            uint rDst = bgraDst.R;

            uint bSrc = bgraSrc.B;
            uint gSrc = bgraSrc.G;
            uint rSrc = bgraSrc.R;

            uint bOut = (bSrc * a + bDst * aInv) >> 8;
            uint gOut = (gSrc * a + gDst * aInv) >> 8;
            uint rOut = (rSrc * a + rDst * aInv) >> 8;

            return (Alpha | (rOut << 16) | (gOut << 8) | bOut);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateSprite(
            scoped Span<uint> spriteTexturePtr,
            ref int bufferIndex,
            ref BGRA wallTexturePtr,
            int textureYPos,
            byte brightness,
            bool flipY)
        {
            // reuse the cached column
            if (bufferIndex == textureYPos)
            {
                return;
            }

            const uint Alpha = (uint)byte.MaxValue << 24;

            bufferIndex = textureYPos;

            ref BGRA columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);
            uint scale = (uint)brightness;

            if (flipY)
            {
                for (int i = spriteTexturePtr.Length - 1; i >= 0; i--)
                {
                    if (columnPtr.IsTransparent)
                    {
                        spriteTexturePtr[i] = default;
                    }
                    else
                    {
                        uint b = columnPtr.B * scale >> 8;
                        uint g = columnPtr.G * scale >> 8 << 8;
                        uint r = columnPtr.R * scale >> 8 << 16;
                        spriteTexturePtr[i] = b | g | r | Alpha;
                    }

                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
            else
            {
                for (int i = 0; i < spriteTexturePtr.Length; i++)
                {
                    if (columnPtr.IsTransparent)
                    {
                        spriteTexturePtr[i] = default;
                    }
                    else
                    {
                        uint b = columnPtr.B * scale >> 8;
                        uint g = columnPtr.G * scale >> 8 << 8;
                        uint r = columnPtr.R * scale >> 8 << 16;
                        spriteTexturePtr[i] = b | g | r | Alpha;
                    }

                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
        }
    }
}
