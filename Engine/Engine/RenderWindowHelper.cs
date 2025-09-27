using RenderingEngine.Models;
using System.Diagnostics.CodeAnalysis;

namespace RenderingEngine.Engine
{
    internal struct RenderWindow
    {
        public bool Calculated;
        public int CeilingStart;
        public int WallStart;
        public int WallEnd;
        public int FloorEnd;

        public override readonly string ToString()
        {
            return $"{Calculated}: {CeilingStart} {WallStart} {WallEnd} {FloorEnd}";
        }

    }

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
            (sectorFromX, sectorToX) = (sectorInfo.RenderableWall?.XLeft ?? 0, sectorInfo.RenderableWall?.XRight ?? width);

            for (int i = sectorFromX; i < sectorToX; i++)
            {
                renderWindow[i].Calculated = false;
            }
        }

        [MemberNotNull(nameof(wall))]
        public bool SetWallToRender2(Wall wall)
        {
            this.wall = wall;
            this.wallFromX = wall.XLeft;
            this.wallToX = wall.XRight;

            // clamp to sector window
            wallFromX = Math.Max(sectorFromX, wall.XLeft);
            wallToX = Math.Min(sectorToX, wall.XRight);

            // find where rendering didn't take place
            for (; wallFromX <= wallToX; wallFromX++)
            {
                ref RenderWindow window = ref renderWindow[wallFromX];

                if (window.Calculated)
                {
                    break;
                }
            }

            for (; wallToX > wallFromX; wallToX--)
            {
                ref RenderWindow window = ref renderWindow[wallToX];

                if (window.Calculated)
                {
                    break;
                }
            }

            return wallFromX < wallToX;
        }

        [MemberNotNull(nameof(wall))]
        public bool SetWallToRender3(Wall wall)
        {
            this.wall = wall;
            this.wallFromX = wall.XLeft;
            this.wallToX = wall.XRight;

            // clamp to sector window
            wallFromX = Math.Max(sectorFromX, wall.XLeft);
            wallToX = Math.Min(sectorToX, wall.XRight);

            // find where rendering didn't take place
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

            return wallFromX < wallToX;
        }

        public (int offset, int WallFromX, int WallToX) GetWallRenderWindowX()
        {
            ArgumentNullException.ThrowIfNull(wall);

            int wallFromXOffset = wallFromX > wall.XLeft ? wallFromX - wall.XLeft : 0;

            return (wallFromXOffset, wallFromX, wallToX);
        }


        public RenderWindow TryGetRenderableDimensionsForX2(int x, int wallStartY, int wallEndY)
        {
            if (sectorFromX > x || x > sectorToX)
                return default;

            ref RenderWindow window = ref this.renderWindow[x];

            if (!window.Calculated || window.WallStart >= window.WallEnd)
                return default;

            return window;
        }

        public RenderWindow GetFloorCeilDimensions2(int x)
        {
            if (sectorFromX > x || x > sectorToX)
                return default;

            ref RenderWindow window = ref this.renderWindow[x];

            if (!window.Calculated)
                return default;

            return this.renderWindow[x];
        }
    }
}
