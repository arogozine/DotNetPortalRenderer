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

        private Wall? wall;
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
                WallEnd = height - 1
            });
        }

        public void NewSector(NeighborsToRender sectorInfo)
        {
            if (sectorInfo.RenderableWall is RenderableWall renderableWall)
            {
                (sectorFromX, sectorToX) = (renderableWall.XLeft, renderableWall.XRight);
            }
            else
            {
                (sectorFromX, sectorToX) = (0, width - 1);
            }

            for (int i = sectorFromX; i <= sectorToX; i++)
            {
                ref RenderWindow render = ref renderWindow[i];
                render.Calculated = render.CeilingStart == render.FloorEnd;
            }
        }

        public RenderWindow[] CopyRenderWindow()
        {
            RenderWindow[] renderWindow = new RenderWindow[this.renderWindow.Length];
            this.renderWindow.AsSpan()[sectorFromX..sectorToX].CopyTo(renderWindow.AsSpan()[sectorFromX..sectorToX]);
            return renderWindow;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int SectroFromX, int SectorToX) GetSectorX()
        {
            return (sectorFromX, sectorToX);
        }


        [MemberNotNull(nameof(wall))]
        public bool SetWallToCalculate(Wall wall)
        {
            this.wall = wall;
            this.wallFromX = wall.XLeft;
            this.wallToX = wall.XRight;

            // clamp to sector window
            wallFromX = Math.Max(sectorFromX, wall.XLeft);
            wallToX = Math.Min(sectorToX, wall.XRight);

            // skip calculated areas
            for (; wallFromX <= wallToX; wallFromX++)
            {
                ref RenderWindow window = ref renderWindow[wallFromX];

                if (!window.Calculated)
                {
                    break;
                }
            }

            for (; wallToX > wallFromX; wallToX--)
            {
                ref RenderWindow window = ref renderWindow[wallToX];

                if (!window.Calculated)
                {
                    break;
                }
            }

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

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref RenderWindow TryGetRenderableDimensionsForX2(int x)
        {
            if (sectorFromX > x || x > sectorToX)
                return ref Unsafe.NullRef<RenderWindow>();

            ref RenderWindow window = ref this.renderWindow[x];

            if (!window.Calculated || window.WallStart >= window.WallEnd)
                return ref Unsafe.NullRef<RenderWindow>();

            return ref window;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref RenderWindow GetFloorCeilDimensions2(int x)
        {
            if (sectorFromX > x || x > sectorToX)
                return ref Unsafe.NullRef<RenderWindow>();

            ref RenderWindow window = ref this.renderWindow[x];

            if (!window.Calculated || window.WallStart >= window.WallEnd)
                return ref Unsafe.NullRef<RenderWindow>();

            return ref window;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref RenderWindow GetCeilingDimensions(int x)
        {
            if (sectorFromX > x || x > sectorToX)
                return ref Unsafe.NullRef<RenderWindow>();

            ref RenderWindow window = ref this.renderWindow[x];

            if (!window.Calculated || window.CeilingStart >= window.WallEnd)
                return ref Unsafe.NullRef<RenderWindow>();

            return ref window;
        }
    }
}
