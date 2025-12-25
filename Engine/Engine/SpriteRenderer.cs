using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void DrawSprite(ReadOnlySpan<Sector> sectors, Sprite sprite, SectorSprites renderableWall)
        {
            ref Texture texture = ref TextureCache.GetTextureOrNullRef(sprite.TextureName);

            if (Unsafe.IsNullRef(ref texture))
            {
                return;
            }

            ref uint screenPtr = ref GetScreenPtr<uint>();
            ref BGRA texturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Rotated);

            int width = PixelWidth;
            int textureWidth = texture.Height;

            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            Sector sector = sectors[sprite.SectorId];
            byte lightLevel = sector.LightLevel;

            float rx1 = sprite.R1.X;

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

            Span<uint> columnBuffer = this.columnA.AsSpan(..textureWidth);
            ref uint columnBufferPtr = ref MemoryMarshal.GetReference(columnBuffer);

            float textureXIncr = (((float)textureWidth) / (spriteEndY - spriteStartY));

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

                // Calculate Middle Texture Position
                int textureYPos = textureXLocation * textureWidth;
                float textureXPos = (clamptedFromY - spriteStartY) * textureXIncr;

                CalculateSprite(columnBuffer, ref this.columnABufferIndex, ref texturePtr, textureYPos, lightLevel);

                DrawSpriteLine(width, x, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                    ref screenPtr, ref columnBufferPtr);
            }

            int CalculateTextureXPosition(float cameraRay)
            {
                float fromToXDist = fromToYDist * cameraRay;
                float distX = rx1 - fromToXDist;

                return float.ConvertToIntegerNative<int>(MathF.Abs(distX));
            }
        }

        private void DrawTransparentWall(
            ReadOnlySpan<Sector> sectors,
            TransparentWall renderableWall)
        {
            int width = PixelWidth;
            Wall wall = renderableWall.Wall;
            Line line = wall.Line;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Sector sector = wall.Sector;
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

            int yOffset = textureInfo.YOffset > sectorHeight ? textureInfo.YOffset - 65536 : textureInfo.YOffset;
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

            ref uint screenPtr = ref GetScreenPtr<uint>();

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                ref RenderWindow renderWindow = ref window[x];

                if (renderWindow.CanRenderPortal)
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
                        textureStartY = renderFromTop ? textureStartY - yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY - yOffsetF : textureEndY - yOffsetF;
                    }
                }

                int textureStartYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureStartY), renderWindow.CeilingStart, renderWindow.FloorEnd);
                int textureEndYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureEndY), renderWindow.CeilingStart, renderWindow.FloorEnd);

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

            unchecked
            {
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
