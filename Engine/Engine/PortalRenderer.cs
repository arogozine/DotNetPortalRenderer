using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        public readonly int PixelWidth;
        public readonly int PixelHeight;
        public required Sprite[] Sprites { get; set; }
        public required Player Player { get; set; }
        public required Sector[] Sectors { get; set; }

        private readonly WallHelper WallHelper;
        private readonly SpriteHelper SpriteHelper;

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
            SpriteHelper = new SpriteHelper(width, height, EngineConstants.CameraPlaneX);
            WallHelper = new WallHelper(width, height, EngineConstants.CameraPlaneX);
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
            int halfHeightInt = height / 2;
            float oneOverHeight = 1f / height;
            var yaw = player.Yaw;

            float pz = player.Z;

            float yfloor = sector.Floor - pz;
            float yCeil = sector.Ceil - pz;

            for (int i = 0; i < halfHeightInt; i++)
            {
                int j = halfHeightInt - i;

                float yMopPosR = yCeil / (j * oneOverHeight + yaw);
                float distance = yMopPosR + 1;
                distanceArray[i] = distance;

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * distance;
                distanceMult[i] = (uint)(brightness * 255f);
            }

            for (int i = halfHeightInt + 1; i < height; i++)
            {
                int j = halfHeightInt - i;

                float yMopPosR = yfloor / (j * oneOverHeight + yaw);
                float distance = yMopPosR + 1;
                distanceArray[i] = distance;

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * distance;
                distanceMult[i] = (uint)(brightness * 255f);
            }
        }

        private readonly List<RenderableSprite> transparentWalls = [];
        private readonly Queue<NeighborsToRender> sectorRenderQueue = [];

        public void DrawScreen(Span<BGRA> screen, PortalPlayerSnapshot player)
        {
            this.WallHelper.SetSnapShot(player);

            ReadOnlySpan<Sector> sectors = Sectors;

            RenderWindowHelper.NewRender();

            sectorRenderQueue.Enqueue(new NeighborsToRender
            {
                SectorId = player.Sector
            });

            int renderDepth = 0;

            Span<Sprite> playerVisibleSprites = SpriteHelper.GetSpritesForPlayer(player, Sprites, Sectors);

            // render solid walls using a portal based approach
            do
            {
                NeighborsToRender sectorInfo = sectorRenderQueue.Dequeue();
                Sector sector = sectors[sectorInfo.SectorId];

                Span<Wall> parentWalls = sectorInfo.ParentWalls;

                Span<Wall> walls = WallHelper.DetermineWallsToRender(sector, parentWalls, player);

                // copy render window to render sprites
                SectorSprites sectorSprites = new()
                {
                    XLeft = sectorInfo.RenderableWall?.XLeft ?? 0,
                    XRight = sectorInfo.RenderableWall?.XRight ?? PixelWidth,
                    CeilingStart = new int[PixelWidth],
                    FloorEnd = new int[PixelWidth],
                    Distance = new float[PixelWidth],
                    Sector = sector,
                };


                for (int i = 0; i < RenderWindowHelper.RenderWindow.Length; i++)
                {
                    ref RenderWindow from = ref RenderWindowHelper.RenderWindow[i];
                    sectorSprites.CeilingStart[i] = from.CeilingStart;
                    sectorSprites.FloorEnd[i] = from.FloorEnd;
                }

                transparentWalls.Add(sectorSprites);

                CalculateRenderWindow(player, sectorInfo, sectors, sector, walls);
                List<RenderableWall> neighbors = RenderSector(player, sector, sectors, screen);

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

                    if (renderableWall.IsPortalWithMiddleTexture)
                    {
                        renderableArea ??= RenderWindowHelper.CopyRenderWindow(true);
                        renderableWall.RenderWindow = renderableArea;
                        transparentWalls.Add(new TransparentWall {
                            Offset = renderableWall.Offset,
                            XLeft = renderableWall.XLeft,
                            XRight = renderableWall.XRight,
                            RenderWindow = renderableArea,
                            Sector = renderableWall.Sector,
                            Wall = renderableWall.Wall
                        });
                    }
                }

                for (int i = 0; i < RenderWindowHelper.RenderWindow.Length; i++)
                {
                    ref RenderWindow from = ref RenderWindowHelper.RenderWindow[i];
                    sectorSprites.Distance[i] = from.Distance;
                }
            }
            while (sectorRenderQueue.Count > 0 && ++renderDepth < EngineConstants.MaxPortalsRendered);

            // render transparent walls and sprites
            Span<RenderableSprite> transparentWallsSpan = CollectionsMarshal.AsSpan(transparentWalls);
            for (int i = transparentWallsSpan.Length - 1; i >= 0; i--)
            {
                RenderableSprite renderableWall = transparentWallsSpan[i];

                if (renderableWall is TransparentWall transparentWall)
                {
                    DrawTransparentWall(screen, sectors, transparentWall);
                }
                else if (renderableWall is SectorSprites sectorSprites)
                {
                    List<Sprite> sprites = this.SpriteHelper.FilterOutSpritesOutsideSector(player, sectorSprites, playerVisibleSprites);

                    foreach (Sprite s in sprites)
                    {
                        DrawSprite(screen, s, sectorSprites);
                    }
                }
            }

            transparentWalls.Clear();
            sectorRenderQueue.Clear();
        }

        private readonly List<RenderableWall> neightbors = [];
        private readonly List<RenderableWall> renderableWalls = [];

        private void CalculateRenderWindow(
            PortalPlayerSnapshot player,
            NeighborsToRender sectorInfo,
            ReadOnlySpan<Sector> sectors,
            Sector sector,
            Span<Wall> walls)
        {
            neightbors.Clear();
            renderableWalls.Clear();

            if (sector.Floor == sector.Ceil)
            {
                return;
            }

            GenerateDistanceCache(player, sectorInfo, sector);
            RenderWindowHelper.NewSector(sectorInfo);

            for (int s = 0; s < walls.Length; s++)
            {
                Wall wall = walls[s];

                CalculateRenderWindow(wall, sector, renderableWalls, sectors);
            }
        }

        private List<RenderableWall> RenderSector(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            Span<BGRA> screen)
        {
            ref Texture groundTexture = ref TextureCache.GetTexture(sector.FloorTexture.Name);
            ref Texture ceilingTexture = ref TextureCache.GetTexture(sector.CeilTexture.Name);

            if (Vector.IsHardwareAccelerated)
            {
                RenderFloorVector(player, sector, screen, ref groundTexture);

                if (sector.CeilTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox))
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

                if (sector.CeilTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox))
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
                    DrawPortalWall(player, screen, sector, sectors, renderableWall) :
                    DrawBasicWall(screen, sector, renderableWall);

                if (wallDrawn && wall.IsPortal)
                {
                    neightbors.Add(renderableWall);
                }
            }

            return neightbors;
        }

        private void Meh(Span<BGRA> screen, SectorSprites sectorSprites)
        {
            var floorEnd = sectorSprites.FloorEnd;
            var ceilingStart = sectorSprites.CeilingStart;

            for (int x = 0; x < PixelWidth; x++)
            {
                int floor = floorEnd[x];
                int ceiling = ceilingStart[x];

                Render(screen, ceiling, x, BGRA.Green);
                Render(screen, floor, x, BGRA.White);
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
                // don't render this wall, as its not within the window or is fully obscured by other walls
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
                renderedAreaX.WallStart = wallStartYInt;
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
