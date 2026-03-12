using RenderingEngine.Models;
using RenderingEngine.Tooling;

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

            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> ceilingStartSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            Span<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);
            Span<int> wallStartSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            Span<int> wallEndSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);
            Span<int> floorEndSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                int ceilingStart = ceilingStartSpan[x];
                int wallStartY = wallStartSpan[x];
                int wallEndY = wallEndSpan[x];
                int floorEndY = floorEndSpan[x];

                int portalFromYClamped = Math.Clamp(wallStartY, ceilingStart, floorEndY);
                int portalToYClamped = Math.Clamp(wallEndY, ceilingStart, floorEndY);

                distance[x] = MathFormulas.CalculateDistance2(cameraRay, t1, d2y, d2x);
                ceilingStartSpan[x] = portalFromYClamped;
                floorEnd[x] = portalToYClamped;
                status[x] ^= RenderColumnStatus.CanRenderWall;
            }
        }

        private bool DrawPortalWall(
            PortalPlayerSnapshot player,
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

            CalculateWallClamp(renderableWall);
            CalculatePortalClamp(renderableWall);

            if (renderUpper)
            {
                TextureInfo upperTexture = wall.UpperTexture!;


                CalculateRenderWindow2(true, renderableWall, upperTexture);


                PrecalculateUpperWallDistance(renderableWall);

                if (upperTexture.RenderingOptions.IsSkybox)
                {
                    DrawUpperSkyboxPortalWall(player, sectors, renderableWall);
                }
                else
                {
                    DrawUpperPortalWall(sectors, renderableWall);
                }
            }

            if (renderLower)
            {
                TextureInfo lowerTexture = wall.LowerTexture!;

                CalculateRenderWindow2(false, renderableWall, lowerTexture);


                PrecalculateLowerWallDistance(renderableWall);

                if (lowerTexture.RenderingOptions.IsSkybox)
                {
                    DrawLowerSkyboxPortalWall(player, sectors, renderableWall);
                }
                else
                {
                    DrawLowerPortalWall(sectors, renderableWall);
                }
            }

            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            Span<int> portalFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFrom);
            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);

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

                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];


                int portalFromY = portalFrom[x];
                int portalToY = portalTo[x]; 
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
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            (int sectorHeight, int ceilOffset, _) = CalculatePortalOffsets(sectors, renderableWall.Wall);

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
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetReference(upperTexture.Texture.GetBinary(false, wall.Shade));
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
            (int sectorHeight, _, int floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);

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
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetReference(upperTexture.Texture.GetBinary(false, wall.Shade));
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
            for (int x = wallFromX; x <= wallToX; x++)
            {
                ref RenderColumnStatus columnStatus = ref Unsafe.Add(ref statusRef, x);

                if (!columnStatus.WallRenderable)
                {
                    repeatedCount[x - wallFromX] = 0;
                }
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

            Span<uint> textureXLocation = memoryPool.GetBucket<uint>(MemoryPoolBucket.TopTextureXLocation);
            Span<uint> topTextureYIncrement = memoryPool.GetBucket<uint>(MemoryPoolBucket.TopTextureYIncrement);
            Span<uint> wallStartClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.WallStartClamped);
            Span<uint> wallEndClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.PortalFromClamped);

            Span<ushort> repeatedCount = DetermineMaxHorizontalRenderingDistance(renderableWall);

            DrawWallShared(renderableWall, upperTexture, repeatedCount, textureXLocation, topTextureYIncrement, wallStartClamped, wallEndClamped);
        }

        private void DrawLowerPortalWall(
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo lowerTexture = wall.LowerTexture!;

            Debug.Assert(lowerTexture != null);

            Span<uint> textureXLocation = memoryPool.GetBucket<uint>(MemoryPoolBucket.BottomTextureXLocation);
            Span<uint> topTextureYIncrement = memoryPool.GetBucket<uint>(MemoryPoolBucket.BottomTextureYIncrement);
            Span<uint> wallStartClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.PortalToClamped);
            Span<uint> wallEndClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.WallEndClamped);

            Span<ushort> repeatedCount = DetermineMaxHorizontalRenderingDistance(renderableWall);

            DrawWallShared(renderableWall, lowerTexture, repeatedCount, textureXLocation, topTextureYIncrement, wallStartClamped, wallEndClamped);
        }

        private void PrecalculateUpperWallDistance(RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;

            Span<int> xLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.TopTextureXLocation);

            PrecalculateWallDistanceShared(true, renderableWall, wall.UpperTexture!, xLocation);
        }

        private void PrecalculateLowerWallDistance(RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;

            Span<int> xLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.BottomTextureXLocation);

            PrecalculateWallDistanceShared(false, renderableWall, wall.LowerTexture!, xLocation);
        }

        private void PrecalculateWallDistanceShared(bool upper,
            RenderablePortalWall renderableWall, TextureInfo textureInfo,
            scoped Span<int> xLocation)
        {
            CalculateTextureDistanceAndXPosition(xLocation, renderableWall, textureInfo);
            CalculateTextureYStartAndIncrement(upper, renderableWall, textureInfo);
        }
    }
}
