using RenderingEngine.Tooling;
using SoftwareRendererModels;

namespace RenderingEngine.Engine
{
    internal sealed unsafe partial class PortalRenderer
    {
        public readonly int PixelWidth;
        public readonly int PixelHeight;
        public required RenderableSprite[] Sprites { get; set; }
        public required PlayerLocation Player { get; set; }
        public required RenderableSector[] Sectors { get; set; }

        public void* Buffer => buffer;

        private readonly WallHelper WallHelper;
        private readonly SpriteHelper SpriteHelper;

        private readonly RenderWindowHelper RenderWindowHelper;

        private PortalPlayerSnapshot? Snapshot = null;

        private readonly AlignedMemoryPool memoryPool;

        // BGRA screen buffer
        private readonly void* buffer;

        public PortalRenderer(int width, int height)
        {
            PixelWidth = width;
            PixelHeight = height;
            SpriteHelper = new SpriteHelper(width, height);
            WallHelper = new WallHelper(width, height);

            memoryPool = AlignedMemoryPool.GeneratePool(width, (int)MemoryPoolBucket.Buffer + height + 1);
            RenderWindowHelper = new RenderWindowHelper(width, height, memoryPool);
            buffer = memoryPool.GetBucketPtr(MemoryPoolBucket.Buffer);

            spriteCacheMemoryPool = DynamicAlignedMemoryPool.GeneratePool(width, (int)SpriteCachePoolBucket.RenderStatus + 1);

            GenerateAngleCache();
            GenerateCache();
        }

        /// <summary>
        /// Precache Angle for Skybox Rendering
        /// </summary>
        private void GenerateAngleCache()
        {
            Span<float> angleCache = memoryPool.GetBucket<float>(MemoryPoolBucket.AngleCache);

            int width = angleCache.Length;

            float cameraWidthIncr = 2.0f / width;
            float cameraRay = -EngineConstants.CameraPlaneX;

            for (int x = 0; x < width; x++, cameraRay += cameraWidthIncr)
            {
                angleCache[x] = MathF.Atan(cameraRay);
            }
        }

        private void GenerateCache()
        {
            Span<float> xMapPosMultiplierCache = memoryPool.GetBucket<float>(MemoryPoolBucket.XMapPosMultiplierCache);
            Span<float> cameraHeightToMapYPos = memoryPool.GetBucket<float>(MemoryPoolBucket.CameraHeightToMapYPos);

            int width = this.PixelWidth;
            int height = this.PixelHeight;
            int halfHeightInt = this.PixelHeight / 2;

            for (int y = 0; y < height; y++)
            {
                int lower = halfHeightInt - y;
                if (lower == 0)
                {
                    lower = 1;
                }

                cameraHeightToMapYPos[y] = height / (float)lower;
            }

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);
            int widthDiv2 = width / 2;

            for (int x = 0; x < width; x++)
            {
                xMapPosMultiplierCache[x] = (widthDiv2 - x) * xPosIncr;
            }
        }

        // avoid re-allocating lists to reduce memory pressure

        private readonly List<RenderableSpriteSnapshot> transparentWalls = [];
        private readonly List<NeighborsToRender> sectorRenderQueue = [];
        private readonly List<RenderablePortalWall> neightbors = [];
        private readonly List<RenderablePortalWall> renderableWalls = [];
        private readonly HashSet<int> renderedSectors = [];
        private readonly HashSet<int> mirroredSectors = [];

        public void DrawScreen(PortalPlayerSnapshot player)
        {
            FillDepthZero();

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
                // 1. Render all sectors at current depth and calculate new z buffer and render window
                List<RenderablePortalWall> neighborsForDepth = DrawScreenStep(player);

                // 2. Cache Distance and Window for Sprite Rendering
                CacheDepthAndStartEndBoundsForSpriteRendering(renderDepth);

                // 3. We render sprites after all the walls were rendered
                var renderedSectorsCopy = new HashSet<int>(this.renderedSectors);
                var spriteSnapShot = new RenderWindowSpriteSnapshot
                {
                    Depth = renderDepth,
                    XLeft = 0,
                    XRight = PixelWidth,
                    RenderDepth = renderDepth,
                    RenderedSectors = renderedSectorsCopy
                };
                transparentWalls.Add(spriteSnapShot);

                // 4. We render transparent walls after all the walls were rendered
                foreach (RenderablePortalWall renderableWall in neighborsForDepth)
                {
                    Debug.Assert(renderableWall.Wall.Neighbor != null);

                    _ = renderedSectors.Add(renderableWall.Wall.Neighbor.Value);

                    // Keep track of mirrored wall for sprite rendering later on
                    if (renderableWall.MirrorWall is not null)
                    {
                        spriteSnapShot.MirroredWalls ??= [];
                        _ = spriteSnapShot.MirroredWalls.Add(renderableWall.MirrorWall);

                        Debug.Assert(renderableWall.MirrorWall.Neighbor != null);

                        _ = mirroredSectors.Add(renderableWall.MirrorWall.Neighbor.Value);
                        _ = mirroredSectors.Add(renderableWall.Wall.Neighbor.Value);
                    }

                    if (renderableWall.IsPortalWithMiddleTexture)
                    {
                        transparentWalls.Add(new RenderWindowWallSnapshot
                        {
                            Depth = renderDepth,
                            Offset = renderableWall.Offset,
                            XLeft = renderableWall.XLeft,
                            XRight = renderableWall.XRight,
                            Wall = renderableWall.Wall
                        });
                    }
                }

                // 5. Enqueue all portal walls for next depth
                foreach (RenderablePortalWall renderableWall in neighborsForDepth)
                {
                    RenderableWall neighbor = renderableWall.Wall;

                    Debug.Assert(neighbor.Neighbor != null);

                    var neighborToRender = new NeighborsToRender(renderableWall, renderableWall.ParentWalls!)
                    {
                        SectorId = neighbor.Neighbor.Value,
                        MirrorWall = neighbor.IsMirror ? neighbor : renderableWall.MirrorWall
                    };

                    sectorRenderQueue.Add(neighborToRender);
                }

                if (NewDepth() == RenderColumnStatus.FinishedRendering)
                {
                    break;
                }
            }
            while (sectorRenderQueue.Count > 0 && ++renderDepth < EngineConstants.MaxRenderDepth);

            RenderSpritesAndTransparentWalls(player);

            transparentWalls.Clear();
            sectorRenderQueue.Clear();
            renderedSectors.Clear();
            mirroredSectors.Clear();
        }

        /// <summary>
        /// Draw all current sectors (one wall at a time) and return the next set of portal walls to drawn
        /// </summary>
        /// <param name="player">Player information</param>
        /// <returns>Set of portal walls to render nexts</returns>
        public List<RenderablePortalWall> DrawScreenStep(PortalPlayerSnapshot player)
        {
            ReadOnlySpan<RenderableSector> sectors = Sectors;

            List<RenderablePortalWall> neighborsForDepth = [];

            // 0. Dequeue next sector to render. All sectors in the queue are for the current depth.
            Span<NeighborsToRender> renderQueueSpan = CollectionsMarshal.AsSpan(sectorRenderQueue);
            for (int s = 0; s < renderQueueSpan.Length; s++)
            {
                NeighborsToRender sectorInfo = renderQueueSpan[s];

                RenderableSector sector = sectors[sectorInfo.SectorId];
                RenderableWall[] parentWalls = sectorInfo.ParentWalls;

                // 1. Filter out walls outside the player's view and sort them closest to furthest
                Span<RenderableWall> walls = WallHelper.DetermineWallsToRender(sector, parentWalls, sectorInfo, player);
                WallHelper.CalculateConnectingSectorsForSlope(player, sectors, sector);

                // 2. Determine where ceiling, floor, and walls start and end
                RenderColumnStatus sectorStatus = CalculateRenderWindow(sectorInfo, sectors, sector, walls);

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
                    neighborsSpan[i].MirrorWall = sectorInfo.MirrorWall;
                }

                neighborsForDepth.AddRange(neighbors);
            }

            sectorRenderQueue.Clear();

            return neighborsForDepth;
        }

        public void RenderSpritesAndTransparentWalls(PortalPlayerSnapshot player)
        {
            ReadOnlySpan<RenderableSector> sectors = Sectors;

            Span<float> depthBuffer = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance);
            Span<RenderableSprite> playerVisibleSprites = SpriteHelper.GetSpritesForPlayer(player, Sprites, Sectors);

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
                    int renderDepth = sectorSprites.RenderDepth;
                    Span<float> currentDistance = depthBuffer[(PixelWidth * renderDepth)..];
                    Span<float> nextDistance = (--renderDepth) >= 0 ? depthBuffer[(PixelWidth * renderDepth)..] : default;

                    List<RenderableSprite> sprites = SpriteHelper.FilterOutSpritesOutsideDepth(playerVisibleSprites,
                        renderedSectors, currentDistance, nextDistance);

                    sprites.AddRange(SpriteHelper.GetMirroredSprites(player, Sprites, Sectors, mirroredSectors, sectorSprites));

                    foreach (RenderableSprite s in sprites)
                    {
                        DrawSprite(player, sectors, s, sectorSprites);
                    }
                }
            }
        }

        private RenderColumnStatus CalculateRenderWindow(
            NeighborsToRender sectorInfo,
            ReadOnlySpan<RenderableSector> sectors,
            RenderableSector sector,
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

                RenderColumnStatus status = CalculateRenderWindow(wall, sector, sectors, renderableWalls);
                sectorStatus |= status;
            }

            return sectorStatus & RenderColumnStatus.NewRender;

        }

        private List<RenderablePortalWall> RenderSector(
            PortalPlayerSnapshot player,
            RenderableSector sector,
            ReadOnlySpan<RenderableSector> sectors,
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

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            if (sectorStatus.HasFlag(RenderColumnStatus.CanRenderWall))
            {
                for (int s = 0; s < renderableWalls.Count; s++)
                {
                    RenderablePortalWall renderableWall = renderableWalls[s];

                    if (renderableWall.RenderColumnStatus.HasFlag(RenderColumnStatus.CanRenderWall))
                    {
                        RenderableWall wall = renderableWall.Wall;

                        bool wallDrawn = wall.IsPortal ?
                            DrawPortalWall(player, sectors, renderableWall) :
                            DrawBasicWall(player, renderableWall);

                        if (wallDrawn && wall.IsPortal)
                        {
                            neightbors.Add(renderableWall);
                        }
                    }
                }
            }

            CalculateNewFloorCeiling(sectorFromX, sectorToX);

            return neightbors;
        }


        private RenderColumnStatus CalculateRenderWindow(
            RenderableWall wall,
            RenderableSector sector,
            ReadOnlySpan<RenderableSector> sectors,
            List<RenderablePortalWall> renderableWalls)
        {
            if (!RenderWindowHelper.SetWallToCalculate(wall))
            {
                // don't render this wall, as its not within the window or is fully obscured by other walls
                return default;
            }

            Span<RenderColumnStatus> renderStatus = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);

            Span<int> portalFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFrom);
            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);
            Span<int> portalFromClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFromClamped);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);


            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            (int offset, int wallFromX, int wallToX) = RenderWindowHelper.GetWallRenderWindowX();

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo2(sectors, wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;
            float wallStartYClamped = yPlaneInfo.WallStartYSloped;
            float ceilDistIncrSloped = yPlaneInfo.CeilDistIncrSloped;
            float wallEndYClamped = yPlaneInfo.WallEndYSloped;
            float floorDistIncrSloped = yPlaneInfo.FloorDistIncrSloped;
            float? portalStartY = yPlaneInfo.PortalStartY;
            float? portalEndY = yPlaneInfo.PortalEndY;
            float? portalStartIncr = yPlaneInfo.PortalStartIncr;
            float? portalEndIncr = yPlaneInfo.PortalEndIncr;

            Debug.Assert(wall.IsPortal ? wall.Neighbor != null : wall.Neighbor == null);

            RenderableSector? neighborSector = wall.IsPortal ? sectors[wall.Neighbor!.Value] : null;
            bool sloped = neighborSector is not null && (sector.Settings.Sloped || neighborSector.Settings.Sloped);

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
                RenderColumnStatus columnStatus = renderStatus[x];

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
                    wallStartYClamped += ceilDistIncrSloped;
                    wallEndYClamped += floorDistIncrSloped;
                    wallStatus |= status;
                    status = default;

                    if (sloped)
                    {
                        portalStartY += portalStartIncr;
                        portalEndY += portalEndIncr;
                    }

                    continue;
                }

                if (sloped || wall.IsPortal)
                {
                    (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, wall);

                    float pixelsPerHeight = (wallEndY - wallStartY) / sectorHeight;

                    float ceilPixelOffset = pixelsPerHeight * ceilOffset;
                    float floorPixelOffset = pixelsPerHeight * floorOffset;
                    float portalToY = wallEndY - floorPixelOffset;
                    float portalFromY = wallStartY - ceilPixelOffset;


                    portalFrom[x] = portalFromClamped[x] = float.ConvertToIntegerNative<int>(portalFromY);
                    portalTo[x] = portalToClamped[x] = float.ConvertToIntegerNative<int>(portalToY);

                    if (sloped)
                    {
                        portalFromClamped[x] = float.ConvertToIntegerNative<int>(portalStartY!.Value);
                        portalToClamped[x] = float.ConvertToIntegerNative<int>(portalEndY!.Value);
                    }

                }

                int wallStartYClampedInt = float.ConvertToIntegerNative<int>(wallStartYClamped);
                int wallEndYClampedInt = float.ConvertToIntegerNative<int>(wallEndYClamped);

                wallStartClamped[x] = upperWallIsSkybox ? wallEndYClampedInt : wallStartYClampedInt;
                wallEndClamped[x] = wallEndYClampedInt;

                status |= RecalculateRenderWindow(x, true, renderStatus, ceilingStart, floorEnd, wallStartClamped, wallEndClamped);

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
                wallStartYClamped += ceilDistIncrSloped;
                wallEndYClamped += floorDistIncrSloped;

                if (sloped)
                {
                    portalStartY += portalStartIncr;
                    portalEndY += portalEndIncr;
                }
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
        public void* DrawFrame(PortalPlayerSnapshot snapShot)
        {
            Snapshot = snapShot;

            DrawScreen(snapShot);

            return this.buffer;
        }
    }
}