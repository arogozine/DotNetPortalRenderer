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
        private readonly float[] incrVectorCache;

        private readonly BGRA[] buffer;

        public PortalRenderer(int width, int height)
        {
            int overflowBuffer = (Vector<float>.Count - width % Vector<float>.Count) + Vector<float>.Count;

            PixelWidth = width;
            PixelHeight = height;
            SpriteHelper = new SpriteHelper(width, height, EngineConstants.CameraPlaneX);
            WallHelper = new WallHelper(width, height);
            buffer = GC.AllocateUninitializedArray<BGRA>(width * height);
            angleCache = new float[width + overflowBuffer];
            incrVectorCache = new float[width + overflowBuffer];

            RenderWindowHelper = new RenderWindowHelper(width, height);

            GenerateAngleCache();
            GenerateCache();
        }

        /// <summary>
        /// Precache Angle for Skybox Rendering
        /// </summary>
        private void GenerateAngleCache()
        {
            int width = this.angleCache.Length;

            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;
            float cameraRay = -EngineConstants.CameraPlaneX;

            for (int x = 0; x < width; x++, cameraRay += cameraWidthIncr)
            {
                angleCache[x] = MathF.Atan(cameraRay);
            }
        }

        private void GenerateCache()
        {
            int width = this.PixelWidth;
            int halfHeightInt = this.PixelHeight / 2;
            float oneOverHeight = 1f / PixelHeight;

            for (int x = 0; x < width; x++)
            {
                int upper = (halfHeightInt - x) << 8;
                incrVectorCache[x] = 1f / (upper * oneOverHeight);
            }
        }

        private sealed record RenderableAreaAndZBuffer(int[] CeilingStart, int[] FloorEnd, float[] ZBuffer);
        private readonly RenderableAreaAndZBuffer[] spriteRenderableAreaCache = new RenderableAreaAndZBuffer[EngineConstants.MaxRenderDepth];

        private readonly List<RenderableSprite> transparentWalls = [];
        private readonly Queue<NeighborsToRender> sectorRenderQueue = [];

        public void DrawScreen(PortalPlayerSnapshot player)
        {
            InitializeSharedVectors(player);
            this.WallHelper.SetSnapShot(player);

            RenderWindowHelper.NewRender();

            sectorRenderQueue.Enqueue(new NeighborsToRender
            {
                SectorId = player.Sector
            });

            int renderDepth = 0;

            do
            {
                // 0. Cache current renderable area for sprite rendering
                int[] ceilingStart;
                int[] floorEnd;
                float[] zBuffer;

                if (spriteRenderableAreaCache[renderDepth] is RenderableAreaAndZBuffer spriteCache)
                {
                    ceilingStart = spriteCache.CeilingStart;
                    floorEnd = spriteCache.FloorEnd;
                    zBuffer = spriteCache.ZBuffer;
                }
                else
                {
                    ceilingStart = new int[PixelWidth];
                    floorEnd = new int[PixelWidth];
                    zBuffer = new float[PixelWidth];
                    spriteRenderableAreaCache[renderDepth] = new RenderableAreaAndZBuffer(ceilingStart, floorEnd, zBuffer);
                }

                // 1. Copy over the renderable area for sprite rendering
                for (int i = 0; i < RenderWindowHelper.RenderWindow.Length; i++)
                {
                    ref RenderWindow from = ref RenderWindowHelper.RenderWindow[i];
                    ceilingStart[i] = from.CeilingStart;
                    floorEnd[i] = from.FloorEnd;
                }

                // 2. Render all sectors at current depth and calculate new z buffer and render window
                List<RenderableWall> neighborsForDepth = DrawScreenStep(player);

                // 3. Cache z-buffer for sprite rendering
                for (int i = 0; i < RenderWindowHelper.RenderWindow.Length; i++)
                {
                    ref RenderWindow from = ref RenderWindowHelper.RenderWindow[i];
                    zBuffer[i] = from.Distance;
                }

                // 4. We render sprites after all the walls were rendered
                transparentWalls.Add(new SectorSprites()
                {
                    XLeft = 0,
                    XRight = PixelWidth,
                    CeilingStart = ceilingStart,
                    FloorEnd = floorEnd,
                    Distance = zBuffer,
                    RenderDepth = renderDepth
                });

                // 5. We render transparent walls after all the walls were rendered
                RenderWindow[]? renderableArea = null;
                foreach (RenderableWall renderableWall in neighborsForDepth)
                {
                    if (renderableWall.IsPortalWithMiddleTexture)
                    {
                        renderableArea ??= RenderWindowHelper.CopyRenderWindow(false);
                        renderableWall.RenderWindow = renderableArea;
                        transparentWalls.Add(new TransparentWall
                        {
                            Offset = renderableWall.Offset,
                            XLeft = renderableWall.XLeft,
                            XRight = renderableWall.XRight,
                            RenderWindow = renderableArea,
                            Wall = renderableWall.Wall
                        });
                    }
                }

                // 7. Enqueue all portal walls for next depth
                foreach (RenderableWall renderableWall in neighborsForDepth)
                {
                    Wall neighbor = renderableWall.Wall;

                    var neighborToRender = new NeighborsToRender(renderableWall, renderableWall.ParentWalls!)
                    {
                        SectorId = neighbor.Neighbor
                    };

                    sectorRenderQueue.Enqueue(neighborToRender);
                }

                if (RenderWindowHelper.NewDepth() == RenderColumnStatus.FinishedRendering)
                {
                    break;
                }
            }
            while (sectorRenderQueue.Count > 0 && ++renderDepth < EngineConstants.MaxRenderDepth);

            RenderSpritesAndTransparentWalls(player);

            transparentWalls.Clear();
            sectorRenderQueue.Clear();
        }

        /// <summary>
        /// Draw all current sectors (one wall at a time) and return the next set of portal walls to drawn
        /// </summary>
        /// <param name="player">Player information</param>
        /// <returns>Set of portal walls to render nexts</returns>
        public List<RenderableWall> DrawScreenStep(PortalPlayerSnapshot player)
        {
            ReadOnlySpan<Sector> sectors = Sectors;

            List<RenderableWall> neighborsForDepth = [];

            // 0. Dequeue next sector to render. All sectors in the queue are for the current depth.
            while (sectorRenderQueue.TryDequeue(out NeighborsToRender? sectorInfo))
            {
                Sector sector = sectors[sectorInfo.SectorId];
                Wall[] parentWalls = sectorInfo.ParentWalls;

                // 1. Filter out walls outside the player's view and sort them closest to furthest
                Span<Wall> walls = WallHelper.DetermineWallsToRender(sector, parentWalls, player);

                // 2. Determine where ceiling, floor, and walls start and end
                RenderColumnStatus sectorStatus = CalculateRenderWindow(sectorInfo, sector, walls);

                if (sectorStatus == default || renderableWalls.Count == 0)
                {
                    continue;
                }

                // 3. Render Floors, Ceilings, and Walls
                List<RenderableWall> neighbors = RenderSector(player, sector, sectors, sectorStatus);

                // 4. Keep track of parent walls to avoid rendering them again
                Span<RenderableWall> neighborsSpan = CollectionsMarshal.AsSpan(neighbors);
                for (int i = 0; i < neighborsSpan.Length; i++)
                {
                    neighborsSpan[i].ParentWalls = parentWalls;
                }

                neighborsForDepth.AddRange(neighbors);
            }

            return neighborsForDepth;
        }

        public void RenderSpritesAndTransparentWalls(PortalPlayerSnapshot player)
        {
            ReadOnlySpan<Sector> sectors = Sectors;

            Span<Sprite> playerVisibleSprites = SpriteHelper.GetSpritesForPlayer(player, Sprites, sectors);

            // render transparent walls and sprites
            Span<RenderableSprite> transparentWallsSpan = CollectionsMarshal.AsSpan(transparentWalls);
            for (int i = transparentWallsSpan.Length - 1; i >= 0; i--)
            {
                RenderableSprite renderableWall = transparentWallsSpan[i];

                if (renderableWall is TransparentWall transparentWall)
                {
                    DrawTransparentWall(sectors, transparentWall);
                }
                else if (renderableWall is SectorSprites sectorSprites)
                {
                    // filter sprites based on depth between this and next set of sectors
                    float[] currentDistance = sectorSprites.Distance;
                    float[]? nextDistance = sectorSprites.RenderDepth > 1 ? spriteRenderableAreaCache[sectorSprites.RenderDepth - 1].ZBuffer : null;

                    List<Sprite> sprites = this.SpriteHelper.FilterOutSpritesOutsideDepth(playerVisibleSprites, currentDistance, nextDistance);

                    foreach (Sprite s in sprites)
                    {
                        DrawSprite(sectors, s, sectorSprites);
                    }
                }
            }
        }

        private readonly List<RenderableWall> neightbors = [];
        private readonly List<RenderableWall> renderableWalls = [];

        private RenderColumnStatus CalculateRenderWindow(
            NeighborsToRender sectorInfo,
            Sector sector,
            Span<Wall> walls)
        {
            neightbors.Clear();
            renderableWalls.Clear();

            if (sector.Floor == sector.Ceil)
            {
                return default;
            }

            RenderColumnStatus sectorStatus = default;

            RenderWindowHelper.NewSector(sectorInfo);

            for (int s = 0; s < walls.Length; s++)
            {
                Wall wall = walls[s];

                RenderColumnStatus status = CalculateRenderWindow(wall, sector, renderableWalls);
                sectorStatus |= status;
            }

            return sectorStatus;
        }

        private List<RenderableWall> RenderSector(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            RenderColumnStatus sectorStatus)
        {
            if (sectorStatus.HasFlag(RenderColumnStatus.CanRenderFloor))
            {
                if (sector.FloorTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox))
                {
                    RenderSkyboxFloorVector(player, sector);
                }
                else
                {
                    RenderFloorVector(player, sector);
                }
            }

            if (sectorStatus.HasFlag(RenderColumnStatus.CanRenderCeiling))
            {
                if (sector.CeilTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox))
                {
                    RenderSkyboxVector(player, sector);
                }
                else
                {
                    RenderCeilingVector(player, sector);
                }
            }

            if (sectorStatus.HasFlag(RenderColumnStatus.CanRenderWall))
            {
                for (int s = 0; s < renderableWalls.Count; s++)
                {
                    RenderableWall renderableWall = renderableWalls[s];

                    if (renderableWall.RenderColumnStatus.HasFlag(RenderColumnStatus.CanRenderWall))
                    {
                        Wall wall = renderableWall.Wall;

                        bool wallDrawn = wall.IsPortal ?
                            DrawPortalWall(player, sector, sectors, renderableWall) :
                            DrawBasicWall(player, sector, renderableWall);

                        if (wallDrawn && wall.IsPortal)
                        {
                            neightbors.Add(renderableWall);
                        }
                    }
                }
            }

            return neightbors;
        }


        private RenderColumnStatus CalculateRenderWindow(Wall wall, Sector sector, List<RenderableWall> renderableWalls)
        {
            Span<RenderWindow> renderedArea = RenderWindowHelper.RenderWindow;

            if (!RenderWindowHelper.SetWallToCalculate(wall))
            {
                // don't render this wall, as its not within the window or is fully obscured by other walls
                return default;
            }

            (int offset, int wallFromX, int wallToX) = RenderWindowHelper.GetWallRenderWindowX();

            WallYPlaneInfo yPlaneInfo = CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            bool upperWallIsSkybox = sector.CeilTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox) &&
                !wall.IsPortal && wall.Line.MiddleTexture!.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);

            if (wallToX <= wallFromX)
            {
                return default;
            }

            RenderColumnStatus wallStatus = default;
            RenderColumnStatus status = default;

            int renderableFromX = wallFromX;
            int renderableToX = wallToX;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                ref RenderWindow renderedAreaX = ref renderedArea[x];

                // we already have a different wall rendering in front of this one
                if (renderedAreaX.Calculated || renderedAreaX.Finished)
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
                            RenderColumnStatus = status
                        });
                    }

                    renderableFromX = x;
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    wallStatus |= status;
                    status = default;

                    continue;
                }

                int wallStartYInt = float.ConvertToIntegerNative<int>(wallStartY);
                int wallEndYInt = float.ConvertToIntegerNative<int>(wallEndY);

                renderedAreaX.WallStart = upperWallIsSkybox ? wallEndYInt : wallStartYInt;
                renderedAreaX.WallEnd = wallEndYInt;

                RenderWindowHelper.RecalculateRenderWindow(ref renderedAreaX, true);
                status |= renderedAreaX.Status;

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }

            wallStatus |= status;

            if (renderableToX > renderableFromX)
            {
                offset = renderableFromX > wall.XLeft ? renderableFromX - wall.XLeft : 0;

                renderableWalls.Add(new RenderableWall {
                    Wall = wall,
                    XLeft = renderableFromX,
                    XRight = renderableToX,
                    Offset = offset,
                    RenderColumnStatus = status
                });
            }

            return wallStatus;
        }

        [MemberNotNull(nameof(Snapshot))]
        public BGRA[] DrawFrame(PortalPlayerSnapshot snapShot)
        {
            Snapshot = snapShot;

            DrawScreen(snapShot);

            return buffer;
        }
    }
}
