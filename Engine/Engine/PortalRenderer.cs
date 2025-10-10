using RenderingEngine.Models;
using RenderingEngine.TextureManagement;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        public readonly int PixelWidth;
        public readonly int PixelHeight;
        public readonly float VFov;
        public required Player Player { get; set; }
        public required Sector[] Sectors { get; set; }

        private readonly WallHelper WallHelper;
        private readonly RenderWindowHelper RenderWindowHelper;

        private PortalPlayerSnapshot? Snapshot = null;

        private readonly float[] distanceCache;
        private readonly uint[] distanceMult;
        private readonly BGRA[] buffer;
        private int? lastSectorDistanceCache = null;

        public PortalRenderer(int width, int height)
        {
            PixelWidth = width;
            PixelHeight = height;
            VFov = .7f * height;
            WallHelper = new WallHelper(width, height, EngineConstants.CameraPlaneX, VFov);
            buffer = GC.AllocateUninitializedArray<BGRA>(width * height);
            distanceCache = new float[height];
            distanceMult = new uint[height];

            RenderWindowHelper = new RenderWindowHelper(width, height);
        }

        private void GenerateDistanceCache(PortalPlayerSnapshot player,
                        NeighborsToRender sectorInfo,
                        Sector sector)
        {
            if (lastSectorDistanceCache == sectorInfo.SectorId)
            {
                return;
            }

            lastSectorDistanceCache = sectorInfo.SectorId;

            float[] distanceArray = distanceCache;

            int height = PixelHeight;
            float vFov = VFov;
            int halfHeightInt = height / 2;
            float oneOvervFov = 1f / vFov;
            var yaw = player.Yaw;

            (_, _, float pz) = player.Where;

            float yfloor = sector.Floor - pz;
            float yCeil = sector.Ceil - pz;

            for (int i = 0; i < halfHeightInt; i++)
            {
                int j = halfHeightInt - i;

                float yMopPosR = yCeil / (j * oneOvervFov + yaw);
                float distance = yMopPosR + 1;
                distanceArray[i] = distance;

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * distance;
                distanceMult[i] = (uint)(brightness * 255f);
            }

            for (int i = halfHeightInt + 1; i < height; i++)
            {
                int j = halfHeightInt - i;

                float yMopPosR = yfloor / (j * oneOvervFov + yaw);
                float distance = yMopPosR + 1;
                distanceArray[i] = distance;

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * distance;
                distanceMult[i] = (uint)(brightness * 255f);
            }
        }

        public void DrawScreen(Span<BGRA> screen, PortalPlayerSnapshot player)
        {
            float yaw = player.Yaw;

            (float pSin, float pCos) = MathF.SinCos(player.Angle);
            (float px, float py, float pz) = player.Where;
            screen.Fill(BGRA.Black);

            ReadOnlySpan<Sector> sectors = Sectors;

            RenderWindowHelper.NewRender();

            Queue<NeighborsToRender> sectorRenderQueue = [];
            sectorRenderQueue.Enqueue(new NeighborsToRender
            {
                SectorId = player.Sector
            });

            int renderDepth = 0;

            do
            {
                NeighborsToRender sectorInfo = sectorRenderQueue.Dequeue();
                Sector sector = sectors[sectorInfo.SectorId];

                float yceil = sector.Ceil - pz;
                float yfloor = sector.Floor - pz;
                Span<Wall> parentWalls = CollectionsMarshal.AsSpan(sectorInfo.ParentWalls);

                Span<Wall> walls = WallHelper.DetermineWallsToRender(sector,
                    parentWalls, pSin, pCos, px, py, yceil, yfloor, yaw);

                List<RenderableWall> neighbors = RenderSector(player, sector, sectors, sectorInfo, walls, screen);

                foreach (RenderableWall renderableWall in neighbors)
                {
                    Wall neighbor = renderableWall.Wall;

                    var neighborToRender = new NeighborsToRender(renderableWall)
                    {
                        SectorId = neighbor.Neighbor
                    };
                    neighborToRender.ParentWalls.AddRange(parentWalls);

                    sectorRenderQueue.Enqueue(neighborToRender);
                }

                DebugPortal(screen, this.RenderWindowHelper.RenderWindow);

            }
            while (sectorRenderQueue.Count > 0 && ++renderDepth < EngineConstants.MaxPortalsRendered);
        }

        private List<RenderableWall> RenderSector(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            NeighborsToRender sectorInfo,
            Span<Wall> walls,
            Span<BGRA> screen)
        {
            List<RenderableWall> neightbors = [];

            if (sector.Floor == sector.Ceil)
            {
                return neightbors;
            }

            GenerateDistanceCache(player, sectorInfo, sector);
            RenderWindowHelper.NewSector(sectorInfo);

            TextureInfo wallTexture = TextureLoader.GetTexture(TextureName.Rock, true);
            // TextureInfo groundTexture = TextureLoader.GetTexture(TextureName.CaveGround, false);
            // TextureInfo ceilingTexture = TextureLoader.GetTexture(TextureName.CeilingOffice, false);

            List<RenderableWall> renderableWalls = [];

            for (int s = 0; s < walls.Length; s++)
            {
                Wall wall = walls[s];

                CalculateRenderWindow(wall, renderableWalls);
            }

            TextureInfo groundTexture = TextureCache.GetTexture(sector.FloorTexture, false);
            TextureInfo ceilingTexture = TextureCache.GetTexture(sector.FloorTexture, false);

            if (Vector.IsHardwareAccelerated)
            {
                RenderFloorVector(player, sector, screen, groundTexture);
                RenderCeilingVector(player, sector, screen, ceilingTexture);
            }
            else
            {
                RenderFloor(player, sector, screen, groundTexture);
                RenderCeiling(player, sector, screen, ceilingTexture);
            }

            for (int s = 0; s < renderableWalls.Count; s++)
            {
                RenderableWall renderableWall = renderableWalls[s];
                Wall wall = renderableWall.Wall;

                bool wallDrawn = wall.Neighbor == EngineConstants.NullSector ?
                    DrawBasicWall(screen, wallTexture, renderableWall) :
                    DrawPortalWall(sector, sectors, screen, wallTexture, renderableWall);

                if (wallDrawn && wall.Neighbor != EngineConstants.NullSector)
                {
                    neightbors.Add(renderableWall);
                }
            }

            return neightbors;
        }

        private void DebugPortal(
            Span<BGRA> screen,
            Span<RenderWindow> renderedArea)
        {
            int height = PixelWidth;

            for (int x = 0; x < PixelWidth; x++)
            {
                ref RenderWindow rendered = ref renderedArea[x];

                /*
                if (!rendered.Calculated)
                    continue;
                */
                Render(screen, rendered.CeilingStart, x, BGRA.Red);
                Render(screen, rendered.FloorEnd, x, BGRA.Blue);
                Render(screen, rendered.WallStart, x, BGRA.Green);
                Render(screen, rendered.WallEnd, x, BGRA.Yellow);

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Render(Span<BGRA> screen, int y, int x, BGRA color)
            {
                int index = (y - 1) * height + x;

                if (y != 0)
                {
                    screen[index] = color;
                }

                index += height;
                screen[index] = color;
                index += height;

                if (y != PixelHeight - 1)
                {
                    screen[index] = color;
                }
            }
        }

        private void CalculateRenderWindow(Wall wall, List<RenderableWall> renderableWalls)
        {
            Span<RenderWindow> renderedArea = RenderWindowHelper.RenderWindow;

            if (!RenderWindowHelper.SetWallToCalculate(wall))
            {
                return;
            }

            (int offset, int wallFromX, int wallToX) = RenderWindowHelper.GetWallRenderWindowX();

            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            if (wallToX <= wallFromX)
            {
                return;
            }

            int renderableFromX = wallFromX;
            int renderableToX = wallToX;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;

                ref RenderWindow renderedAreaX = ref renderedArea[x];

                if (renderedAreaX.Calculated || wallStartY >= wallEndY || renderedAreaX.CeilingStart >= renderedAreaX.FloorEnd)
                {

                    if (x - 1 > renderableFromX)
                    {
                        offset = wallFromX > wall.XLeft ? wallFromX - wall.XLeft : 0;

                        renderableWalls.Add(new RenderableWall
                        {
                            Wall = wall,
                            XLeft = renderableFromX,
                            XRight = x,
                            Offset = offset
                        });

                        renderableFromX = x;
                    }

                    renderableFromX = x;
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                renderedAreaX.Calculated = true;
                renderedAreaX.WallStart = Math.Clamp(wallStartYInt, renderedAreaX.CeilingStart, renderedAreaX.FloorEnd);
                renderedAreaX.WallEnd = Math.Clamp(wallEndYInt, renderedAreaX.CeilingStart, renderedAreaX.FloorEnd);

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }

            if (renderableToX > renderableFromX)
            {
                offset = renderableFromX > wall.XLeft ? renderableFromX - wall.XLeft : 0;

                renderableWalls.Add(new RenderableWall {
                    Wall = wall,
                    XLeft = renderableFromX,
                    XRight = renderableToX,
                    Offset = offset
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShadeByPrecalc(ref BGRA inColor, ref BGRA outColor, ref uint scale)
        {
            unchecked
            {
                const uint Alpha = (uint)byte.MaxValue << 24;

                uint b = inColor.B * scale >> 8;
                uint g = inColor.G * scale >> 8 << 8;
                uint r = inColor.R * scale >> 8 << 16;

                uint bgra = b | g | r | Alpha;

                Unsafe.As<BGRA, uint>(ref outColor) = bgra;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ShadeByBrightness2(BGRA inColor, float brightness)
        {
            const uint Alpha = (uint)byte.MaxValue << 24;

            if (brightness <= 0)
            {
                return Alpha;
            }

            unchecked
            {
                uint scale = (uint)(brightness * 255f);

                uint b = inColor.B * scale >> 8;
                uint g = inColor.G * scale >> 8 << 8;
                uint r = inColor.R * scale >> 8 << 16;

                return b | g | r | Alpha;
            }
        }

        [MemberNotNull(nameof(Snapshot))]
        public BGRA[] DrawFrame(PortalPlayerSnapshot snapShot)
        {
            Snapshot = snapShot;

            Span<BGRA> currentBuffer = buffer;
            DrawScreen(currentBuffer, snapShot);

            return buffer;
        }
    }
}
