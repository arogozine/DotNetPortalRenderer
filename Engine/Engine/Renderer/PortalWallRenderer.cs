using RenderingEngine.Models;
using RenderingEngine.Tooling;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private bool DrawPortalWall(
            PortalPlayerSnapshot player,
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            (bool renderLower, bool renderUpper, bool basicWall) = CalculateCanRenderPortalWall(sectors, renderableWall.Wall);

            // ceiling and floor of the sector are the same
            // so no wall is drawn
            if (!renderLower && !renderUpper)
            {
                CalculateDistance(renderableWall);
                CalculateWallClamp(renderableWall);
                CalculatePortalClamp(renderableWall);

                return true;
            }

            RenderableWall wall = renderableWall.Wall;

            if (renderUpper)
            {
                TextureInfo upperTexture = wall.UpperTexture!;

                if (upperTexture.RenderingOptions.IsSkybox)
                {
                    CalculateDistance(renderableWall);
                    CalculateWallClamp(renderableWall);
                    CalculatePortalClamp(renderableWall);
                    DrawUpperSkyboxPortalWall(player, sectors, renderableWall);
                }
                else
                {
                    CalculateUpperTextureYIncrement(renderableWall, upperTexture);
                    CalculateWallClamp(renderableWall);

                    PrecalculateUpperWallDistance(renderableWall);
                    DrawUpperPortalWall(sectors, renderableWall);
                }
            }

            if (renderLower)
            {
                TextureInfo lowerTexture = wall.LowerTexture!;

                if (lowerTexture.RenderingOptions.IsSkybox)
                {
                    CalculateDistance(renderableWall);
                    CalculateWallClamp(renderableWall);
                    CalculatePortalClamp(renderableWall);
                    DrawLowerSkyboxPortalWall(player, sectors, renderableWall);
                }
                else
                {
                    CalculateLowerTextureYIncrement(renderableWall, lowerTexture);
                    CalculateWallClamp(renderableWall);

                    PrecalculateLowerWallDistance(renderableWall);
                    DrawLowerPortalWall(sectors, renderableWall);
                }
            }

            // if sector height matches top or bottom offset only top or bottom texture was drawn
            // no middle texture is possible, thus we can treat this as basic wall
            return basicWall;
        }


        private void DrawUpperSkyboxPortalWall(
            PortalPlayerSnapshot player,
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            (float sectorHeight, float ceilOffset, _) = CalculatePortalOffsets(sectors, renderableWall.Wall);

            float oneOverSectorHeight = 1f / sectorHeight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ReadOnlySpan<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            ReadOnlySpan<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            ReadOnlySpan<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            ReadOnlySpan<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);
            ReadOnlySpan<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            ReadOnlySpan<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;

            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetReference(upperTexture.Texture.GetBinary(wall.Shade, TextureTransform.Normal));
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
                int fromYClamped = wallStartClamped[x];

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
                ReadOnlySpan<Sector> sectors,
                RenderablePortalWall renderableWall)
        {
            (float sectorHeight, _, float floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);

            float oneOverSectorHeight = 1f / sectorHeight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ReadOnlySpan<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            ReadOnlySpan<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            ReadOnlySpan<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            ReadOnlySpan<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);
            ReadOnlySpan<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            ReadOnlySpan<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;

            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetReference(upperTexture.Texture.GetBinary(wall.Shade, TextureTransform.Normal));
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
                int toYClamped = wallEndClamped[x];

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

        private Span<ushort> DetermineMaxHorizontalRenderingDistance(RenderablePortalWall renderableWall)
        {
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            ushort length = (ushort)(wallToX - wallFromX + 1);

            Span<ushort> repeatedCount = TempBuffer<ushort>.GetBuffer(length);
            repeatedCount.Fill(length);

            // set repeat count to 0 where there is nothing to draw
            ref RenderColumnStatus statusRef = ref memoryPool.GetBucketRef<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            statusRef = ref Unsafe.Add(ref statusRef, wallFromX);

            ushort count = 1;
            for (int x = wallFromX; x <= wallToX; x++, count++)
            {
                if (!statusRef.WallRenderable)
                {
                    repeatedCount[x - wallFromX] = 0;
                    count = 1;
                }
                else
                {
                    repeatedCount[x - wallFromX] = count;
                }

                statusRef = ref Unsafe.Add(ref statusRef, 1);
            }

            return repeatedCount;
        }

        private void DrawUpperPortalWall(
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;

            Debug.Assert(upperTexture != null);

            Span<uint> wallStartClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.WallStartClamped);
            Span<uint> wallEndClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.PortalFromClamped);

            Span<ushort> repeatedCount = DetermineMaxHorizontalRenderingDistance(renderableWall);

            DrawWallShared(renderableWall, upperTexture, repeatedCount, wallStartClamped, wallEndClamped);
        }

        private void DrawLowerPortalWall(
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo lowerTexture = wall.LowerTexture!;

            Debug.Assert(lowerTexture != null);

            Span<uint> wallStartClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.PortalToClamped);
            Span<uint> wallEndClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.WallEndClamped);

            Span<ushort> repeatedCount = DetermineMaxHorizontalRenderingDistance(renderableWall);

            DrawWallShared(renderableWall, lowerTexture, repeatedCount, wallStartClamped, wallEndClamped);
        }

        private void PrecalculateUpperWallDistance(RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;

            PrecalculateWallDistanceShared(renderableWall, wall.UpperTexture!);
        }

        private void PrecalculateLowerWallDistance(RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            PrecalculateWallDistanceShared(renderableWall, wall.LowerTexture!);
        }

        private void PrecalculateWallDistanceShared(RenderablePortalWall renderableWall, TextureInfo textureInfo)
        {
            CalculateTextureDistanceAndXPosition(renderableWall, textureInfo);
            CalculatePortalClamp(renderableWall);
        }
    }
}
