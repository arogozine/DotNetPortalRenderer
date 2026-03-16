using RenderingEngine.Models;
using RenderingEngine.Tooling;

namespace RenderingEngine.Engine
{

    internal sealed partial class PortalRenderer
    {
        private void DrawSprite(
            PortalPlayerSnapshot player,
            ReadOnlySpan<Sector> sectors,
            RenderableSprite sprite,
            RenderWindowSpriteSnapshot renderableWall)
        {
            switch (sprite)
            {
                case RenderableWallSprite renderableWallSprite:
                    DrawWallSprite(sectors, renderableWallSprite, renderableWall);
                    break;
                case RenderableFloorSprite renderableFloorSprite:
                    DrawFloorSprite(player, sectors, renderableFloorSprite, renderableWall);
                    break;
                case RenderableBasicSprite renderableSprite:
                    DrawSprite(sectors, renderableSprite, renderableWall);
                    break;
            }
        }

        private void DrawSprite(ReadOnlySpan<Sector> sectors, RenderableBasicSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            TextureInfo texture = sprite.Texture;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            int width = PixelWidth;
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            Sector sector = sectors[sprite.SectorId];

            ref uint texturePtr = ref texture.Texture.GetBinaryRef<uint>(true, sprite.Shade ?? sector.FloorShade);

            float rx1 = sprite.R1.X;
            float rx2 = sprite.R2.X;

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteStartY = sprite.YLeftCeil;
            int spriteEndY = sprite.YLeftFloor;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            Span<int> wallStartSpan = renderableWall.WallStart;
            Span<int> wallEndSpan = renderableWall.WallEnd;
            Span<float> distance = renderableWall.Depth;

            float fromToYDist = sprite.DistanceMin;

            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * spriteFromX;

            int length = spriteToX - spriteFromX;

            using TempBuffer<uint> tempBuffer = TempBuffer<uint>.GetBuffer(textureWidth);
            Span<int> textureYPosArray = TempBuffer<int>.GetBuffer(length);
            Span<int> textureXPosArray = TempBuffer<int>.GetBuffer(length);
            Span<int> clampedFromYArray = TempBuffer<int>.GetBuffer(length);
            Span<int> clampedToYArray = TempBuffer<int>.GetBuffer(length);
            Span<ushort> repeatedCount = TempBuffer<ushort>.GetBuffer(length);
            Span<ushort> repeatedCountB = TempBuffer<ushort>.GetBuffer(length);

            int textureXIncr = (textureWidth << 16) / (spriteEndY - spriteStartY);

            bool flipY = sprite.Texture.RenderingOptions.IsFlippedY;
            bool flipX = sprite.Texture.RenderingOptions.IsFlippedX;

            float textureLen = texture.Width / sprite.Length;
            repeatedCount.Fill((ushort)length);

            // offset to start at 0
            wallStartSpan = wallStartSpan[spriteFromX..];
            wallEndSpan = wallEndSpan[spriteFromX..];
            distance = distance[spriteFromX..];

            for (int x = 0; x < length; x++, cameraRay += cameraWidthIncr)
            {
                int wallStart = wallStartSpan[x];
                int wallEnd = wallEndSpan[x];

                if (wallEnd <= wallStart || distance[x] < fromToYDist)
                {
                    clampedFromYArray[x] = 0;
                    clampedToYArray[x] = 0;
                    repeatedCount[x] = 0;

                    continue;
                }

                int clamptedFromY = Math.Clamp(spriteStartY, wallStart, wallEnd);
                int clamptedToY = Math.Clamp(spriteEndY, wallStart, wallEnd);
                clampedFromYArray[x] = clamptedFromY;
                clampedToYArray[x] = clamptedToY;

                if (clamptedFromY >= clamptedToY)
                {
                    repeatedCount[x] = 0;
                    continue;
                }

                int textureXLocation = CalculateTextureXPosition(cameraRay);
                if (textureXLocation >= textureHeight) {
                    textureXLocation = 0;
                }
                textureYPosArray[x] = textureXLocation * textureWidth;
                textureXPosArray[x] = (clamptedFromY - spriteStartY) * textureXIncr;
            }

            // basic sprites are always facing the player
            // so their start and end Y position is the same
            // throughout.
            // here we determine how many columns can be rendered
            // horizontally with the same texture pixel
            bool repeat =
                SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount)
                && SharedHelpers.PopulateRepeatedValues(repeatedCountB, textureYPosArray)
                && SharedHelpers.RefineRepeatedValues(repeatedCount, repeatedCountB)
                && SharedHelpers.PopulateRepeatedValues(repeatedCountB, textureXPosArray)
                && SharedHelpers.RefineRepeatedValues(repeatedCount, repeatedCountB)
                && SharedHelpers.PopulateRepeatedValues(repeatedCountB, clampedFromYArray)
                && SharedHelpers.RefineRepeatedValues(repeatedCount, repeatedCountB)
                && SharedHelpers.PopulateRepeatedValues(repeatedCountB, clampedToYArray);

            for (int x = 0; x < length; x++)
            {
                ushort count = repeatedCount[x];

                if (count == 0)
                {
                    continue;
                }

                int clamptedFromY = clampedFromYArray[x];
                int clamptedToY = clampedToYArray[x];
                int textureYPos = textureYPosArray[x];
                int textureXPos = textureXPosArray[x];

                CalculateSprite(tempBuffer, ref texturePtr, textureYPos, flipY);

                if (repeat && count > 1)
                {
                    DrawSpriteLine(count, width, x + spriteFromX, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                        ref screenPtr, ref tempBuffer.Pointer);
                    x += count - 1;
                    continue;
                }

                DrawSpriteLine(width, x + spriteFromX, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                    ref screenPtr, ref tempBuffer.Pointer);
            }

            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int CalculateTextureXPosition(float cameraRay)
            {
                float distX =
                    flipX ? MathF.FusedMultiplyAdd(-fromToYDist, cameraRay, rx2): MathF.FusedMultiplyAdd(fromToYDist, cameraRay, - rx1);

                return float.ConvertToIntegerNative<int>(MathF.Abs(distX) * textureLen);
            }
        }


        private void DrawWallSprite(ReadOnlySpan<Sector> sectors, RenderableWallSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            TextureInfo texture = sprite.Texture;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            int width = PixelWidth;
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;
            // optimize to avoid "%" when possible
            bool texHeightDivisible2 = SharedHelpers.IsPowerOfTwo(textureHeight);
            if (texHeightDivisible2)
            {
                textureHeight--;
            }

            Sector sector = sectors[sprite.SectorId];

            ref uint texturePtr = ref texture.Texture.GetBinaryRef<uint>(true, sprite.Shade ?? sector.FloorShade);

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(sprite, 0);
            float spriteStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float spriteEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            int xOffset = 0;

            ReadOnlySpan<int> wallStart = renderableWall.WallStart;
            ReadOnlySpan<int> wallEnd = renderableWall.WallEnd;
            ReadOnlySpan<float> distance = renderableWall.Depth;

            using TempBuffer<uint> tempBuffer = TempBuffer<uint>.GetBuffer(textureWidth);

            bool flipY = sprite.Texture.RenderingOptions.IsFlippedY;
            bool flipX = sprite.Texture.RenderingOptions.IsFlippedX;

            float xScale = texture.Width / sprite.Length;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(sprite, width, spriteFromX);

            for (int x = spriteFromX; x < spriteToX; x++, cameraRay += cameraWidthIncr, spriteStartY += ceilDistIncr, spriteEndY += floorDistIncr)
            {
                int ceilingStart = wallStart[x];
                int floorEnd = wallEnd[x];

                if (floorEnd <= ceilingStart)
                {
                    continue;
                }

                int spriteStartY_Int = float.ConvertToIntegerNative<int>(spriteStartY);
                int spriteEndY_Int = float.ConvertToIntegerNative<int>(spriteEndY);
                int clamptedFromY = Math.Clamp(spriteStartY_Int, ceilingStart, floorEnd);
                int clamptedToY = Math.Clamp(spriteEndY_Int, ceilingStart, floorEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    continue;
                }

                (float textureXLocation, float fromToYdist) = MathFormulas.CalculateDistance(sprite, cameraRay, t1, d2y, d2x, flipX);

                if ((int)distance[x] < (int)fromToYdist)
                {
                    continue;
                }

                textureXLocation += xOffset;
                textureXLocation *= xScale;
                int textureXIncr = (textureWidth << 16) / (spriteEndY_Int - spriteStartY_Int);
                int textureYPos = float.ConvertToIntegerNative<int>(textureXLocation);
                textureYPos = texHeightDivisible2 ? (textureYPos & textureHeight) : (textureYPos % textureHeight);
                textureYPos *= textureWidth;

                int textureXPos = (clamptedFromY - spriteStartY_Int) * textureXIncr;

                CalculateSprite(tempBuffer, ref texturePtr, textureYPos, flipY);

                DrawSpriteLine(width, x, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                    ref screenPtr, ref tempBuffer.Pointer);
            }
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

            Texture texture = TextureCache.GetTexture(textureInfo);
            ref uint texturePtr = ref texture.GetBinaryRef<uint>(true, sector.FloorShade);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            using TempBuffer<uint> tempBuffer = TempBuffer<uint>.GetBuffer(textureWidth);

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);
            Sector neighborSector = sectors[wall.Neighbor];

            float oneOverSectorHeight = 1f / sectorHeight;

            bool renderFromTop = textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromTop);
            float alpha = Math.Clamp(textureInfo.Alpha, 0f, 1f);

            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset > sectorHeight ? textureInfo.YOffset - 65536 : textureInfo.YOffset;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus columnStatus = renderableWall.ColumnStatus[x];

                if (columnStatus.PortalRenderable)
                {
                    continue;
                }

                float buffer = RenderWindowHelper.Distance[x];
                int floorEnd = renderableWall.WallEnd[x];
                int ceilingStart = renderableWall.WallStart[x];

                (float distance, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x, false);

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
                int textureYPos = ((float.ConvertToIntegerNative<int>(distance) + xOffset) % textureHeight) * textureWidth;

                float textureXPos = MathF.FusedMultiplyAdd(textureXIncr, offset, textureWidth);

                CalculateSprite(tempBuffer, ref texturePtr, textureYPos, false);

                if (alpha == 1f)
                {
                    DrawTransparentWallLine(width, x,
                        textureStartYClamped, textureEndYClamped,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref tempBuffer.Pointer);
                }
                else
                {
                    DrawTransparentWallLineWithAlpha(width, x,
                        textureStartYClamped, textureEndYClamped,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref tempBuffer.Pointer,
                        alpha);
                }
            }
        }

        private void DrawTransparentWall_Build(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            ReadOnlySpan<int> wallStart = renderableWall.WallStart;
            ReadOnlySpan<int> wallEnd = renderableWall.WallEnd;
            ReadOnlySpan<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);
            ReadOnlySpan<RenderColumnStatus> columnStatus = renderableWall.ColumnStatus;

            int width = PixelWidth;
            RenderableWall wall = renderableWall.Wall;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Sector sector = wall.Sector;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            TextureInfo textureInfo = wall.MiddleTexture!;
            Texture texture = TextureCache.GetTexture(textureInfo);
            ref uint texturePtr = ref texture.GetBinaryRef<uint>(true, sector.FloorShade);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;
            // optimize to avoid "%" when possible
            bool texHeightDivisible2 = SharedHelpers.IsPowerOfTwo(textureHeight);
            if (texHeightDivisible2)
            {
                textureHeight--;
            }

            using TempBuffer<uint> tempBuffer = TempBuffer<uint>.GetBuffer(textureWidth);

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);
            float oneOverSectorHeight = 1f / sectorHeight;

            float alpha = Math.Clamp(textureInfo.Alpha, 0f, 1f);

            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;

            (_, bool flipX, bool flipY) = GetFlags(textureInfo);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

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
                int floorEndY = wallEnd[x];
                int ceilingStartY = wallStart[x];

                (float distanceY, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x, flipX);

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
                textureYPos += xOffset;
                textureYPos = texHeightDivisible2 ? (textureYPos & textureHeight) : (textureYPos % textureHeight);
                textureYPos *= textureWidth;

                float textureXIncr = (textureWidth * yScale) / (wallEndY - wallStartY);
                float textureXPos = yOffset - textureXIncr * (wallStartY - clampedFromY);

                CalculateSprite(tempBuffer, ref texturePtr, textureYPos, flipY);

                if (alpha == 1f)
                {
                    DrawTransparentWallLine(width, x,
                        clampedFromY, clampedToY,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref tempBuffer.Pointer);
                }
                else
                {
                    DrawTransparentWallLineWithAlpha(width, x,
                        clampedFromY, clampedToY,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref tempBuffer.Pointer,
                        alpha);
                }
            }
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
            ref readonly BGRA screenIndexPtrEnd = ref Unsafe.Add(ref screenIndexPtrBgra, textureEndYClamped * width + x);

            uint textureXPos_u = float.ConvertToIntegerNative<uint>(textureXPos * (1 << 16));
            uint textureXIncr_u = float.ConvertToIntegerNative<uint>(textureXIncr * (1 << 16));
            uint textureHeight_u = (uint)textureHeight;

            while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
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
            int count,
            int width,
            int x,
            int textureStartYClamped, int textureEndYClamped,
            int textureXPos,
            int textureXIncr,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
        )
        {
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, textureStartYClamped * width + x);
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, textureEndYClamped * width + x);
            uint textureXPos_u = (uint)(textureXPos);
            uint textureXIncr_u = (uint)(textureXIncr);

            while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
            {
                uint texelIndex = textureXPos_u >> 16;
                uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                // skip rendering the whole row is transparent
                if (shaded != 0U)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Unsafe.Add(ref screenIndexPtr, i) = shaded;
                    }
                }

                screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                textureXPos_u += textureXIncr_u;
            }
        }


        private static void DrawSpriteLine(
                int width,
                int x,
                int textureStartYClamped, int textureEndYClamped,
                int textureXPos,
                int textureXIncr,
                scoped ref uint screenPtr,
                scoped ref uint textureBuffer
                )
        {
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, textureStartYClamped * width + x);
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, textureEndYClamped * width + x);
            uint textureXPos_u = (uint)(textureXPos);
            uint textureXIncr_u = (uint)(textureXIncr);

            while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
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
        public static uint BlendBGRA(ref readonly BGRA bgraDst, ref readonly BGRA bgraSrc, uint a, uint aInv)
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
            TempBuffer<uint> buffer,
            ref uint wallTexturePtr,
            int textureYPos,
            bool flipY)
        {
            // reuse the cached column
            if (buffer.Index == textureYPos)
            {
                return;
            }

            buffer.Index = textureYPos;

            Span<uint> spriteTexturePtr = buffer.Span;
            ref uint columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);

            if (flipY)
            {
                for (int i = spriteTexturePtr.Length - 1; i >= 0; i--)
                {
                    spriteTexturePtr[i] = columnPtr;
                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
            else
            {
                for (int i = 0; i < spriteTexturePtr.Length; i++)
                {
                    spriteTexturePtr[i] = columnPtr;
                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
        }
    }
}
