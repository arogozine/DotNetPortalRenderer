using RenderingEngine.Models;
using RenderingEngine.Tooling;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ref T GetScreenPtr<T>()
            where T : unmanaged
        {
            return ref Unsafe.AsRef<T>(buffer);
        }

        private bool DrawBasicWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall)
        {
            // separate path for skybox rendering
            RenderableWall wall = renderableWall.Wall;
            TextureInfo textureInfo = wall.MiddleTexture!;

            Debug.Assert(textureInfo != null);

            if (textureInfo.RenderingOptions.IsSkybox)
            {
                CalculateDistance(renderableWall);
                return DrawBasicSkyboxWall(player, renderableWall);
            }

            // Precalculate render window and texture positions
            PrecalculateBasicWallDistance(renderableWall);

            ref RenderColumnStatus statusRef = ref memoryPool.GetBucketRef<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            ushort length = (ushort)(wallToX - wallFromX + 1);
            Span<ushort> repeatedCount = TempBuffer<ushort>.GetBuffer(length);
            repeatedCount.Fill(length);

            // Set the X position as "FinishedRendering" for this wall
            // and determines where there repeat count is 0 (nothing to draw)
            statusRef = ref Unsafe.Add(ref statusRef, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++)
            {
                if (!statusRef.WallRenderable)
                {
                    repeatedCount[x - wallFromX] = 0;
                }

                statusRef = RenderColumnStatus.FinishedRendering;
                statusRef = ref Unsafe.Add(ref statusRef, 1);
            }

            Span<uint> wallStartClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.WallStartClamped);
            Span<uint> wallEndClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.WallEndClamped);

            DrawWallShared(renderableWall, wall.MiddleTexture!, repeatedCount, wallStartClamped, wallEndClamped);

            return true;
        }

        #region Pre Calculate

        private void PrecalculateBasicWallDistance(RenderablePortalWall renderableWall)
        {
            Debug.Assert(renderableWall.Wall.MiddleTexture != null);

            CalculateUpperTextureYIncrement(renderableWall, renderableWall.Wall.MiddleTexture);
            CalculateWallClamp(renderableWall);
            CalculateTextureDistanceAndXPosition(renderableWall, renderableWall.Wall.MiddleTexture!);
        }

        private static (int Height, int Width, float XScale, float ScaledTextureHeight) CalculateScale(
            Sector sector,
            RenderableWall wall,
            TextureInfo wallTexture)
        {
            int textureHeight = wallTexture.Height;
            int textureWidth = wallTexture.Width;

            if (wallTexture.XScale is float xScale)
            {
                float wallLength = wall.Length;
                xScale = xScale / wallLength * textureWidth;
            }
            else
            {
                xScale = 1f;
            }

            float scaledTextureHeight;

            if (wallTexture.YScale is float yScale)
            {
                yScale = (sector.Ceil - sector.Floor) * yScale;
                scaledTextureHeight = (textureHeight << 16) * yScale;
            }
            else
            {
                scaledTextureHeight = (sector.Ceil - sector.Floor) << 16;
            }

            return (textureHeight, textureWidth, xScale, scaledTextureHeight);
        }

        #endregion

        #region Calculation Helpers

        private static (bool RenderLower, bool RenderUpper, bool IsBasicWall) CalculateCanRenderPortalWall(ReadOnlySpan<Sector> sectors, RenderableWall wall)
        {
            Sector sector = wall.Sector;
            Sector neighborSector = sectors[wall.Neighbor];
            bool wallSloped = sector.Settings.Sloped || neighborSector.Settings.Sloped;

            bool renderLower, renderUpper, basicWall;
            float sectorHeight, ceilOffset, floorOffset;

            if (wallSloped)
            {
                (float floorZ_a, float ceilingZ_a) = CalculateZAtPoint(sector, wall.C2);
                (float floorZ_b, float ceilingZ_b) = CalculateZAtPoint(sector, wall.C1);

                (float p_floorZ_a, float p_ceilingZ_a) = CalculateZAtPoint(neighborSector, wall.C1);
                (float p_floorZ_b, float p_ceilingZ_b) = CalculateZAtPoint(neighborSector, wall.C2);

                (sectorHeight, ceilOffset, floorOffset) = CalculatePortalOffsets(floorZ_a, ceilingZ_a, p_floorZ_a, p_ceilingZ_a);

                renderLower = floorOffset != 0;
                renderUpper = ceilOffset != 0;
                basicWall = !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);

                (sectorHeight, ceilOffset, floorOffset) = CalculatePortalOffsets(floorZ_b, ceilingZ_b, p_floorZ_b, p_ceilingZ_b);

                renderLower |= floorOffset != 0;
                renderUpper |= ceilOffset != 0;
                basicWall &= !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);

                return (renderLower, renderUpper, basicWall);
            }
            else
            {
                (sectorHeight, ceilOffset, floorOffset) = CalculatePortalOffsets(sectors, wall);

                renderLower = floorOffset != 0;
                renderUpper = ceilOffset != 0;
                basicWall = !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);

                return (renderLower, renderUpper, basicWall);
            }
        }

        private static (float SectorHeight, float CeilingOffset, float FloorOffset) CalculatePortalOffsets(ReadOnlySpan<Sector> sectors, RenderableWall wall)
        {
            Sector sector = wall.Sector;
            Sector neighborSector = sectors[wall.Neighbor];

            return CalculatePortalOffsets(sector.Floor, sector.Ceil, neighborSector.Floor, neighborSector.Ceil);
        }

        private static (float SectorHeight, float CeilingOffset, float FloorOffset) CalculatePortalOffsets(float floorA, float ceilA, float floorB, float ceilB)
        {
            float sectorHeight = ceilA - floorA;
            float floorOffset = floorB - floorA;
            float ceilOffset = ceilB - ceilA;

            if (floorOffset < 0f)
            {
                floorOffset = 0f;
            }

            if (ceilOffset > 0f)
            {
                ceilOffset = 0f;
            }

            // don't draw beyond the bounds
            if (ceilOffset < -sectorHeight)
            {
                ceilOffset = -sectorHeight;
            }

            if (floorOffset > sectorHeight)
            {
                floorOffset = sectorHeight;
            }

            return (sectorHeight, ceilOffset, floorOffset);
        }

        #endregion
    }
}