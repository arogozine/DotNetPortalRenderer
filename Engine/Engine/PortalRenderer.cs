using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        public readonly int PixelWidth;
        public readonly int PixelHeight;
        public required RenderableSprite[] Sprites { get; set; }
        public required Player Player { get; set; }
        public required Sector[] Sectors { get; set; }

        private readonly WallHelper WallHelper;
        private readonly SpriteHelper SpriteHelper;

        private readonly RenderWindowHelper RenderWindowHelper;

        private PortalPlayerSnapshot? Snapshot = null;

        private readonly float[] angleCache;
        private readonly int[] incrVectorCache;

        private readonly BGRA[] buffer;

        public PortalRenderer(int width, int height)
        {
            int overflowBuffer = (Vector<float>.Count - width % Vector<float>.Count) + Vector<float>.Count;

            PixelWidth = width;
            PixelHeight = height;
            SpriteHelper = new SpriteHelper(width, height);
            WallHelper = new WallHelper(width, height);
            buffer = GC.AllocateUninitializedArray<BGRA>(width * height);
            angleCache = new float[width + overflowBuffer];
            incrVectorCache = new int[width + overflowBuffer];

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

            float cameraWidthIncr = 2.0f / width;
            float cameraRay = -EngineConstants.CameraPlaneX;

            for (int x = 0; x < width; x++, cameraRay += cameraWidthIncr)
            {
                angleCache[x] = MathF.Atan(cameraRay);
            }
        }

        private void GenerateCache()
        {
            Span<int> incrVectorCache = SharedHelpers.AlignSpan(this.incrVectorCache);

            int width = this.PixelWidth;
            int halfHeightInt = this.PixelHeight / 2;
            float div = this.PixelHeight * (1 << 8);

            for (int x = 0; x < width; x++)
            {
                int upper = halfHeightInt - x;
                incrVectorCache[x] = float.ConvertToIntegerNative<int>(div / upper);
            }
        }

        private readonly RenderableAreaAndZBuffer[] spriteRenderableAreaCache = new RenderableAreaAndZBuffer[EngineConstants.MaxRenderDepth];

        // avoid re-allocating lists to reduce memory pressure

        private readonly List<RenderableSpriteSnapshot> transparentWalls = [];
        private readonly List<NeighborsToRender> sectorRenderQueue = [];
        private readonly List<RenderablePortalWall> neightbors = [];
        private readonly List<RenderablePortalWall> renderableWalls = [];
        private readonly HashSet<int> renderedSectors = [];

        public void DrawScreen(PortalPlayerSnapshot player)
        {
            InitializeSharedVectors(player);
            this.WallHelper.SetSnapShot(player);

            RenderWindowHelper.NewRender();

            sectorRenderQueue.Add(new NeighborsToRender
            {
                SectorId = player.Sector
            });

            _ = renderedSectors.Add(player.Sector);

            int renderDepth = 0;

            do
            {
                // 0. Cache current renderable area for sprite rendering
                int[] ceilingStart, floorEnd, wallEnd;
                float[] zBuffer;
                RenderColumnStatus[] columnStatus;

                if (spriteRenderableAreaCache[renderDepth] is RenderableAreaAndZBuffer spriteCache)
                {
                    ceilingStart = spriteCache.CeilingStart;
                    floorEnd = spriteCache.FloorEnd;
                    zBuffer = spriteCache.ZBuffer;
                    columnStatus = spriteCache.ColumnStatus;
                    wallEnd = spriteCache.WallEnd;
                }
                else
                {
                    ceilingStart = new int[PixelWidth];
                    floorEnd = new int[PixelWidth];
                    zBuffer = new float[PixelWidth];
                    columnStatus = new RenderColumnStatus[PixelWidth];
                    wallEnd = new int[PixelWidth];
                    spriteRenderableAreaCache[renderDepth] = new RenderableAreaAndZBuffer(ceilingStart, floorEnd, wallEnd, zBuffer, columnStatus);
                }

                // 1. Copy over the renderable area for sprite rendering
                RenderWindowHelper.CeilingStart.AsSpan().CopyTo(ceilingStart);
                RenderWindowHelper.FloorEnd.AsSpan().CopyTo(floorEnd);
                RenderWindowHelper.Status.AsSpan().CopyTo(columnStatus);

                // 2. Render all sectors at current depth and calculate new z buffer and render window
                List<RenderablePortalWall> neighborsForDepth = DrawScreenStep(player);

                // 3. Cache z-buffer for sprite rendering
                RenderWindowHelper.Distance.AsSpan().CopyTo(zBuffer);
                RenderWindowHelper.FloorEnd.AsSpan().CopyTo(wallEnd);

                // 4. We render sprites after all the walls were rendered
                transparentWalls.Add(new RenderWindowSpriteSnapshot()
                {
                    XLeft = 0,
                    XRight = PixelWidth,
                    CeilingStart = ceilingStart,
                    FloorEnd = floorEnd,
                    Distance = zBuffer,
                    WallEnd = wallEnd,
                    RenderDepth = renderDepth
                });

                // 5. We render transparent walls after all the walls were rendered
                foreach (RenderablePortalWall renderableWall in neighborsForDepth)
                {
                    _ = renderedSectors.Add(renderableWall.Wall.Neighbor);
                    if (renderableWall.IsPortalWithMiddleTexture)
                    {
                        transparentWalls.Add(new RenderWindowWallSnapshot
                        {
                            Offset = renderableWall.Offset,
                            XLeft = renderableWall.XLeft,
                            XRight = renderableWall.XRight,
                            Wall = renderableWall.Wall,
                            CeilingStart = ceilingStart,
                            ColumnStatus = columnStatus,
                            Distance = zBuffer,
                            WallEnd = wallEnd,
                            FloorEnd = floorEnd
                        });
                    }
                }

                // 7. Enqueue all portal walls for next depth
                foreach (RenderablePortalWall renderableWall in neighborsForDepth)
                {
                    RenderableWall neighbor = renderableWall.Wall;

                    var neighborToRender = new NeighborsToRender(renderableWall, renderableWall.ParentWalls!)
                    {
                        SectorId = neighbor.Neighbor
                    };

                    sectorRenderQueue.Add(neighborToRender);
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
            renderedSectors.Clear();
        }

        /// <summary>
        /// Draw all current sectors (one wall at a time) and return the next set of portal walls to drawn
        /// </summary>
        /// <param name="player">Player information</param>
        /// <returns>Set of portal walls to render nexts</returns>
        public List<RenderablePortalWall> DrawScreenStep(PortalPlayerSnapshot player)
        {
            ReadOnlySpan<Sector> sectors = Sectors;

            List<RenderablePortalWall> neighborsForDepth = [];

            // 0. Dequeue next sector to render. All sectors in the queue are for the current depth.
            Span<NeighborsToRender> renderQueueSpan = CollectionsMarshal.AsSpan(sectorRenderQueue);
            for (int s = 0; s < renderQueueSpan.Length; s++)
            {
                NeighborsToRender sectorInfo = renderQueueSpan[s];

                Sector sector = sectors[sectorInfo.SectorId];
                RenderableWall[] parentWalls = sectorInfo.ParentWalls;

                // 1. Filter out walls outside the player's view and sort them closest to furthest
                Span<RenderableWall> walls = WallHelper.DetermineWallsToRender(sector, parentWalls, sectorInfo, player);

                // 2. Determine where ceiling, floor, and walls start and end
                RenderColumnStatus sectorStatus = CalculateRenderWindow(sectorInfo, sector, walls);

                // 3. Nothing to render, bail early
                if (sectorStatus == default || renderableWalls.Count == 0)
                {
                    continue;
                }

                // 4. Render Floors, Ceilings, and Walls
                List<RenderablePortalWall> neighbors = RenderSector(player, sector, sectors, sectorStatus);

                // 5. Keep track of parent walls to avoid rendering them again
                Span<RenderablePortalWall> neighborsSpan = CollectionsMarshal.AsSpan(neighbors);
                for (int i = 0; i < neighborsSpan.Length; i++)
                {
                    neighborsSpan[i].ParentWalls = parentWalls;
                }

                neighborsForDepth.AddRange(neighbors);
            }

            sectorRenderQueue.Clear();

            return neighborsForDepth;
        }

        public void RenderSpritesAndTransparentWalls(PortalPlayerSnapshot player)
        {
            ReadOnlySpan<Sector> sectors = Sectors;

            Span<RenderableSprite> playerVisibleSprites = SpriteHelper.GetSpritesForPlayer(player, Sprites, sectors);

            // render transparent walls and sprites
            Span<RenderableSpriteSnapshot> transparentWallsSpan = CollectionsMarshal.AsSpan(transparentWalls);
            for (int i = transparentWallsSpan.Length - 1; i >= 0; i--)
            {
                RenderableSpriteSnapshot renderableWall = transparentWallsSpan[i];

                if (renderableWall is RenderWindowWallSnapshot transparentWall)
                {
                    DrawTransparentWall(sectors, transparentWall);
                }
                else if (renderableWall is RenderWindowSpriteSnapshot sectorSprites)
                {
                    // filter sprites based on depth between this and next set of sectors
                    float[] currentDistance = sectorSprites.Distance;
                    float[]? nextDistance = sectorSprites.RenderDepth > 1 ? spriteRenderableAreaCache[sectorSprites.RenderDepth - 1].ZBuffer : null;

                    List<RenderableSprite> sprites = SpriteHelper.FilterOutSpritesOutsideDepth(playerVisibleSprites,
                        renderedSectors, currentDistance, nextDistance);

                    foreach (RenderableSprite s in sprites)
                    {
                        DrawSprite(player, sectors, s, sectorSprites);
                    }
                }
            }
        }

        private RenderColumnStatus CalculateRenderWindow(
            NeighborsToRender sectorInfo,
            Sector sector,
            Span<RenderableWall> walls)
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
                RenderableWall wall = walls[s];

                RenderColumnStatus status = CalculateRenderWindow(wall, sector, renderableWalls);
                sectorStatus |= status;
            }

            return sectorStatus & RenderColumnStatus.NewRender;
        }

        private List<RenderablePortalWall> RenderSector(
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
                    RenderablePortalWall renderableWall = renderableWalls[s];

                    if (renderableWall.RenderColumnStatus.HasFlag(RenderColumnStatus.CanRenderWall))
                    {
                        RenderableWall wall = renderableWall.Wall;

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


        private RenderColumnStatus CalculateRenderWindow(RenderableWall wall, Sector sector, List<RenderablePortalWall> renderableWalls)
        {
            if (!RenderWindowHelper.SetWallToCalculate(wall))
            {
                // don't render this wall, as its not within the window or is fully obscured by other walls
                return default;
            }

            (int offset, int wallFromX, int wallToX) = RenderWindowHelper.GetWallRenderWindowX();

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            // minor performance hack
            bool upperWallIsSkybox = sector.CeilTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox) &&
                !wall.IsPortal && wall.MiddleTexture!.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);

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
                RenderColumnStatus columnStatus = RenderWindowHelper.Status[x];

                // we already have a different wall rendering in front of this one
                if (columnStatus.IsCalculated || columnStatus.IsFinished)
                {
                    if (x - 1 > renderableFromX)
                    {
                        offset = wallFromX > wall.XLeft ? wallFromX - wall.XLeft : 0;

                        renderableWalls.Add(new RenderablePortalWall
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

                RenderWindowHelper.WallStart[x] = upperWallIsSkybox ? wallEndYInt : wallStartYInt;
                RenderWindowHelper.WallEnd[x] = wallEndYInt;

                status |= RenderWindowHelper.RecalculateRenderWindow(x, true);

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }

            wallStatus |= status;

            if (renderableToX > renderableFromX)
            {
                offset = renderableFromX > wall.XLeft ? renderableFromX - wall.XLeft : 0;

                renderableWalls.Add(new RenderablePortalWall {
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
