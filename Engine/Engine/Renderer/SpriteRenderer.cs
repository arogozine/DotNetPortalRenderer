using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Numerics;

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
            int bufferOffset = PixelWidth * renderableWall.Depth;

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<int> wallStartSpan = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEndSpan = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            TextureInfo texture = sprite.Texture;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            int width = PixelWidth;
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            bool flipY = sprite.Texture.RenderingOptions.IsFlippedY;
            bool flipX = sprite.Texture.RenderingOptions.IsFlippedX;

            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            Sector sector = sectors[sprite.SectorId];

            var transform = TextureTransform.Rotated;

            if (flipY)
            {
                transform |= TextureTransform.FlippedY;
            }

            if (flipX)
            {
                transform |= TextureTransform.FlippedX;
            }

            ref uint texturePtr = ref texture.Texture.GetBinaryRef<uint>(sprite.Shade ?? sector.FloorShade,
                transform);

            float rx1 = sprite.R1.X;
            float rx2 = sprite.R2.X;

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteStartY = sprite.YLeftCeil;
            int spriteEndY = sprite.YLeftFloor;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            float fromToYDist = sprite.DistanceMin;

            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * spriteFromX;

            int length = spriteToX - spriteFromX;

            Span<int> textureYPosArray = TempBuffer<int>.GetBuffer(length);
            Span<int> textureXPosArray = TempBuffer<int>.GetBuffer(length);
            Span<int> clampedFromYArray = TempBuffer<int>.GetBuffer(length);
            Span<int> clampedToYArray = TempBuffer<int>.GetBuffer(length);
            Span<ushort> repeatedCount = TempBuffer<ushort>.GetBuffer(length);
            Span<ushort> repeatedCountB = TempBuffer<ushort>.GetBuffer(length);

            int textureXIncr = (textureWidth << 16) / (spriteEndY - spriteStartY);

            float textureLen = texture.Width / sprite.Length;
            repeatedCount.Fill((ushort)length);

            // offset to start at 0
            wallStartSpan = wallStartSpan[spriteFromX..];
            wallEndSpan = wallEndSpan[spriteFromX..];
            distance = distance[spriteFromX..];

            ushort maxCount = (ushort)length;
            for (int x = 0; x < length; x++, maxCount--, cameraRay += cameraWidthIncr)
            {
                int wallStart = wallStartSpan[x];
                int wallEnd = wallEndSpan[x];

                if (wallEnd <= wallStart || distance[x] < fromToYDist)
                {
                    repeatedCount[x] = 0;
                    maxCount = (ushort)x;
                    continue;
                }

                int clamptedFromY = Math.Clamp(spriteStartY, wallStart, wallEnd);
                int clamptedToY = Math.Clamp(spriteEndY, wallStart, wallEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    repeatedCount[x] = 0;
                    maxCount = (ushort)x;
                    continue;
                }

                int textureXLocation = CalculateTextureXPosition(cameraRay);
                if (textureXLocation >= textureHeight) {
                    textureXLocation = 0;
                }

                textureYPosArray[x] = textureXLocation * textureWidth;
                textureXPosArray[x] = (clamptedFromY - spriteStartY) * textureXIncr;
                clampedFromYArray[x] = clamptedFromY;
                clampedToYArray[x] = clamptedToY;
                repeatedCount[x] = maxCount;
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

                if (repeat && count > 1)
                {
                    DrawSpriteLine(count, width, x + spriteFromX, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                        ref screenPtr, ref Unsafe.Add(ref texturePtr, textureYPos));
                    x += count - 1;
                    continue;
                }

                DrawSpriteLine(width, x + spriteFromX, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                    ref screenPtr, ref Unsafe.Add(ref texturePtr, textureYPos));
            }

            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int CalculateTextureXPosition(float cameraRay)
            {
                float distX = MathF.FusedMultiplyAdd(fromToYDist, cameraRay, - rx1);

                return float.ConvertToIntegerNative<int>(MathF.Abs(distX) * textureLen);
            }
        }


        private void DrawWallSprite(ReadOnlySpan<Sector> sectors, RenderableWallSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            int bufferOffset = PixelWidth * renderableWall.Depth;

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

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

            bool flipY = sprite.Texture.RenderingOptions.IsFlippedY;
            bool flipX = sprite.Texture.RenderingOptions.IsFlippedX;

            var transform = TextureTransform.Rotated;

            if (flipY)
            {
                transform |= TextureTransform.FlippedY;
            }

            if (flipX)
            {
                transform |= TextureTransform.FlippedX;
            }

            ref uint texturePtr = ref texture.Texture.GetBinaryRef<uint>(sprite.Shade ?? sector.FloorShade, transform);

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(sprite, 0);
            float spriteStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float spriteEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            Debug.Assert(renderableWall.XLeft <= spriteFromX);

            int xOffset = 0;

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

                (float textureXLocation, float fromToYdist) = MathFormulas.CalculateDistance(sprite, cameraRay, t1, d2y, d2x);

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

                DrawSpriteLine(width, x, clamptedFromY, clamptedToY, textureXPos, textureXIncr,
                    ref screenPtr, ref Unsafe.Add(ref texturePtr, textureYPos));
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

            int bufferOffset = PixelWidth * renderableWall.Depth;

            Span<float> distanceSpan = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<RenderColumnStatus> columnStatusSpan = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[bufferOffset..];

            if (renderableWall.Depth > 1)
            {
                bufferOffset = PixelWidth * (renderableWall.Depth + 1);
            }

            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            Sector sector = wall.Sector;
            int width = PixelWidth;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            Texture texture = TextureCache.GetTexture(textureInfo);
            ref uint texturePtr = ref texture.GetBinaryRef<uint>(sector.FloorShade, TextureTransform.Rotated);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);

            float oneOverSectorHeight = 1f / sectorHeight;

            bool renderFromTop = textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromTop);
            float alpha = Math.Clamp(textureInfo.Alpha, 0f, 1f);

            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset > sectorHeight ? textureInfo.YOffset - 65536 : textureInfo.YOffset;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus columnStatus = columnStatusSpan[x];

                if (columnStatus.PortalRenderable)
                {
                    continue;
                }

                float buffer = distanceSpan[x];
                int floorEnd = wallEnd[x];
                int ceilingStart = wallStart[x];

                (float distance, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x);

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

                if (alpha == 1f)
                {
                    DrawTransparentWallLine(width, x,
                        textureStartYClamped, textureEndYClamped,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref Unsafe.Add(ref texturePtr, textureYPos));
                }
                else
                {
                    DrawTransparentWallLineWithAlpha(width, x,
                        textureStartYClamped, textureEndYClamped,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref Unsafe.Add(ref texturePtr, textureYPos),
                        alpha);
                }
            }
        }

        private void DrawTransparentWall_Build(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            int offset = PixelWidth * renderableWall.Depth;

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[offset..];
            Span<RenderColumnStatus> columnStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[offset..];

            if (renderableWall.Depth > 1)
            {
                offset = PixelWidth * (renderableWall.Depth - 1);
            }

            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[offset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[offset..];


            int width = PixelWidth;
            RenderableWall wall = renderableWall.Wall;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Sector sector = wall.Sector;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            TextureInfo textureInfo = wall.MiddleTexture!;
            (_, bool flipX, bool flipY) = GetFlags(textureInfo);

            var transform = TextureTransform.Rotated;

            if (flipY)
            {
                transform |= TextureTransform.FlippedY;
            }

            if (flipX)
            {
                transform |= TextureTransform.FlippedX;
            }

            Texture texture = TextureCache.GetTexture(textureInfo);
            ref uint texturePtr = ref texture.GetBinaryRef<uint>(sector.FloorShade, transform);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;
            // optimize to avoid "%" when possible
            bool texHeightDivisible2 = SharedHelpers.IsPowerOfTwo(textureHeight);
            if (texHeightDivisible2)
            {
                textureHeight--;
            }

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

                (float distanceY, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x);

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

                if (alpha == 1f)
                {
                    DrawTransparentWallLine(width, x,
                        clampedFromY, clampedToY,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref Unsafe.Add(ref texturePtr, textureYPos));
                }
                else
                {
                    DrawTransparentWallLineWithAlpha(width, x,
                        clampedFromY, clampedToY,
                        textureWidth,
                        textureXPos, textureXIncr,
                        ref screenPtr, ref Unsafe.Add(ref texturePtr, textureYPos),
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
    }
}
