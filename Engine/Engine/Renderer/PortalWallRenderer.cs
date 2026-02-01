using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void CalculateDistance(RenderablePortalWall renderableWall)
        {
            int width = PixelWidth;
            var wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<float> distance = RenderWindowHelper.Distance;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                (int portalFromYClamped, int portalToYClamped) = RenderWindowHelper.GetClampedWallFromTo(x);
                distance[x] = MathFormulas.CalculateDistance2(cameraRay, t1, d2y, d2x);
                ceilingStart[x] = portalFromYClamped;
                floorEnd[x] = portalToYClamped;
                status[x] ^= RenderColumnStatus.CanRenderWall;
            }
        }

        private bool DrawPortalWall(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            (int sectorHeight, int ceilOffset, int floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);
            bool renderLower = floorOffset != 0;
            bool renderUpper = ceilOffset != 0;

            // ceiling and floor of the sector are the same
            // so no wall is drawn
            if (!renderLower && !renderUpper)
            {
                CalculateDistance(renderableWall);
                return true;
            }

            RenderableWall wall = renderableWall.Wall;

            if (renderUpper)
            {
                TextureInfo upperTexture = wall.UpperTexture!;
                PrecalculateUpperWallDistance(sector, renderableWall);

                if (upperTexture.RenderingOptions.IsSkybox)
                {
                    DrawUpperSkyboxPortalWall(player, sector, sectors, renderableWall);
                }
                else
                {
                    DrawUpperPortalWall(sector, sectors, renderableWall);
                }
            }

            if (renderLower)
            {
                TextureInfo lowerTexture = wall.LowerTexture!;
                PrecalculateLowerWallDistance(sector, renderableWall);

                if (lowerTexture.RenderingOptions.IsSkybox)
                {
                    DrawLowerSkyboxPortalWall(player, sector, sectors, renderableWall);
                }
                else
                {
                    DrawLowerPortalWall(sector, sectors, renderableWall);
                }
            }

            float oneOverSectorHeight = 1f / sectorHeight;

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = RenderWindowHelper.Status[x];

                if (!columnStatus.WallRenderable)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                int floorPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * floorOffset);
                int ceilPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * ceilOffset);
                int portalFromY = wallStartY - ceilPixelOffset;
                int portalToY = wallEndY - floorPixelOffset;
                int portalFromYClamped = Math.Clamp(portalFromY, ceilingStartY, floorEndY);
                int portalToYClamped = Math.Clamp(portalToY, ceilingStartY, floorEndY);

                ceilingStart[x] = portalFromYClamped;
                floorEnd[x] = portalToYClamped;
                status[x] ^= RenderColumnStatus.CanRenderWall;
            }

            // if sector height matches top or bottom offset only top or bottom texture was drawn
            // no middle texture is possible, thus we can treat this as basic wall
            return !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);
        }


        private void DrawUpperSkyboxPortalWall(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            (int sectorHeight, int ceilOffset, _) = CalculatePortalOffsets(sectors, renderableWall.Wall);

            float oneOverSectorHeight = 1f / sectorHeight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<int> clampedFrom = RenderWindowHelper.ClampedFrom;

            byte lightLevel = sector.LightLevel;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;

            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetReference(upperTexture.Texture.GetBinary(false, lightLevel));
            ref uint upperTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref upperTexturePtr);
            ref float angleCachePtr = ref memoryPool.GetBucketRef<float>(MemoryPoolBucket.AngleCache);

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];
                int fromYClamped = clampedFrom[x];

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;


                // Portal Calculation
                int ceilPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * ceilOffset);
                int portalFromY = wallStartY - ceilPixelOffset;
                int portalFromYClamped = Math.Clamp(portalFromY, ceilingStartY, floorEndY);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, fromYClamped * width + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, portalFromYClamped * width + x);

                RenderSkyboxLine(player,
                    x,
                    upperTexture,
                    ref upperTextureUintPtr,
                    ref angleCachePtr,
                    ref screenIndexPtr,
                    ref screenIndexPtrEnd);
            }
        }

        private void DrawLowerSkyboxPortalWall(
                PortalPlayerSnapshot player,
                Sector sector,
                ReadOnlySpan<Sector> sectors,
                RenderablePortalWall renderableWall)
        {
            (int sectorHeight, _, int floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);

            float oneOverSectorHeight = 1f / sectorHeight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<int> clampedTo = RenderWindowHelper.ClampedTo;

            byte lightLevel = sector.LightLevel;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;

            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetReference(upperTexture.Texture.GetBinary(false, lightLevel));
            ref uint upperTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref upperTexturePtr);
            ref float angleCachePtr = ref memoryPool.GetBucketRef<float>(MemoryPoolBucket.AngleCache);

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];
                int toYClamped = clampedTo[x];

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                int floorPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * floorOffset);
                int portalToY = wallEndY - floorPixelOffset;
                int portalToYClamped = Math.Clamp(portalToY, ceilingStartY, floorEndY);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToYClamped * width + x);
                ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, toYClamped * width + x);

                RenderSkyboxLine(player,
                    x,
                    upperTexture,
                    ref upperTextureUintPtr,
                    ref angleCachePtr,
                    ref screenIndexPtr,
                    in screenIndexPtrEnd);
            }
        }

        private void DrawUpperPortalWall(
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            (int sectorHeight, int ceilOffset, int floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);

            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;

            (_, _, bool upperFlipY) = GetFlags(upperTexture);

            float oneOverSectorHeight = 1f / sectorHeight;
            byte lightLevel = sector.LightLevel;

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            ReadOnlySpan<int> topTextureYLocation = RenderWindowHelper.TopTextureYLocation;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<int> topTextureXLocation = RenderWindowHelper.TopTextureXLocation;

            Span<int> clampedTo = RenderWindowHelper.ClampedTo;
            Span<int> clampedFrom = RenderWindowHelper.ClampedFrom;

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint upperTexturePtr = ref upperTexture.Texture.GetBinaryRef<uint>(true, lightLevel);

            ref uint screenPtr = ref GetScreenPtr<uint>();

            using TempBuffer<uint> upperBuffer = TempBuffer<uint>.GetBuffer(upperTexture.Height);

            int upperTextureStart = upperTexture.YOffset << 16;
            int textureWidth = upperTexture.Height;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = RenderWindowHelper.Status[x];

                if (!columnStatus.WallRenderable)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Wall Calculation
                int fromYClamped = clampedFrom[x];
                int toYClamped = clampedTo[x];

                // Portal Calculation
                int floorPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * floorOffset);
                int ceilPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * ceilOffset);
                int portalFromY = wallStartY - ceilPixelOffset;
                int portalToY = wallEndY - floorPixelOffset;
                int portalFromYClamped = Math.Clamp(portalFromY, ceilingStartY, floorEndY);
                int portalToYClamped = Math.Clamp(portalToY, ceilingStartY, floorEndY);

                int textureYPos = topTextureXLocation[x];
                int textureXIncr = topTextureYLocation[x];

                // Calculate Upper  Texture Position
                int textureXPos = upperTextureStart - textureXIncr * (wallStartY - fromYClamped);

                textureXPos = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                CalculateAndCacheWallColumn(upperBuffer, ref upperTexturePtr, textureYPos, upperFlipY);

                RenderWallLine(
                    width,
                    x,
                    textureWidth,
                    fromYClamped,
                    portalFromYClamped,
                    (uint)textureXPos,
                    (uint)textureXIncr,
                    ref screenPtr,
                    ref upperBuffer.Pointer
                );
            }
        }

        private void DrawLowerPortalWall(
                Sector sector,
                ReadOnlySpan<Sector> sectors,
                RenderablePortalWall renderableWall)
        {
            (int sectorHeight, int ceilOffset, int floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);

            RenderableWall wall = renderableWall.Wall;
            TextureInfo lowerTexture = wall.LowerTexture!;

            (_, _, bool lowerFlipY) = GetFlags(lowerTexture);

            float oneOverSectorHeight = 1f / sectorHeight;
            byte lightLevel = sector.LightLevel;

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            ReadOnlySpan<int> bottomTextureYLocation = RenderWindowHelper.BottomTextureYLocation;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<int> bottomTextureXLocation = RenderWindowHelper.BottomTextureXLocation;

            Span<int> clampedFrom = RenderWindowHelper.ClampedFrom;
            Span<int> clampedTo = RenderWindowHelper.ClampedTo;

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint lowerTexturePtr = ref lowerTexture.Texture.GetBinaryRef<uint>(true, lightLevel);

            ref uint screenPtr = ref GetScreenPtr<uint>();

            using TempBuffer<uint> lowerBuffer = TempBuffer<uint>.GetBuffer(lowerTexture.Height);

            int lowerTextureStart = lowerTexture.YOffset << 16;
            int textureWidth = lowerTexture.Height;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = RenderWindowHelper.Status[x];

                if (!columnStatus.WallRenderable)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];
                int fromYClamped = clampedFrom[x];
                int toYClamped = clampedTo[x];

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                int floorPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * floorOffset);
                int ceilPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * ceilOffset);
                int portalFromY = wallStartY - ceilPixelOffset;
                int portalToY = wallEndY - floorPixelOffset;
                int portalFromYClamped = Math.Clamp(portalFromY, ceilingStartY, floorEndY);
                int portalToYClamped = Math.Clamp(portalToY, ceilingStartY, floorEndY);

                int textureYPos = bottomTextureXLocation[x];
                int textureXIncr = bottomTextureYLocation[x];


                int textureXPos = textureXIncr * (portalToYClamped - portalToY) + lowerTextureStart;

                textureXPos = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                CalculateAndCacheWallColumn(lowerBuffer, ref lowerTexturePtr, textureYPos, lowerFlipY);

                RenderWallLine(
                    width,
                    x,
                    textureWidth,
                    portalToYClamped,
                    toYClamped,
                    (uint)textureXPos,
                    (uint)textureXIncr,
                    ref screenPtr,
                    ref lowerBuffer.Pointer
                );
            }
        }

        private void PrecalculateUpperWallDistance(
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;

            PrecalculateWallDistanceShared(renderableWall, sector, wall.UpperTexture!, RenderWindowHelper.TopTextureXLocation, RenderWindowHelper.TopTextureYLocation);
        }

        private void PrecalculateLowerWallDistance(
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;

            PrecalculateWallDistanceShared(renderableWall, sector, wall.LowerTexture!, RenderWindowHelper.BottomTextureXLocation, RenderWindowHelper.BottomTextureYLocation);
        }

        private void PrecalculateWallDistanceShared(
            RenderablePortalWall renderableWall, Sector sector, TextureInfo textureInfo,
            scoped Span<int> xLocation, scoped Span<int> yLocation)
        {
            Span<float> distance = RenderWindowHelper.Distance;
            Span<int> wallStart = RenderWindowHelper.WallStart;
            Span<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<RenderColumnStatus> status = RenderWindowHelper.Status;

            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<int> clampedFrom = RenderWindowHelper.ClampedFrom;
            Span<int> clampedTo = RenderWindowHelper.ClampedTo;

            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;
            bool flipX = textureInfo.RenderingOptions.IsFlippedX;
            flipX = wall.Flipped ? !flipX : flipX;
            float xOffset = textureInfo.XOffset;

            (int textureWidth, int textureHeight, float xScale, float scaledTextureWidth) = CalculateScale(sector, wall, textureInfo);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            float rX = flipX ? wall.R2.X : wall.R1.X;
            float rY = flipX ? wall.R2.Y : wall.R1.Y;

            int length = (wallToX - wallFromX);

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                const int canRenderWallMask = (int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall);

                Vector<int> canRenderWallMaskV = Vector.Create(canRenderWallMask);

                Span<int> statusInt = MemoryMarshal.Cast<RenderColumnStatus, int>(status);

                Vector<float> t1V = Vector.Create(t1);
                Vector<float> d2yV = Vector.Create(d2y);
                Vector<float> d2xV = Vector.Create(d2x);
                Vector<float> rXV = Vector.Create(rX);
                Vector<float> rYV = Vector.Create(rY);

                Vector<float> xScaleV = Vector.Create(xScale);
                Vector<float> xOffsetV = Vector.Create(xOffset);
                Vector<float> scaledTextureWidthV = Vector.Create(scaledTextureWidth);
                Vector<int> textureWidthV = Vector.Create(textureWidth);


                Vector<float> cameraRayV = Vector.CreateSequence(cameraRay, cameraWidthIncr);
                Vector<float> cameraWidthIncrV = Vector.Create(cameraWidthIncr * Vector<float>.Count);

                int rem = (wallToX - wallFromX) % Vector<float>.Count;
                wallToX -= rem;

                bool textureHeightEven = SharedHelpers.IsPowerOfTwo(textureHeight);
                Vector<int> heightMask = textureHeightEven ? Vector.Create(textureHeight - 1) : default;

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> columnStatusV = Vector.LoadUnsafe(ref statusInt[x]);

                    if ((columnStatusV & canRenderWallMaskV) == Vector<int>.Zero)
                    {
                        continue;
                    }

                    (Vector<float> fromToXdist, Vector<float> fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRayV, t1V, d2yV, d2xV);
                    Vector<float> distX = rXV - fromToXdist;
                    Vector<float> distY = rYV - fromToYdist;

                    Vector<int> wallStartV = Vector.LoadUnsafe(ref wallStart[x]);
                    Vector<int> wallEndV = Vector.LoadUnsafe(ref wallEnd[x]);

                    Vector<float> textureDist = Vector.SquareRoot(distX * distX + distY * distY);
                    Vector<int> topXLocationV = Vector.ConvertToInt32Native(Vector.FusedMultiplyAdd(textureDist, xScaleV, xOffsetV));
                    Vector<int> topYLocationV = Vector.ConvertToInt32Native(scaledTextureWidthV / Vector.ConvertToSingle(wallEndV - wallStartV));

                    Vector.StoreUnsafe(fromToYdist, ref distance[x]);
                    Vector.StoreUnsafe(topYLocationV, ref yLocation[x]);

                    if (textureHeightEven)
                    {
                        topXLocationV = (topXLocationV & heightMask) * textureWidthV;
                        Vector.StoreUnsafe(topXLocationV, ref xLocation[x]);
                    }
                    else
                    {
                        for (int i = 0; i < Vector<float>.Count; i++)
                        {
                            xLocation[x + i] = (topXLocationV[i] % textureHeight) * textureWidth;
                        }
                    }

                    Vector<int> ceilingStartYV = Vector.LoadUnsafe(ref ceilingStart[x]);
                    Vector<int> floorEndYV = Vector.LoadUnsafe(ref floorEnd[x]);

                    Vector<int> clamptedFromYV = Vector.Clamp(wallStartV, ceilingStartYV, floorEndYV);
                    Vector<int> clamptedToYV = Vector.Clamp(wallEndV, ceilingStartYV, floorEndYV);

                    Vector.StoreUnsafe(clamptedFromYV, ref clampedFrom[x]);
                    Vector.StoreUnsafe(clamptedToYV, ref clampedTo[x]);

                    cameraRayV += cameraWidthIncrV;
                }

                wallFromX = wallToX;
                wallToX += rem;
                cameraRay = cameraRayV[0];
            }

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                (float fromToXdist, float fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];

                float distX = rX - fromToXdist;
                float distY = rY - fromToYdist;
                float textureDist = MathF.Sqrt(distX * distX + distY * distY);

                distance[x] = fromToYdist;
                xLocation[x] = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, xScale, xOffset));
                yLocation[x] = float.ConvertToIntegerNative<int>(scaledTextureWidth / (wallEndY - wallStartY));
                xLocation[x] = (xLocation[x] % textureHeight) * textureWidth;

                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);
                // int textureXPosY = textureStart - textureXIncr * (wallStartY - clamptedFromY);
                // textureXPosY = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPosY);

                clampedFrom[x] = clamptedFromY;
                clampedTo[x] = clamptedToY;
                // textureXPos[x] = textureXPosY;
            }
        }

    }
}
