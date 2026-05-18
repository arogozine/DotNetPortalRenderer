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
                    DrawLowerSkyboxPortalWall(player, renderableWall);
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
                    DrawUpperSkyboxPortalWall(player, renderableWall);
                }
                else
                {
                    CalculateUpperTextureYIncrement(renderableWall, upperTexture);
                    CalculateTextureDistanceAndXPosition(renderableWall, upperTexture);
                    DrawUpperPortalWall(renderableWall);
                }
            }


            // if sector height matches top or bottom offset only top or bottom texture was drawn
            // no middle texture is possible, thus we can treat this as basic wall
            return basicWall;
        }


        private void DrawUpperSkyboxPortalWall(
            PortalPlayerSnapshot player,
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

        private unsafe void DrawUpperPortalWall(
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;

            Debug.Assert(upperTexture != null);

            uint* wallStartClamped = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.WallStartClamped);
            uint* wallEndClamped = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalFromClamped);

            Span<ushort> repeatedCount = DetermineMaxHorizontalRenderingDistance(renderableWall);

            DrawWallShared(renderableWall, upperTexture, repeatedCount, wallStartClamped, wallEndClamped);
        }

        private unsafe void DrawLowerPortalWall(
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo lowerTexture = wall.LowerTexture!;

            Debug.Assert(lowerTexture != null);

            uint* wallStartClamped = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalToClamped);
            uint* wallEndClamped = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.WallEndClamped);

            Span<ushort> repeatedCount = DetermineMaxHorizontalRenderingDistance(renderableWall);

            DrawWallShared(renderableWall, lowerTexture, repeatedCount, wallStartClamped, wallEndClamped);
        }
    }
}
