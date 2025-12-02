using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void DrawSprite(Span<BGRA> screen, ReadOnlySpan<Sector> sectors, Sprite sprite, SectorSprites renderableWall)
        {
            ref Texture texture = ref TextureCache.GetTextureOrNullRef(sprite.TextureName);

            if (Unsafe.IsNullRef(ref texture))
            {
                return;
            }

            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);

            int width = PixelWidth;
            int height = PixelHeight;
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            Sector sector = sectors[sprite.SectorId];
            byte lightLevel = sector.LightLevel;

            float rx1 = sprite.R1.X;
            float rx2 = sprite.R2.X;
            float ry = sprite.Rotated.Y;

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteStartY = sprite.YLeftCeil;
            int spriteEndY = sprite.YLeftFloor;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            Span<int> floorEndArray = renderableWall.FloorEnd;
            Span<int> ceilingStartArray = renderableWall.CeilingStart;
            Span<float> distance = renderableWall.Distance;

            float d2x = textureHeight;
            float t1 = -ry * d2x;
            float fromToYDist = sprite.Distance;

            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * spriteFromX;

            float distIncr = texture.Width / (float)(xRight - xLeft);

            Span<uint> columnBuffer = this.columnA.AsSpan(..textureWidth);
            ref uint columnBufferPtr = ref MemoryMarshal.GetReference(columnBuffer);

            float textureXIncr = (((float)textureWidth) / (spriteEndY - spriteStartY));

            for (int x = spriteFromX; x < spriteToX; x++, cameraRay += cameraWidthIncr)
            {
                int ceilingStart = ceilingStartArray[x];
                int floorEnd = floorEndArray[x];

                if (floorEnd <= ceilingStart)
                {
                    continue;
                }

                if (distance[x] < fromToYDist)
                {
                    continue;
                }

                int clamptedFromY = Math.Clamp(spriteStartY, ceilingStart, floorEnd);
                int clamptedToY = Math.Clamp(spriteEndY, ceilingStart, floorEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    continue;
                }

                int textureXLocation = CalculateTextureXPosition(cameraRay, t1, d2x);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, clamptedToY * width + x);

                // Calculate Middle Texture Position
                int textureYPos = textureXLocation * textureWidth;
                float textureXPos = (clamptedFromY - spriteStartY) * textureXIncr;

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel);

                int textureXPosI = 0;

                for (uint shaded = Unsafe.Add(ref columnBufferPtr, textureXPosI);
                     Unsafe.IsAddressGreaterThan(ref screenIndexPtrEnd, ref screenIndexPtr);
                     textureXPos += textureXIncr, screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width))
                {
                    textureXPosI = (int)textureXPos;
                    shaded = Unsafe.Add(ref columnBufferPtr, textureXPosI);

                    if (shaded != 0U)
                    {
                        screenIndexPtr = shaded;
                    }
                }
            }

            int CalculateTextureXPosition(float cameraRay, float t1, float d2x)
            {
                float fromToXDist = fromToYDist * cameraRay;
                float distX = rx1 - fromToXDist;

                return (int)MathF.Abs(distX);
            }
        }

        private void DrawTransparentWall(
            Span<BGRA> screen,
            ReadOnlySpan<Sector> sectors,
            TransparentWall renderableWall)
        {
            int width = PixelWidth;
            Wall wall = renderableWall.Wall;
            Line line = wall.Line;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Sector sector = renderableWall.Sector;
            float sectorHeight = sector.Ceil - sector.Floor;


            Span<RenderWindow> window = renderableWall.RenderWindow!;

            TextureInfo textureInfo = line.MiddleTexture!;
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

            int yOffset = textureInfo.YOffset;
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

            int xOffset = DetermineXOffset(textureInfo, ref texture);

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

                (int distance, float fromToYdist) = CalculateDistance(wall, cameraRay, t1, d2y, d2x);

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
                        textureStartY = renderFromTop ? textureStartY + yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY + yOffsetF : textureEndY - yOffsetF;
                    }
                }

                int textureStartYClamped = Math.Clamp((int)textureStartY, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int textureEndYClamped = Math.Clamp((int)textureEndY, renderWindow.CeilingStart, renderWindow.FloorEnd);

                if (textureStartYClamped >= textureEndYClamped)
                {
                    continue;
                }

                float offset = textureStartYClamped - textureStartY;

                // Calculate Middle Texture Position
                float textureXIncr = (float)(sectorHeight / (wallEndY - wallStartY));
                int textureYPos = ((distance + xOffset) % textureHeight) * textureWidth;
                float textureXPos = MathF.FusedMultiplyAdd(textureXIncr, offset, textureWidth);


                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel);

                if (alpha == 1f)
                {
                    DrawTransparentWallLine(textureWidth, ref columnBufferPtr, ref screenPtr, x, textureStartYClamped, textureEndYClamped, textureXIncr, ref textureXPos);
                }
                else
                {
                    DrawTransparentWallLineWithAlpha(textureWidth, ref columnBufferPtr, ref screenPtr, x, textureStartYClamped, textureEndYClamped, textureXIncr, ref textureXPos, alpha);
                }
            }

            columnABufferIndex = EngineConstants.Unset;
        }

        private void DrawTransparentWallLine(int textureWidth, ref uint columnBufferPtr, ref uint screenPtr, int x, int textureStartYClamped, int textureEndYClamped, float textureXIncr, ref float textureXPos)
        {
            uint shaded = default;
            int textureXPosIOld = -1;

            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, textureStartYClamped * PixelWidth + x);
            ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, textureEndYClamped * PixelWidth + x);

            for (; Unsafe.IsAddressGreaterThan(ref screenIndexPtrEnd, ref screenIndexPtr);
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

        private void DrawTransparentWallLineWithAlpha(int textureWidth, ref uint columnBufferPtr, ref uint screenPtr, int x, int textureStartYClamped, int textureEndYClamped, float textureXIncr, ref float textureXPos, float alpha)
        {
            uint a = (uint)(alpha * byte.MaxValue);
            uint aInv = byte.MaxValue - a;

            BGRA shaded = default;
            int textureXPosIOld = -1;

            ref BGRA screenIndexPtrBgra = ref Unsafe.As<uint, BGRA>(ref screenPtr);
            ref BGRA screenIndexPtr = ref Unsafe.Add(ref screenIndexPtrBgra, textureStartYClamped * PixelWidth + x);
            ref BGRA screenIndexPtrEnd = ref Unsafe.Add(ref screenIndexPtrBgra, textureEndYClamped * PixelWidth + x);

            for (; Unsafe.IsAddressGreaterThan(ref screenIndexPtrEnd, ref screenIndexPtr);
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
                    screenIndexPtr = BlendBGRA(ref screenIndexPtr, ref shaded, a, aInv);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint BlendBGRA(ref BGRA bgraDst, ref BGRA bgraSrc, uint a, uint aInv)
        {
            const uint Alpha = (uint)byte.MaxValue << 24;

            unchecked
            {
                // if (a == 0) return bgraDst.Value;
                // if (a == 255) return bgraSrc.Value;

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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateSprite(scoped Span<uint> spriteTexturePtr, ref int bufferIndex, ref BGRA wallTexturePtr, int textureYPos, byte brightness)
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
                spriteTexturePtr.Fill(Alpha);
                return;
            }

            ref BGRA columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);
            uint scale = (uint)brightness;

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
        }
    }
}
