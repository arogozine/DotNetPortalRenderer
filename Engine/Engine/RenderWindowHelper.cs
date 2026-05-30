using RenderingEngine.Tooling;
using SoftwareRendererModels;

namespace RenderingEngine.Engine
{
    internal sealed class RenderWindowHelper
    {
        private readonly int width;
        private readonly int height;

        public Span<RenderColumnStatus> Status => alignedMemoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
        public Span<float> Distance => alignedMemoryPool.GetBucket<float>(MemoryPoolBucket.Distance);

        private int sectorFromX;
        private int sectorToX;

        private RenderableWall? wall;
        private int wallFromX;
        private int wallToX;
        private readonly AlignedMemoryPool alignedMemoryPool;

        public int SectorFrom => sectorFromX;
        public int SectorTo => sectorToX;

        public RenderWindowHelper(int width, int height, AlignedMemoryPool alignedMemoryPool)
        {
            this.width = width;
            this.height = height;

            sectorFromX = 0;
            sectorToX = width;
            this.alignedMemoryPool = alignedMemoryPool;
        }

        public void NewRender()
        {
            Span<RenderColumnStatus> status = alignedMemoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> floorEnd = alignedMemoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            Span<int> wallEndSloped = alignedMemoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            Span<float> distance = alignedMemoryPool.GetBucket<float>(MemoryPoolBucket.Distance);

            status.Fill(RenderColumnStatus.NewRender);
            floorEnd.Fill(height - 1);
            wallEndSloped.Fill(height - 1);
            distance.Fill(float.MaxValue);

            alignedMemoryPool.ClearBuckets(
                MemoryPoolBucket.PortalFrom, MemoryPoolBucket.PortalFromClamped,
                MemoryPoolBucket.PortalTo, MemoryPoolBucket.PortalToClamped,
                MemoryPoolBucket.CeilingStart,
                MemoryPoolBucket.TextureYIncrement, MemoryPoolBucket.StartingYTexturePosition);
        }


        public void NewSector(NeighborsToRender sectorInfo)
        {
            if (sectorInfo.RenderableWall is RenderablePortalWall renderableWall)
            {
                (sectorFromX, sectorToX) = (renderableWall.XLeft, Math.Min(renderableWall.XRight, width - 1));
            }
            else
            {
                (sectorFromX, sectorToX) = (0, width - 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int SectroFromX, int SectorToX) GetSectorX()
        {
            return (sectorFromX, sectorToX);
        }

        [MemberNotNull(nameof(wall))]
        public bool SetWallToCalculate(RenderableWall wall)
        {
            Span<RenderColumnStatus> status = this.Status;

            this.wall = wall;
            this.wallFromX = wall.XLeft;
            this.wallToX = wall.XRight;

            // clamp to sector window
            wallFromX = Math.Max(sectorFromX, wall.XLeft);
            wallToX = Math.Min(sectorToX, wall.XRight);

            int i, j;

            for (i = wallFromX; i <= wallToX; i++)
            {
                RenderColumnStatus columnStatus = status[i];

                if (!columnStatus.IsFinished && !columnStatus.IsCalculated)
                {
                    break;
                }
            }

            for (j = wallToX; j >= wallFromX; j--)
            {
                RenderColumnStatus columnStatus = status[j];

                if (!columnStatus.IsFinished && !columnStatus.IsCalculated)
                {
                    break;
                }
            }

            (wallFromX, wallToX) = (i, j);

            // wall has been rendered over for this sector
            return wallFromX < wallToX;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int offset, int WallFromX, int WallToX) GetWallRenderWindowX()
        {
            ArgumentNullException.ThrowIfNull(wall);

            int wallFromXOffset = wallFromX > wall.XLeft ? wallFromX - wall.XLeft : 0;

            return (wallFromXOffset, wallFromX, wallToX);
        }
    }
}
