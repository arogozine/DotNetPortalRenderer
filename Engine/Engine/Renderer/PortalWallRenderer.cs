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

            CalculateWallClamp(renderableWall);
            CalculatePortalClamp(renderableWall);

            // ceiling and floor of the sector are the same
            // so no wall is drawn
            if (!renderLower && !renderUpper)
            {
                CalculateDistance(renderableWall);
                return true;
            }

            RenderableWall wall = renderableWall.Wall;

            if (renderLower)
            {
                TextureInfo lowerTexture = wall.LowerTexture!;

                if (lowerTexture.RenderingOptions.IsSkybox)
                {
                    CalculateDistance(renderableWall);
                    DrawLowerSkyboxPortalWall(player, sectors, renderableWall);
                }
                else
                {
                    CalculateLowerTextureYIncrement(renderableWall, lowerTexture);
                    CalculateTextureDistanceAndXPosition(renderableWall, lowerTexture);
                    DrawLowerPortalWall(renderableWall);
                }
            }

            if (renderUpper)
            {
                TextureInfo upperTexture = wall.UpperTexture!;

                if (upperTexture.RenderingOptions.IsSkybox)
                {
                    CalculateDistance(renderableWall);
                    DrawUpperSkyboxPortalWall(player, sectors, renderableWall);
                }
                else
                {
                    CalculateUpperTextureYIncrement(renderableWall, upperTexture);
                    CalculateTextureDistanceAndXPosition(renderableWall, upperTexture);
                    DrawUpperPortalWall(sectors, renderableWall);
                }
            }


            // if sector height matches top or bottom offset only top or bottom texture was drawn
            // no middle texture is possible, thus we can treat this as basic wall
            return basicWall;
        }

        private void DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall,
            Span<int> wallStartSpan, Span<int> wallEndSpan,
            TextureInfo wallTexture)
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> ceilingStartSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> floorEndSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ref uint wallTextureUintPtr = ref wallTexture.Texture.GetBinaryRef<uint>(0, TextureTransform.Normal);
            ref float angleCachePtr = ref memoryPool.GetBucketRef<float>(MemoryPoolBucket.AngleCache);

            for (int x = wallFromX; x <= wallToX; x++)
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

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStart, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStart, floorEndY);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, clamptedToY * width + x);

                RenderSkyboxLine(player,
                    wallStartSpan,
                    x,
                    wallTexture,
                    ref wallTextureUintPtr,
                    ref angleCachePtr,
                    ref screenIndexPtr,
                    ref screenIndexPtrEnd);
            }
        }

        private void DrawUpperSkyboxPortalWall(
            PortalPlayerSnapshot player,
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFromClamped);


            RenderableWall wall = renderableWall.Wall;

            Debug.Assert(wall.UpperTexture is not null);
            DrawBasicSkyboxWall(player, renderableWall, wallStartClamped, wallEndClamped, wall.UpperTexture);
        }

        private void DrawLowerSkyboxPortalWall(
                PortalPlayerSnapshot player,
                ReadOnlySpan<Sector> sectors,
                RenderablePortalWall renderableWall)
        {
            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            RenderableWall wall = renderableWall.Wall;

            Debug.Assert(wall.LowerTexture is not null);
            DrawBasicSkyboxWall(player, renderableWall, wallStartClamped, wallEndClamped, wall.LowerTexture);
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

            for (int x = wallFromX; x <= wallToX; x++)
            {
                if (!statusRef.WallRenderable)
                {
                    repeatedCount[x - wallFromX] = 0;
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
    }
}
