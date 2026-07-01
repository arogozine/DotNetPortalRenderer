using RenderingEngine.Tooling;
using SoftwareRendererModels;

namespace RenderingEngine.Engine;

internal unsafe sealed class BuildRenderer : PortalRenderer
{
    public BuildRenderer(int width, int height) : base(width, height)
    {
    }

    protected override (int xOffset, int yOffset, XyOpts Opts) DetermineOffsets(
        GameTextureInfo textureInfo)
    {
        int textureWidth = textureInfo.Width;
        int textureHeight = textureInfo.Height;

        XyOpts xyOpts = XyOpts.None;
        int xOffset = textureInfo.XOffset;
        int yOffset = textureInfo.YOffset;

        bool doubleSize = textureInfo.XScale == 2 && textureInfo.YScale == 2;

        const TextureRenderingOptions mask = TextureRenderingOptions.FlipX | TextureRenderingOptions.FlipY | TextureRenderingOptions.SwapXY;

        switch (textureInfo.RenderingOptions & mask)
        {
            case TextureRenderingOptions.FlipX:
                xOffset = textureWidth - xOffset;
                xyOpts = XyOpts.FlipX;
                break;
            case TextureRenderingOptions.FlipX | TextureRenderingOptions.FlipY:
                yOffset = textureHeight - yOffset;
                break;
            case TextureRenderingOptions.FlipX | TextureRenderingOptions.SwapXY:
                xyOpts = XyOpts.SwapXY | XyOpts.FlipY;
                break;
            case TextureRenderingOptions.FlipY:
                xyOpts = XyOpts.FlipY;
                break;
            case TextureRenderingOptions.FlipY | TextureRenderingOptions.SwapXY:
                xOffset = textureWidth - xOffset;
                xyOpts = XyOpts.SwapXY | XyOpts.FlipX;
                break;
            case TextureRenderingOptions.SwapXY:
                yOffset = textureHeight - yOffset;
                xyOpts = XyOpts.SwapXY | XyOpts.FlipY;
                break;
            case TextureRenderingOptions.FlipX | TextureRenderingOptions.FlipY | TextureRenderingOptions.SwapXY:
                xyOpts = XyOpts.SwapXY;
                break;
            case TextureRenderingOptions.None:
                xOffset = textureWidth - xOffset;
                yOffset = textureHeight - yOffset;
                break;
            default:
                throw new NotImplementedException();
        }

        if (doubleSize)
        {
            xOffset <<= 1;
            yOffset <<= 1;
            xyOpts |= XyOpts.DoubleSize;
        }

        return (xOffset, yOffset, xyOpts);
    }

    protected override ushort* CalculateTransparentWall(
        ReadOnlySpan<RenderableSector> sectors,
        RenderWindowWallSnapshot renderableWall)
    {
        int width = PixelWidth;

        int offset = PixelWidth * renderableWall.Depth;

        Span<float> spriteDistance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[offset..];
        Span<RenderColumnStatus> columnStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[offset..];

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
        GameTextureInfo? textureInfo = wall.MiddleTexture;
        Debug.Assert(textureInfo != null);
        GameTexture texture = TextureCache.GetTexture(textureInfo);
        int textureWidth = texture.Height;
        int textureHeight = texture.Width;
        bool texHeightDivisible2 = SharedHelpers.IsPowerOfTwo(textureHeight);
        if (texHeightDivisible2)
        {
            textureHeight--;
        }
        int xOffset = textureInfo.XOffset;
        int yOffset = textureInfo.YOffset;
        RenderableSector sector = wall.Sector;
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

    protected override void RenderSkyboxShared(PortalPlayerSnapshot player,
        RenderColumnStatus renderColumnStatus,
        uint* screenPtr,
        uint* texturePtr,
        int sectorFromX, int sectorToX,
        int* fromYPtr, int* toYPtr,
        int* ceilingStartPtr, int* floorEndPtr,
        int width,
        int textureWidth,
        int textureHeight)
    {
        float yTextureIncr = ((float)textureHeight) / PixelHeight;

        float* angleCachePtr = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.AngleCache);

        Span<ushort> repeatedCount = CaclulateRepeatedCount();
        _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

        bool isPowerOfTwo = SharedHelpers.IsPowerOfTwo(textureHeight);

        fixed (ushort* repeatedCountPtr = &repeatedCount[0])
        {

            if (isPowerOfTwo)
            {
                CoreRendererForPowTextures<DrawSimplePixel>.RenderSkybox(player, 1, repeatedCountPtr, angleCachePtr, screenPtr, texturePtr, sectorFromX, sectorToX, fromYPtr, toYPtr, ceilingStartPtr, floorEndPtr, width, textureWidth, textureHeight, yTextureIncr);
            }
            else
            {
                CoreRendererForOddTextures<DrawSimplePixel>.RenderSkybox(player, 1, repeatedCountPtr, angleCachePtr, screenPtr, texturePtr, sectorFromX, sectorToX, fromYPtr, toYPtr, ceilingStartPtr, floorEndPtr, width, textureWidth, textureHeight, yTextureIncr);
            }
        }

        return;

        Span<ushort> CaclulateRepeatedCount()
        {
            RenderColumnStatus* status = memoryPool.GetBucketPtr<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            int length = sectorToX - sectorFromX;

            Span<ushort> repeatedCount = memoryPool.GetBucket<ushort>(MemoryPoolBucket.Temp2)[..(length + 1)];

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.HasFlag(renderColumnStatus))
                {
                    repeatedCount[x - sectorFromX] = 0;
                    continue;
                }

                repeatedCount[x - sectorFromX] = (ushort)length;
            }

            return repeatedCount;
        }
    }

}
