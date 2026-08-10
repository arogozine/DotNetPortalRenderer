using RenderingEngine.Tooling;
using SoftwareRendererModels;

namespace RenderingEngine.Engine;

internal unsafe sealed class DoomRenderer : PortalRenderer
{
    public DoomRenderer(int width, int height, CancellationToken cancellationToken) : base(width, height, cancellationToken)
    {

    }

    protected override (int xOffset, int yOffset, XyOpts Opts) DetermineOffsets(
        GameTextureInfo textureInfo)
    {
        XyOpts xyOpts = XyOpts.None;
        int xOffset = textureInfo.XOffset;
        int yOffset = textureInfo.YOffset;

        if (textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FlipX))
        {
            xyOpts |= XyOpts.FlipX;
        }

        if (textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FlipY))
        {
            xyOpts |= XyOpts.FlipY;
        }

        if (textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.SwapXY))
        {
            xyOpts |= XyOpts.SwapXY;
        }

        return (xOffset, yOffset, xyOpts);
    }

    protected override ushort* CalculateTransparentWall(
        ReadOnlySpan<RenderableSector> sectors,
        RenderWindowWallSnapshot renderableWall)
    {
        int width = PixelWidth;

        int bufferOffset = PixelWidth * (renderableWall.Depth + 1);

        Span<float> distanceSpan = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
        Span<RenderColumnStatus> columnStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[bufferOffset..];

        Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
        Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

        // RenderOutline(wallStart, wallEnd, BGRA.Red, BGRA.Green);//, renderableWall.XLeft, renderableWall.XRight);

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
        int textureHeightShifted = (int)texture.Height << 16;

        int xOffset = SharedHelpers.EnsureOffsetIsPositive(textureInfo.Width, textureInfo.XOffset);
        int yOffset = textureInfo.YOffset > sectorHeight ? textureInfo.YOffset - 65536 : textureInfo.YOffset;

        int* textureXLocationPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureXLocation);
        uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
        uint* textureYIncrementPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);

        int* portalFromClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
        int* portalToClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);
        ushort* repeatedCountPtr = this.memoryPool.GetBucketPtr<ushort>(MemoryPoolBucket.Temp);
        repeatedCountPtr += wallFromX;

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
            textureYLocationPtr[x] = (uint)SharedHelpers.EnsureOffsetIsPositive(textureHeightShifted, float.ConvertToIntegerNative<int>(textureYPos));
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
        int textureHeight,
        MemoryPoolBucket repeatedCountBucket)
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
                CoreRendererForPowTextures<DrawSimplePixel>.RenderSkybox(player, 4, repeatedCountPtr, angleCachePtr, screenPtr, texturePtr, sectorFromX, sectorToX, fromYPtr, toYPtr, ceilingStartPtr, floorEndPtr, width, textureWidth, textureHeight, yTextureIncr);
            }
            else
            {
                CoreRendererForOddTextures<DrawSimplePixel>.RenderSkybox(player, 4, repeatedCountPtr, angleCachePtr, screenPtr, texturePtr, sectorFromX, sectorToX, fromYPtr, toYPtr, ceilingStartPtr, floorEndPtr, width, textureWidth, textureHeight, yTextureIncr);
            }
        }

        return;

        Span<ushort> CaclulateRepeatedCount()
        {
            RenderColumnStatus* status = memoryPool.GetBucketPtr<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            int length = sectorToX - sectorFromX;

            Span<ushort> repeatedCount = memoryPool.GetBucket<ushort>(repeatedCountBucket)[..(length + 1)];

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
