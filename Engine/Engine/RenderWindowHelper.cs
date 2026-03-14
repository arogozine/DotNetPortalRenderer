using RenderingEngine.Models;
using RenderingEngine.Tooling;

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
            Span<int> wallEnd = alignedMemoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            Span<float> distance = alignedMemoryPool.GetBucket<float>(MemoryPoolBucket.Distance);

            status.Fill(RenderColumnStatus.NewRender);
            floorEnd.Fill(height - 1);
            wallEnd.Fill(height - 1);
            distance.Fill(float.MaxValue);

            alignedMemoryPool.ClearBuckets(
                MemoryPoolBucket.CeilingStart, MemoryPoolBucket.WallStart,
                MemoryPoolBucket.TextureYIncrement, MemoryPoolBucket.StartingYTexturePosition);
        }

        public RenderColumnStatus NewDepth()
        {
            Span<RenderColumnStatus> status = alignedMemoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            ReadOnlySpan<int> ceilingStart = alignedMemoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            ReadOnlySpan<int> wallStart = alignedMemoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            ReadOnlySpan<int> wallEnd = alignedMemoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            ReadOnlySpan<int> floorEnd = alignedMemoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);


            RenderColumnStatus renderColumnStatus = default;

            for (int i = 0; i < this.width; i++)
            {
                RenderColumnStatus columnStatus = status[i];

                if (columnStatus.IsFinished)
                {
                    continue;
                }
                else if (columnStatus.IsCalculated)
                {
                    columnStatus = RecalculateRenderWindow(i, false, status, ceilingStart, floorEnd, wallStart, wallEnd);
                }
                else
                {
                    columnStatus = RenderColumnStatus.FinishedRendering;
                    status[i] = RenderColumnStatus.FinishedRendering;
                }

                renderColumnStatus |= columnStatus;
            }

            // this allows us to know what, if anything, we can still render
            return renderColumnStatus;
        }

        public static RenderColumnStatus RecalculateRenderWindow(
            int x,
            bool calculated,
            Span<RenderColumnStatus> Status,
            ReadOnlySpan<int> CeilingStart,
            ReadOnlySpan<int> FloorEnd,
            ReadOnlySpan<int> WallStart,
            ReadOnlySpan<int> WallEnd
            )
        {
            RenderColumnStatus status;
            int ceilingStart = CeilingStart[x];
            int floorEnd = FloorEnd[x];
            int wallStart = WallStart[x];
            int wallEnd = WallEnd[x];

            bool windowExists = ceilingStart < floorEnd;

            bool canRenderCeiling = windowExists && ceilingStart < wallStart && ceilingStart < floorEnd;
            bool canRenderFloor = windowExists && wallEnd < floorEnd;
            bool canRenderWall = windowExists && wallStart < wallEnd && ceilingStart < floorEnd;
            bool canRenderPortal = windowExists && floorEnd < ceilingStart && wallStart < floorEnd;

            RenderColumnStatus startingStatus = calculated ? RenderColumnStatus.Calculated : default;

            if (!windowExists || !(canRenderCeiling || canRenderFloor || canRenderWall || canRenderPortal))
            {
                status = RenderColumnStatus.FinishedRendering;
            }
            else
            {
                status = startingStatus;

                if (canRenderCeiling)
                {
                    status |= RenderColumnStatus.CanRenderCeiling;
                }

                if (canRenderFloor)
                {
                    status |= RenderColumnStatus.CanRenderFloor;
                }

                if (canRenderWall)
                {
                    status |= RenderColumnStatus.CanRenderWall;
                }

                if (canRenderPortal)
                {
                    status |= RenderColumnStatus.CanRenderPortal;
                }
            }

            Status[x] = status;
            return status;
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
