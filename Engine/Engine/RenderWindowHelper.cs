using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed class RenderWindowHelper
    {
        private readonly int width;
        private readonly int height;
        private readonly RenderWindow[] renderWindow;

        private int sectorFromX;
        private int sectorToX;

        private RenderableWall? wall;
        private int wallFromX;
        private int wallToX;

        public int SectorFrom => sectorFromX;
        public int SectorTo => sectorToX;
        public Span<RenderWindow> RenderWindow => renderWindow;

        public RenderWindowHelper(int width, int height)
        {
            this.width = width;
            this.height = height;

            renderWindow = new RenderWindow[width];

            sectorFromX = 0;
            sectorToX = width;
        }

        public void NewRender()
        {
            renderWindow.AsSpan().Fill(new RenderWindow {
                CeilingStart = 0,
                FloorEnd = height - 1,
                WallEnd = height - 1,
                Distance = float.MaxValue,
                Status = RenderColumnStatus.NewRender
            });
        }

        public RenderColumnStatus NewDepth()
        {
            RenderColumnStatus renderColumnStatus = default;

            for (int i = 0; i < renderWindow.Length; i++)
            {
                ref RenderWindow render = ref renderWindow[i];

                if (render.Finished)
                {
                    continue;
                }
                else if (render.Calculated)
                {
                    RecalculateRenderWindow(ref render, false);
                }
                else
                {
                    render.Status = RenderColumnStatus.FinishedRendering;
                }

                renderColumnStatus |= render.Status;
            }

            // this allows us to know what, if anything, we can still render
            return renderColumnStatus;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int ClampedWallStart, int ClampedWallEnd) GetClampedWallFromTo(ref RenderWindow renderWindow)
        {
            int portalFromYClamped = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);
            int portalToYClamped = Math.Clamp(renderWindow.WallEnd, renderWindow.CeilingStart, renderWindow.FloorEnd);
            return (portalFromYClamped, portalToYClamped);
        }

        public static void RecalculateRenderWindow(ref RenderWindow render, bool calculated)
        {
            bool windowExists = render.CeilingStart < render.FloorEnd;

            bool canRenderCeiling = windowExists && render.CeilingStart < render.WallStart && render.CeilingStart < render.FloorEnd;
            bool canRenderFloor = windowExists && render.WallEnd < render.FloorEnd;
            bool canRenderWall = windowExists && render.WallStart < render.WallEnd && render.CeilingStart < render.FloorEnd;
            bool canRenderPortal = windowExists && render.FloorEnd < render.CeilingStart && render.WallStart < render.FloorEnd;

            RenderColumnStatus startingStatus = calculated ? RenderColumnStatus.Calculated : default;

            if (!windowExists || !(canRenderCeiling || canRenderFloor || canRenderWall || canRenderPortal))
            {
                render.Status = RenderColumnStatus.FinishedRendering;
            }
            else
            {
                render.Status = startingStatus;

                if (canRenderCeiling)
                {
                    render.Status |= RenderColumnStatus.CanRenderCeiling;
                }

                if (canRenderFloor)
                {
                    render.Status |= RenderColumnStatus.CanRenderFloor;
                }

                if (canRenderWall)
                {
                    render.Status |= RenderColumnStatus.CanRenderWall;
                }

                if (canRenderPortal)
                {
                    render.Status |= RenderColumnStatus.CanRenderPortal;
                }
            }
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
                ref RenderWindow window = ref renderWindow[i];

                if (!window.Finished && !window.Calculated)
                {
                    break;
                }
            }

            for (j = wallToX; j >= wallFromX; j--)
            {
                ref RenderWindow window = ref renderWindow[j];

                if (!window.Finished && !window.Calculated)
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
