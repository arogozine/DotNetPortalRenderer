using RenderingEngine.Models;
using System.Numerics;

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

        private readonly float[] angleCache;
        private readonly float[] distanceCache;
        private readonly uint[] distanceMult;
        private readonly BGRA[] buffer;
        private int? lastSectorDistanceCache = null;

        public PortalRenderer(int width, int height)
        {
            PixelWidth = width;
            PixelHeight = height;
            VFov = 1f * height;
            WallHelper = new WallHelper(width, height, EngineConstants.CameraPlaneX, VFov);
            buffer = GC.AllocateUninitializedArray<BGRA>(width * height);
            distanceCache = new float[height];
            distanceMult = new uint[height];
            angleCache = new float[width];

            RenderWindowHelper = new RenderWindowHelper(width, height);

            GenerateAngleCache();
        }

        private void GenerateAngleCache()
        {
            int width = this.PixelWidth;
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;
            float cameraRay = -EngineConstants.CameraPlaneX;

            for (int x = 0; x < width; x++, cameraRay += cameraWidthIncr)
            {
                angleCache[x] = MathF.Atan(cameraRay);
            }
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

            float pz = player.Z;

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

        private readonly List<RenderableWall> transparentWalls = [];
        private readonly Queue<NeighborsToRender> sectorRenderQueue = [];

        public void DrawScreen(Span<BGRA> screen, PortalPlayerSnapshot player)
        {
            this.WallHelper.SetSnapShot(player);

            screen.Fill(BGRA.Black);

            ReadOnlySpan<Sector> sectors = Sectors;

            RenderWindowHelper.NewRender();

            sectorRenderQueue.Enqueue(new NeighborsToRender
            {
                SectorId = player.Sector
            });

            int renderDepth = 0;

            // render solid walls using a portal based approach
            do
            {
                NeighborsToRender sectorInfo = sectorRenderQueue.Dequeue();
                Sector sector = sectors[sectorInfo.SectorId];

                Span<Wall> parentWalls = sectorInfo.ParentWalls;

                Span<Wall> walls = WallHelper.DetermineWallsToRender(sector, parentWalls, player);

                List<RenderableWall> neighbors = RenderSector(player, sector, sectors, sectorInfo, walls, screen);

                // copy of the renderable area here
                RenderWindow[]? renderableArea = null;

                foreach (RenderableWall renderableWall in neighbors)
                {
                    Wall neighbor = renderableWall.Wall;

                    var neighborToRender = new NeighborsToRender(renderableWall, parentWalls)
                    {
                        SectorId = neighbor.Neighbor
                    };

                    sectorRenderQueue.Enqueue(neighborToRender);

                    if (renderableWall.IsTransparent)
                    {
                        renderableArea ??= RenderWindowHelper.CopyRenderWindow();
                        renderableWall.RenderWindow = renderableArea;
                        transparentWalls.Add(renderableWall);
                    }
                }

                // DebugPortal(screen, this.RenderWindowHelper.RenderWindow);
            }
            while (sectorRenderQueue.Count > 0 && ++renderDepth < EngineConstants.MaxPortalsRendered);

            // render transparent objects and sprites
            ReadOnlySpan<RenderableWall> transparentWallsSpan = CollectionsMarshal.AsSpan(transparentWalls);
            for (int i = transparentWallsSpan.Length - 1; i >= 0; i--)
            {
                DrawTransparentWall(screen, sectors, transparentWallsSpan[i]);
            }

            // DebugZBuffer(screen, this.RenderWindowHelper.RenderWindow);

            transparentWalls.Clear();
            sectorRenderQueue.Clear();
        }

        private readonly List<RenderableWall> neightbors = [];

        private List<RenderableWall> RenderSector(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            NeighborsToRender sectorInfo,
            Span<Wall> walls,
            Span<BGRA> screen)
        {
            neightbors.Clear();

            if (sector.Floor == sector.Ceil)
            {
                return neightbors;
            }

            GenerateDistanceCache(player, sectorInfo, sector);
            RenderWindowHelper.NewSector(sectorInfo);

            List<RenderableWall> renderableWalls = [];

            for (int s = 0; s < walls.Length; s++)
            {
                Wall wall = walls[s];

                CalculateRenderWindow(wall, sector, renderableWalls, sectors);
            }

            ref Texture groundTexture = ref TextureCache.GetTexture(sector.FloorTexture);
            ref Texture ceilingTexture = ref TextureCache.GetTexture(sector.CeilTexture);

            if (Vector.IsHardwareAccelerated)
            {
                RenderFloorVector(player, sector, screen, ref groundTexture);

                if (sector.HasSkybox)
                {
                    RenderSkyboxVector(player, screen, ref ceilingTexture);
                }
                else
                {
                    RenderCeilingVector(player, sector, screen, ref ceilingTexture);
                }
            }
            else
            {
                RenderFloor(player, sector, screen, ref groundTexture);

                if (sector.HasSkybox)
                {
                    RenderSkybox(player, screen, ref ceilingTexture);
                }
                else
                {
                    RenderCeiling(player, sector, screen, ref ceilingTexture);
                }
            }

            for (int s = 0; s < renderableWalls.Count; s++)
            {
                RenderableWall renderableWall = renderableWalls[s];
                Wall wall = renderableWall.Wall;

                bool wallDrawn = wall.IsPortal ?
                    DrawPortalWall(screen, sector, sectors, renderableWall) :
                    DrawBasicWall(screen, sector, renderableWall);

                if (wallDrawn && wall.IsPortal)
                {
                    neightbors.Add(renderableWall);
                }
            }

            return neightbors;
        }

        private void DebugZBuffer(Span<BGRA> screen, Span<RenderWindow> window)
        {
            int height = Math.Min(PixelHeight, 20);
            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));


            float min = float.MaxValue;
            float max = float.MinValue;
            for (int x = 0; x < PixelWidth; x++)
            {
                ref RenderWindow renderWindow = ref window[x];
                float value = renderWindow.Distance;

                min = MathF.Min(value, min);
                max = MathF.Max(value, max);
            }

            float range = byte.MaxValue / max;

            for (int x = 0; x < PixelWidth; x++)
            {
                ref RenderWindow renderWindow = ref window[x];
                float value = renderWindow.Distance;
                uint val = (uint) Math.Clamp((int)(value * range), 0, byte.MaxValue);

                const uint Alpha = (uint)byte.MaxValue << 24;
                uint b = val;
                uint g = val << 8;
                uint r = val << 16;

                val = b | g | r | Alpha;


                for (int y = 0; y < height; y++)
                {
                    int index = y * PixelWidth + x;
                    Unsafe.Add(ref screenPtr, index) = val;
                }
            }
        }

        private void DebugPortal(
            Span<BGRA> screen,
            Span<RenderWindow> renderedArea)
        {
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
                int index = (y - 1) * PixelWidth + x;

                if (index > 0 && index < screen.Length)
                {
                    screen[index] = color;
                }

                index += PixelWidth;
                if (index > 0 && index < screen.Length)
                {
                    screen[index] = color;
                }

                index += PixelWidth;
                if (index > 0 && index < screen.Length)
                {
                    screen[index] = color;
                }
            }
        }

        private void CalculateRenderWindow(Wall wall, Sector sector, List<RenderableWall> renderableWalls, ReadOnlySpan<Sector> sectors)
        {
            Span<RenderWindow> renderedArea = RenderWindowHelper.RenderWindow;

            if (!RenderWindowHelper.SetWallToCalculate(wall))
            {
                return;
            }

            (int offset, int wallFromX, int wallToX) = RenderWindowHelper.GetWallRenderWindowX();

            WallYPlaneInfo yPlaneInfo = CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            if (wallToX <= wallFromX)
            {
                return;
            }

            bool renderUpperWallAsSky = sector.HasSkybox && wall.IsPortal && wall.Line.UpperTexture is null;

            if (renderUpperWallAsSky)
            {
                var n = sectors[wall.Neighbor];
                renderUpperWallAsSky &= n.Ceil == n.Floor;
            }

            int renderableFromX = wallFromX;
            int renderableToX = wallToX;

            for (int x = wallFromX; x <= wallToX; x++)
            {
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
                            Offset = offset,
                            Sector = sector
                        });
                    }

                    renderableFromX = x;
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;

                renderedAreaX.Calculated = true;
                renderedAreaX.WallStart = renderUpperWallAsSky ? wallEndYInt : wallStartYInt;
                renderedAreaX.WallEnd = wallEndYInt;

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
                    Offset = offset,
                    Sector = sector
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
