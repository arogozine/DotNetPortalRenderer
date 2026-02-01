using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed class RenderWindowHelper
    {
        private readonly int width;
        private readonly int height;

        public RenderColumnStatus[] Status { get; }
        public int[] CeilingStart { get; }
        public int[] WallStart { get; }
        public int[] WallEnd { get; }
        public int[] FloorEnd { get; }
        public float[] Distance { get; }
        public int[] TopTextureXLocation { get; }
        public int[] BottomTextureXLocation { get; }
        public int[] TopTextureYLocation { get; }
        public int[] BottomTextureYLocation { get; }
        public int[] ClampedFrom { get; }
        public int[] ClampedTo { get; }
        public int[] TextureXPos { get; }


        private int sectorFromX;
        private int sectorToX;

        private RenderableWall? wall;
        private int wallFromX;
        private int wallToX;

        public int SectorFrom => sectorFromX;
        public int SectorTo => sectorToX;

        public RenderWindowHelper(int width, int height)
        {
            this.width = width;
            this.height = height;

            Status = new RenderColumnStatus[width];
            CeilingStart = new int[width];
            WallStart = new int[width];
            WallEnd = new int[width];
            FloorEnd = new int[width];
            Distance = new float[width];
            TopTextureXLocation = new int[width];
            BottomTextureXLocation = new int[width];
            TopTextureYLocation = new int[width];
            BottomTextureYLocation = new int[width];
            ClampedFrom = new int[width];
            ClampedTo = new int[width];
            TextureXPos = new int[width];

            sectorFromX = 0;
            sectorToX = width;
        }

        public void NewRender()
        {
            Status.AsSpan().Fill(RenderColumnStatus.NewRender);
            CeilingStart.AsSpan().Clear();
            FloorEnd.AsSpan().Fill(height - 1);
            WallEnd.AsSpan().Fill(height - 1);
            Distance.AsSpan().Fill(float.MaxValue);
            TopTextureXLocation.AsSpan().Clear();
            BottomTextureXLocation.AsSpan().Clear();
            TopTextureYLocation.AsSpan().Clear();
            BottomTextureYLocation.AsSpan().Clear();
        }

        public RenderColumnStatus NewDepth()
        {
            RenderColumnStatus renderColumnStatus = default;

            for (int i = 0; i < this.width; i++)
            {
                RenderColumnStatus columnStatus = Status[i];

                if (columnStatus.IsFinished)
                {
                    continue;
                }
                else if (columnStatus.IsCalculated)
                {
                    columnStatus = RecalculateRenderWindow(i, false);
                }
                else
                {
                    columnStatus = RenderColumnStatus.FinishedRendering;
                    Status[i] = RenderColumnStatus.FinishedRendering;
                }

                renderColumnStatus |= columnStatus;
            }

            // this allows us to know what, if anything, we can still render
            return renderColumnStatus;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int ClampedWallStart, int ClampedWallEnd) GetClampedWallFromTo(int x)
        {
            int ceilingStart = this.CeilingStart[x];
            int wallStart = this.WallStart[x];
            int wallEnd = this.WallEnd[x];
            int floorEnd = this.FloorEnd[x];

            int portalFromYClamped = Math.Clamp(wallStart, ceilingStart, floorEnd);
            int portalToYClamped = Math.Clamp(wallEnd, ceilingStart, floorEnd);

            return (portalFromYClamped, portalToYClamped);
        }

        public RenderColumnStatus RecalculateRenderWindow(int x, bool calculated)
        {
            RenderColumnStatus status;
            int ceilingStart = this.CeilingStart[x];
            int floorEnd = this.FloorEnd[x];
            int wallStart = this.WallStart[x];
            int wallEnd = this.WallEnd[x];

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

            this.Status[x] = status;
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

        /*
        // for transparency
        public RenderWindow[] CopyRenderWindow(bool partial)
        {
            RenderWindow[] renderWindow = new RenderWindow[this.renderWindow.Length];
            Span<RenderWindow> span = this.renderWindow.AsSpan();
            Span<RenderWindow> renderWindowSpan = renderWindow.AsSpan();

            if (partial)
            {
                span[sectorFromX..sectorToX].CopyTo(renderWindowSpan[sectorFromX..sectorToX]);
            }
            else
            {
                span.CopyTo(renderWindowSpan);
            }

            return renderWindow;
        }
        */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int SectroFromX, int SectorToX) GetSectorX()
        {
            return (sectorFromX, sectorToX);
        }


        [MemberNotNull(nameof(wall))]
        public bool SetWallToCalculate(RenderableWall wall)
        {
            this.wall = wall;
            this.wallFromX = wall.XLeft;
            this.wallToX = wall.XRight;

            // clamp to sector window
            wallFromX = Math.Max(sectorFromX, wall.XLeft);
            wallToX = Math.Min(sectorToX, wall.XRight);

            int i, j;

            for (i = wallFromX; i <= wallToX; i++)
            {
                RenderColumnStatus columnStatus = Status[i];

                if (!columnStatus.IsFinished && !columnStatus.IsCalculated)
                {
                    break;
                }
            }

            for (j = wallToX; j >= wallFromX; j--)
            {
                RenderColumnStatus columnStatus = Status[j];

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
