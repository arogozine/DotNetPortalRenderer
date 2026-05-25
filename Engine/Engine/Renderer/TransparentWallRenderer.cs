
using RenderingEngine.Models;
using RenderingEngine.Tooling;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private unsafe void DrawTransparentWall(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo textureInfo = wall.MiddleTexture!;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ushort* repeatedCount;
            if (textureInfo.XScale is not null)
            {
                repeatedCount = CalculateTransparentWallBuild(sectors, renderableWall);
            }
            else
            {
                repeatedCount = CalculateTransparentWallDoom(sectors, renderableWall);
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount, wallToX - wallFromX + 1);

            Sector sector = wall.Sector;

            DrawSpriteShared(sector, renderableWall.Wall, repeatedCount, wallFromX, wallToX, false, textureInfo);
        }

        private unsafe ushort* CalculateTransparentWallDoom(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            int width = PixelWidth;

            int bufferOffset = PixelWidth * (renderableWall.Depth + 1);

            Span<float> distanceSpan = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<RenderColumnStatus> columnStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[bufferOffset..];

            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            RenderableWall wall = renderableWall.Wall;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);


            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);
            float oneOverSectorHeight = 1f / sectorHeight;

            // Texture Calculations
            TextureInfo? textureInfo = wall.MiddleTexture;
            Debug.Assert(textureInfo != null);
            Texture texture = TextureCache.GetTexture(textureInfo);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;
            bool texHeightDivisible2 = SharedHelpers.IsPowerOfTwo(textureHeight);
            if (texHeightDivisible2)
            {
                textureHeight--;
            }

            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset > sectorHeight ? textureInfo.YOffset - 65536 : textureInfo.YOffset;

            int* textureXLocationPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncrementPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);

            int* portalFromClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            int* portalToClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);
            ushort* repeatedCountPtr = this.memoryPool.GetBucketPtr<ushort>(MemoryPoolBucket.Temp);

            int length = wallToX - wallFromX;

            bool renderFromTop = textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromTop);


            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus status = columnStatus[x];

                if (status.PortalRenderable)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
                    continue;
                }

                float dist = distanceSpan[x];
                int floorEnd = wallEnd[x];
                int ceilingStart = wallStart[x];

                (float distance, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                if (fromToYdist > dist)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
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
                        // TODO: same expression in true and false branch
                        textureStartY = renderFromTop ? textureStartY - yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY - yOffsetF : textureEndY - yOffsetF;
                    }
                }

                int textureStartYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureStartY), ceilingStart, floorEnd);
                int textureEndYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureEndY), ceilingStart, floorEnd);

                if (textureStartYClamped >= textureEndYClamped)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
                    continue;
                }

                float offset = textureStartYClamped - textureStartY;

                // Calculate Middle Texture Position
                float textureYIncr = sectorHeight / (wallEndY - wallStartY);
                int textureXPos = ((float.ConvertToIntegerNative<int>(distance) + xOffset) % textureHeight) * textureWidth;
                float textureYPos = MathF.FusedMultiplyAdd(textureYIncr, offset, textureWidth);


                textureYIncr *= (1 << 16);
                textureYPos *= (1 << 16);

                portalFromClampedPtr[x] = textureStartYClamped;
                portalToClampedPtr[x] = textureEndYClamped;

                textureXLocationPtr[x] = textureXPos;
                textureYLocationPtr[x] = float.ConvertToIntegerNative<uint>(textureYPos);
                textureYIncrementPtr[x] = float.ConvertToIntegerNative<uint>(textureYIncr);
                repeatedCountPtr[x - wallFromX] = (ushort)length;
            }

            return repeatedCountPtr;
        }

        private unsafe ushort* CalculateTransparentWallBuild(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            int width = PixelWidth;

            int offset = PixelWidth * renderableWall.Depth;

            Span<float> spriteDistance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[offset..];
            Span<RenderColumnStatus> columnStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[offset..];

            if (renderableWall.Depth > 1)
            {
                offset = PixelWidth * (renderableWall.Depth - 1);
            }

            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[offset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[offset..];

            RenderableWall wall = renderableWall.Wall;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);


            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);
            float oneOverSectorHeight = 1f / sectorHeight;

            // Texture Calculations
            TextureInfo? textureInfo = wall.MiddleTexture;
            Debug.Assert(textureInfo != null);
            Texture texture = TextureCache.GetTexture(textureInfo);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;
            bool texHeightDivisible2 = SharedHelpers.IsPowerOfTwo(textureHeight);
            if (texHeightDivisible2)
            {
                textureHeight--;
            }
            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;
            Sector sector = wall.Sector;
            (float xScale, float yScale) = (textureInfo.XScale!.Value, textureInfo.YScale!.Value);
            yScale = (sector.Ceil - sector.Floor) * yScale;
            xScale = xScale / wall.Length * texture.Width;

            int* textureXLocationPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncrementPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);

            int* portalFromClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            int* portalToClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);
            ushort* repeatedCountPtr = this.memoryPool.GetBucketPtr<ushort>(MemoryPoolBucket.Temp);

            int length = wallToX - wallFromX;

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus columnStatusY = columnStatus[x];

                if (columnStatusY.PortalRenderable)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
                    continue;
                }

                float distance = spriteDistance[x];
                int floorEndY = wallEnd[x];
                int ceilingStartY = wallStart[x];

                (float distanceY, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                if (fromToYdist > distance)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
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
                    repeatedCountPtr[x - wallFromX] = 0;
                    continue;
                }

                // Calculate Middle Texture Position
                int textureXPos = float.ConvertToIntegerNative<int>(distanceY * xScale);
                textureXPos += xOffset;
                textureXPos = texHeightDivisible2 ? (textureXPos & textureHeight) : (textureXPos % textureHeight);
                textureXPos *= textureWidth;

                float textureYIncr = (textureWidth * yScale) / (wallEndY - wallStartY);
                float textureYPos = yOffset - textureYIncr * (wallStartY - clampedFromY);

                textureYIncr *= (1 << 16);
                textureYPos *= (1 << 16);

                portalFromClampedPtr[x] = clampedFromY;
                portalToClampedPtr[x] = clampedToY;

                textureXLocationPtr[x] = textureXPos;
                textureYLocationPtr[x] = float.ConvertToIntegerNative<uint>(textureYPos);
                textureYIncrementPtr[x] = float.ConvertToIntegerNative<uint>(textureYIncr);
                repeatedCountPtr[x - wallFromX] = (ushort)length;
            }

            return repeatedCountPtr;
        }

    }
}
