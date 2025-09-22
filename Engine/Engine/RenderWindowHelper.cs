using RenderingEngine.Models;
using System.Diagnostics.CodeAnalysis;

namespace RenderingEngine.Engine
{
    internal sealed class RenderWindowHelper
    {
        private readonly int width;
        private readonly int height;
        private readonly (int top, int bottom)[] sectorPortalWindowOld;
        private readonly (int top, int bottom)[] sectorPortalWindow;
        private readonly RenderedArea[] renderedArea;

        private int sectorFromX;
        private int sectorToX;

        private Wall? wall;
        private int wallFromX;
        private int wallToX;

        public Span<(int top, int bottom)> Portal => sectorPortalWindow;
        public Span<RenderedArea> RenderedArea => renderedArea;

        public RenderWindowHelper(int width, int height)
        {
            this.width = width;
            this.height = height;

            var portalTopBottom = new (int, int)[width];
            portalTopBottom.AsSpan().Fill((0, height - 1));
            sectorPortalWindowOld = new (int, int)[width];

            sectorPortalWindow = portalTopBottom;
            renderedArea = new RenderedArea[width];

            sectorFromX = 0;
            sectorToX = width;
        }

        public void NewRender()
        {
            sectorPortalWindow.AsSpan().Fill((0, height - 1));
        }

        public void NewSector(NeighborsToRender sectorInfo)
        {
            sectorPortalWindow.AsSpan().CopyTo(sectorPortalWindowOld);
            (sectorFromX, sectorToX) = (sectorInfo.Wall?.XLeft ?? 0, sectorInfo.Wall?.XRight ?? width);
            renderedArea.AsSpan().Clear();
        }

        [MemberNotNull(nameof(wall))]
        public bool SetWallToRender(Wall wall)
        {
            this.wall = wall;
            CalculateWallFromToX();
            return wallToX > wallFromX;
        }

        public (int offset, int WallFromX, int WallToX) GetWallRenderWindowX()
        {
            ArgumentNullException.ThrowIfNull(wall);

            int wallFromXOffset = wallFromX > wall.XLeft ? wallFromX - wall.XLeft : 0;
            return (wallFromXOffset, wallFromX, wallToX);
        }

        internal readonly ref struct WallDimensions
        {
            public readonly bool CanRender;
            public readonly int PortalFromY;
            public readonly int PortalToY;
            public readonly int ClampedFromY;
            public readonly int ClampedToY;

            public WallDimensions(bool canRender, int portalFromY, int portalToY, int clampedFromY, int clampedToY)
            {
                CanRender = canRender;
                PortalFromY = portalFromY;
                PortalToY = portalToY;
                ClampedFromY = clampedFromY;
                ClampedToY = clampedToY;
            }
        }

        public WallDimensions TryGetRenderableDimensionsForX(int x, int wallStartY, int wallEndY)
        {
            (int portalFromY, int portalToY) = sectorPortalWindow[x];

            // 1. wall smaller than one pixel, don't render
            // 2. no portal window available, don't render 
            if (wallStartY >= wallEndY || portalFromY >= portalToY)
            {
                return default;
            }

            ref RenderedArea renderTopBottom = ref renderedArea[x];

            // already taken up by a wall this sector, don't render
            if (renderTopBottom.Calculated)
            {
                return default;
            }

            // calculate renderable wall portion based on portal
            int clamptedFromY = Math.Clamp(wallStartY, portalFromY, portalToY);
            int clamptedToY = Math.Clamp(wallEndY, portalFromY, portalToY);

            // 
            renderTopBottom.Calculated = true;
            renderTopBottom.From = clamptedFromY;
            renderTopBottom.To = clamptedToY;

            // calculate new portal
            if (wallStartY > portalFromY)
            {
                sectorPortalWindow[x] = (portalFromY, wallStartY);
            }
            else if (wallEndY < portalToY)
            {
                sectorPortalWindow[x] = (wallEndY, portalToY);
            }
            else
            {
                // wall blocks entire portal
                sectorPortalWindow[x] = (0, 0);
            }

            return new WallDimensions(true, portalFromY, portalToY, clamptedFromY, clamptedToY);
        }

        internal readonly ref struct FloorCeilDimensions
        {
            public readonly bool CanRender;
            public readonly int PortalFromY;
            public readonly int PortalToY;
            public readonly int RenderedFromY;
            public readonly int RenderedToY;

            public FloorCeilDimensions(bool canRender, int portalFromY, int portalToY, int renderedFromY, int renderedToY)
            {
                CanRender = canRender;
                PortalFromY = portalFromY;
                PortalToY = portalToY;
                RenderedFromY = renderedFromY;
                RenderedToY = renderedToY;
            }
        }

        public FloorCeilDimensions GetFloorCeilDimensions(int x)
        {
            ref RenderedArea renderTopBottom = ref renderedArea[x];

            if (!renderTopBottom.Calculated)
            {
                return default;
            }

            if (renderTopBottom.From == 0 && renderTopBottom.To == height - 1)
            {
                return default;
            }

            (int portalFrom, int portalTo) = sectorPortalWindowOld[x];

            if (portalFrom == portalTo)
            {
                return default;
            }

            return new FloorCeilDimensions(true, portalFrom, portalTo, renderTopBottom.From, renderTopBottom.To);
        }

        private void CalculateWallFromToX()
        {
            ArgumentNullException.ThrowIfNull(wall);

            // clamp to sector window
            wallFromX = Math.Max(sectorFromX, wall.XLeft);
            wallToX = Math.Min(sectorToX, wall.XRight);

            // find where rendering didn't take place
            for (; wallFromX <= wallToX; wallFromX++)
            {
                ref RenderedArea renderTopBottom = ref renderedArea[wallFromX];

                if (!renderTopBottom.Calculated)
                {
                    break;
                }
            }

            for (; wallToX > wallFromX; wallToX--)
            {
                ref RenderedArea renderTopBottom = ref renderedArea[wallToX];

                if (!renderTopBottom.Calculated)
                {
                    break;
                }
            }
        }
    }
}
