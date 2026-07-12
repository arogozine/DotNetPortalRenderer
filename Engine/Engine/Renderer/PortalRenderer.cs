using RenderingEngine.Tooling;
using SoftwareRendererModels;
using Tooling;

namespace RenderingEngine.Engine
{
    internal unsafe partial class PortalRenderer
    {
        protected readonly int PixelWidth;
        protected readonly int PixelHeight;
        public required RenderableSprite[] Sprites { get; init; }
        public required RenderableSector[] Sectors { get; init; }

        public void* Buffer { get; }

        private readonly WallHelper WallHelper;
        private readonly WallHelper WallHelperB;
        private readonly SpriteHelper SpriteHelper;

        // AI Assisted: single frame counter shared by both WallHelper instances so per-wall memoization
        // (RenderableWall.LastComputedFrame) stays meaningful regardless of which thread touched a wall last.
        private int frame = -1;

        private readonly RenderThreadState threadStateA;
        private readonly RenderThreadState threadStateB;

        private PortalPlayerSnapshot? Snapshot;

        protected readonly AlignedMemoryPool memoryPool;

        protected PortalRenderer(int width, int height, CancellationToken cancellationToken)
        {
            PixelWidth = width;
            PixelHeight = height;
            SpriteHelper = new SpriteHelper(width, height);
            WallHelper = new WallHelper(width, height);
            WallHelperB = new WallHelper(width, height);

            threadStateA = new RenderThreadState { WallHelper = WallHelper, UsePrimaryTempBuckets = true };
            threadStateB = new RenderThreadState { WallHelper = WallHelperB, UsePrimaryTempBuckets = false };

            memoryPool = AlignedMemoryPool.GeneratePool(width, (int)MemoryPoolBucket.Buffer + height + 1);
            Buffer = memoryPool.GetBucketPtr(MemoryPoolBucket.Buffer);

            spriteCacheMemoryPool = DynamicAlignedMemoryPool.GeneratePool(width, (int)SpriteCachePoolBucket.RenderStatus + 1);

            GenerateAngleCache();
            GenerateCache();

            mirroredSectors = new HashSet<int>[EngineConstants.MaxRenderDepth];
            for (int i = 0; i < EngineConstants.MaxRenderDepth; i++)
            {
                mirroredSectors[i] = new();
            }

            // AI Assisted: dedicated long-running thread instead of ThreadPool dispatch. RenderSector
            // forks work here up to twice per call (dozens of times per frame); queuing to the shared
            // ThreadPool on every call pays dispatch overhead and contends with other ThreadPool work,
            // whereas a persistent worker just waits on a semaphore and reuses the same OS thread.
            _ = Task.Factory
                .StartNew(() => ConcurrentWorkerLoop(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(static (Task t) =>
                {
                    AsyncLogger.Default.AddLog(LogSeverity.Error, "Concurrent Worker Thread Faulted", t.Exception);
                    Debug.WriteLine(t.Exception);
                    Debugger.Break();
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Precache Angle for Skybox Rendering
        /// </summary>
        private void GenerateAngleCache()
        {
            float* angleCache = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.AngleCache);

            int width = this.PixelWidth;

            float cameraWidthIncr = 2.0f / width;
            float cameraRay = -EngineConstants.CameraPlaneX;

            for (int x = 0; x < width; x++, cameraRay += cameraWidthIncr)
            {
                angleCache[x] = MathF.Atan(cameraRay);
            }
        }

        private void GenerateCache()
        {
            float* xMapPosMultiplierCache = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.XMapPosMultiplierCache);
            float* cameraHeightToMapYPos = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.CameraHeightToMapYPos);

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
        private readonly HashSet<int> renderedSectors = [];
        private readonly HashSet<int>[] mirroredSectors;

        // Scratch state for PartitionSectorQueue's union-find grouping pass. Reused across calls to
        // avoid per-depth allocations; sized/cleared as needed at the start of each partitioning pass.
        private readonly Dictionary<int, int> partitionSectorFirstIndex = [];
        private readonly Dictionary<int, int> partitionSlopeNeighborFirstIndex = [];
        private readonly Dictionary<int, bool> partitionGroupAssignedToB = [];
        private int[] partitionParent = new int[32];

        private void DrawScreen(PortalPlayerSnapshot player)
        {
            FillDepthZero();

            InitializeSharedVectors(player);

            frame++;
            this.WallHelper.SetSnapShot(player);
            this.WallHelperB.SetSnapShot(player);

            NewRender();

            var initialNeighbor = ObjectPool.NeighborsToRender.GetOrCreate();
            initialNeighbor.Initialize(player.Sector);
            sectorRenderQueue.Add(initialNeighbor);

            _ = renderedSectors.Add(player.Sector);

            int renderDepth = 0;

            do
            {
                // 1. Render all sectors at current depth and calculate new z buffer and render window
                List<RenderablePortalWall> neighborsForDepth = DrawScreenStep(player);

                // 2. Cache Distance and Window for Sprite Rendering
                CacheDepthAndStartEndBoundsForSpriteRendering(renderDepth);

                // 3. We render sprites after all the walls were rendered
                var spriteSnapShot = ObjectPool.RenderWindowSpriteSnapshot.GetOrCreate(renderDepth);
                {
                    spriteSnapShot.Depth = renderDepth;
                    spriteSnapShot.XLeft = 0;
                    spriteSnapShot.XRight = PixelWidth;
                    spriteSnapShot.RenderDepth = renderDepth;
                }
                transparentWalls.Add(spriteSnapShot);

                // 4. We render transparent walls after all the walls were rendered
                foreach (RenderablePortalWall renderableWall in neighborsForDepth)
                {
                    Debug.Assert(renderableWall.Wall.Neighbor != null);

                    _ = renderedSectors.Add(renderableWall.Wall.Neighbor.Value);

                    // Keep track of mirrored wall for sprite rendering later on
                    if (renderableWall.MirrorWall is not null)
                    {
                        _ = spriteSnapShot.MirroredWalls.Add(renderableWall.MirrorWall);

                        Debug.Assert(renderableWall.MirrorWall.Neighbor != null);

                        _ = mirroredSectors[renderDepth].Add(renderableWall.MirrorWall.Neighbor.Value);
                        _ = mirroredSectors[renderDepth].Add(renderableWall.Wall.Neighbor.Value);
                    }

                    if (renderableWall.IsPortalWithMiddleTexture)
                    {
                        var snapShot = ObjectPool.RenderWindowWallSnapshot.GetOrCreate();
                        {
                            snapShot.Depth = renderDepth;
                            snapShot.Offset = renderableWall.Offset;
                            snapShot.XLeft = renderableWall.XLeft;
                            snapShot.XRight = renderableWall.XRight;
                            snapShot.Wall = renderableWall.Wall;
                        }

                        transparentWalls.Add(snapShot);
                    }
                }

                // 5. Enqueue all portal walls for next depth
                foreach (RenderablePortalWall renderableWall in neighborsForDepth)
                {
                    RenderableWall neighborWall = renderableWall.Wall;

                    Debug.Assert(neighborWall.Neighbor != null);

                    NeighborsToRender neighborToRender = ObjectPool.NeighborsToRender.GetOrCreate();

                    var pool = WallHelper.RequestWallArray(renderableWall.ParentWalls.Count + 1);
                    renderableWall.ParentWalls.CopyTo(pool);
                    pool.AsSpan()[^1] = renderableWall.Wall;

                    neighborToRender.Initialize(pool, renderableWall, neighborWall.Neighbor.Value);
                    neighborToRender.MirrorWall = neighborWall.IsMirror ? neighborWall : renderableWall.MirrorWall;
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

            for (int i = 0; i < renderDepth; i++)
            {
                mirroredSectors[i].Clear();
            }

            ObjectPool.Clear();
            threadStateA.ClearPoolsForNextFrame();
            threadStateB.ClearPoolsForNextFrame();
        }
        
        private void NewRender()
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            Span<int> wallEndSloped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            Span<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);

            status.Fill(RenderColumnStatus.NewRender);
            floorEnd.Fill(PixelHeight - 1);
            wallEndSloped.Fill(PixelHeight - 1);
            distance.Fill(float.MaxValue);

            memoryPool.ClearBuckets(
                MemoryPoolBucket.PortalFrom, MemoryPoolBucket.PortalFromClamped,
                MemoryPoolBucket.PortalTo, MemoryPoolBucket.PortalToClamped,
                MemoryPoolBucket.CeilingStart,
                MemoryPoolBucket.TextureYIncrement, MemoryPoolBucket.StartingYTexturePosition);
        }

        /// <summary>
        /// Process one queued sector: cull/sort its walls, compute its render window, and draw its
        /// floor/ceiling/walls, appending any resulting portal walls to <paramref name="state"/>'s output list.
        /// Called from both thread A (inline) and thread B (<see cref="ConcurrentWorkerLoop"/>) against their
        /// own <see cref="RenderThreadState"/>, never against each other's.
        /// </summary>
        private void ProcessSectorEntry(
            NeighborsToRender sectorInfo, PortalPlayerSnapshot player,
            ReadOnlySpan<RenderableSector> sectors, RenderThreadState state)
        {
            RenderableSector sector = sectors[sectorInfo.SectorId];
            ReadOnlySpan<RenderableWall> parentWalls = sectorInfo.ParentWalls;

            // 1. Filter out walls outside the player's view and sort them closest to furthest
            Span<RenderableWall> walls = state.WallHelper.DetermineWallsToRender(sector, parentWalls, sectorInfo, player, frame);
            state.WallHelper.CalculateConnectingSectorsForSlope(player, sectors, sector);

            // 2. Determine where ceiling, floor, and walls start and end
            (RenderColumnStatus sectorStatus, int sectorFromX, int sectorToX) =
                CalculateRenderWindow(sectorInfo, sectors, sector, state.RenderableWalls, walls, state.RenderablePortalWallPool);

            // 3. Nothing to render, bail early
            if (sectorStatus == default || state.RenderableWalls.Count == 0)
            {
                return;
            }

            // 4. Render Floors, Ceilings, and Walls
            Span<RenderablePortalWall> neighbors = RenderSector(
                player, sector, sectorFromX, sectorToX, state.RenderableWalls, sectorStatus,
                state.RenderablePortalWallArrayPool, state.UsePrimaryTempBuckets);

            // 5. Keep track of parent walls to avoid rendering them again
            for (int i = 0; i < neighbors.Length; i++)
            {
                neighbors[i].ParentWalls = sectorInfo.ParentWalls;
                neighbors[i].MirrorWall = sectorInfo.MirrorWall;
            }

            state.NeighborsForDepth.AddRange(neighbors);
        }

        /// <summary>
        /// Draw all current sectors (one wall at a time) and return the next set of portal walls to drawn.
        /// Splits <see cref="sectorRenderQueue"/> across thread A (this thread, inline) and thread B (the
        /// dedicated <see cref="ConcurrentWorkerLoop"/> worker) via <see cref="PartitionSectorQueue"/>.
        /// </summary>
        /// <param name="player">Player information</param>
        /// <returns>Set of portal walls to render next</returns>
        private List<RenderablePortalWall> DrawScreenStep(PortalPlayerSnapshot player)
        {
            ReadOnlySpan<RenderableSector> sectors = Sectors;

            threadStateA.NeighborsForDepth.Clear();
            threadStateB.NeighborsForDepth.Clear();

            PartitionSectorQueue();
            AssertQueuePartitionDisjoint();

            bool useWorker = threadStateB.SectorQueue.Count > 0;

            if (useWorker)
            {
                _ = concurrentWorkAvailableSemaphore.Release();
            }

            Span<NeighborsToRender> queueA = CollectionsMarshal.AsSpan(threadStateA.SectorQueue);
            for (int s = 0; s < queueA.Length; s++)
            {
                ProcessSectorEntry(queueA[s], player, sectors, threadStateA);
            }

            if (useWorker)
            {
                parallelRenderingDoneSemaphore.Wait();
            }

            threadStateA.NeighborsForDepth.AddRange(threadStateB.NeighborsForDepth);

            sectorRenderQueue.Clear();

            return threadStateA.NeighborsForDepth;
        }

        /// <summary>
        /// The screen-column window a queued sector entry claims: the parent portal wall's clamped
        /// [XLeft, XRight], or the full screen for the depth-0 entry (which has no parent wall).
        /// </summary>
        private (int FromX, int ToX) GetEntryWindow(NeighborsToRender entry) =>
            entry.RenderableWall is { } wall
                ? (wall.XLeft, Math.Min(wall.XRight, PixelWidth - 1))
                : (0, PixelWidth - 1);

        /// <summary>
        /// Splits <see cref="sectorRenderQueue"/> (all entries for the current depth) between
        /// <see cref="threadStateA"/> and <see cref="threadStateB"/>. A naive contiguous/round-robin split
        /// is not safe: (1) the same sector can legitimately be queued twice at one depth (reached through
        /// two different portals), and (2) <see cref="WallHelper.CalculateConnectingSectorsForSlope"/> reaches
        /// into a sloped neighbor sector's first wall even when that neighbor isn't the sector being processed.
        /// Both hazards mean certain sectors present in this depth's queue must never be split across threads,
        /// so entries are first grouped via union-find (by shared SectorId, and by sloped-portal adjacency
        /// between sectors that are both present this depth) before whole groups are assigned to a thread,
        /// balanced by an approximate column-span "weight" rather than raw entry count.
        /// </summary>
        private void PartitionSectorQueue()
        {
            threadStateA.SectorQueue.Clear();
            threadStateB.SectorQueue.Clear();

            int count = sectorRenderQueue.Count;

            if (count == 0)
            {
                return;
            }

            Span<NeighborsToRender> queue = CollectionsMarshal.AsSpan(sectorRenderQueue);

            if (count == 1)
            {
                threadStateA.SectorQueue.Add(queue[0]);
                return;
            }

            if (partitionParent.Length < count)
            {
                Array.Resize(ref partitionParent, count);
            }

            Span<int> parent = partitionParent.AsSpan(0, count);
            for (int i = 0; i < count; i++)
            {
                parent[i] = i;
            }

            partitionSectorFirstIndex.Clear();

            // Union entries that share the same SectorId (same sector queued twice this depth).
            for (int i = 0; i < count; i++)
            {
                int sectorId = queue[i].SectorId;

                if (partitionSectorFirstIndex.TryGetValue(sectorId, out int firstIndex))
                {
                    UnionPartitionGroups(parent, i, firstIndex);
                }
                else
                {
                    partitionSectorFirstIndex[sectorId] = i;
                }
            }

            // Union queued sectors connected by a sloped portal -- mirrors WallHelper's own condition in
            // CalculateConnectingSectorsForSlope. That method mutates the *neighbor's* first wall whenever
            // the current sector has a sloped portal to it, regardless of whether the neighbor itself is
            // queued this depth. So two queued sectors that both have a sloped portal to the same neighbor
            // (queued or not) must land on the same thread too, or they race writing that neighbor's wall --
            // union on the shared neighborId, not just on the neighbor's own queue entry.
            ReadOnlySpan<RenderableSector> sectors = Sectors;
            partitionSlopeNeighborFirstIndex.Clear();

            for (int i = 0; i < count; i++)
            {
                RenderableSector sector = sectors[queue[i].SectorId];
                ReadOnlySpan<RenderableWall> walls = sector.Walls;

                for (int w = 0; w < walls.Length; w++)
                {
                    RenderableWall wall = walls[w];

                    if (!wall.IsPortal || wall.Neighbor == sector.Id)
                    {
                        continue;
                    }

                    int neighborId = wall.Neighbor!.Value;
                    RenderableSector neighbor = sectors[neighborId];

                    if (!(sector.Settings.Sloped || neighbor.Settings.Sloped))
                    {
                        continue;
                    }

                    if (partitionSlopeNeighborFirstIndex.TryGetValue(neighborId, out int firstReferencingIndex))
                    {
                        UnionPartitionGroups(parent, i, firstReferencingIndex);
                    }
                    else
                    {
                        partitionSlopeNeighborFirstIndex[neighborId] = i;
                    }

                    if (partitionSectorFirstIndex.TryGetValue(neighborId, out int neighborIndex))
                    {
                        UnionPartitionGroups(parent, i, neighborIndex);
                    }
                }
            }

            // Assign whole groups to a thread, balancing by accumulated column-span weight.
            partitionGroupAssignedToB.Clear();

            long weightA = 0, weightB = 0;

            for (int i = 0; i < count; i++)
            {
                int root = FindPartitionGroup(parent, i);

                (int fromX, int toX) = GetEntryWindow(queue[i]);
                int weight = Math.Max(1, toX - fromX + 1);

                if (!partitionGroupAssignedToB.TryGetValue(root, out bool toB))
                {
                    toB = weightB <= weightA;
                    partitionGroupAssignedToB[root] = toB;
                }

                if (toB)
                {
                    threadStateB.SectorQueue.Add(queue[i]);
                    weightB += weight;
                }
                else
                {
                    threadStateA.SectorQueue.Add(queue[i]);
                    weightA += weight;
                }
            }
        }

        private static int FindPartitionGroup(Span<int> parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }

            return i;
        }

        private static void UnionPartitionGroups(Span<int> parent, int a, int b)
        {
            int rootA = FindPartitionGroup(parent, a);
            int rootB = FindPartitionGroup(parent, b);

            if (rootA != rootB)
            {
                parent[rootA] = rootB;
            }
        }

        /// <summary>
        /// Cheap tripwire for the invariant PartitionSectorQueue's safety relies on: sibling queue entries at
        /// the same depth always claim disjoint screen-column windows (guaranteed by the portal-occlusion
        /// algorithm itself), so writes into memoryPool's shared column buckets never collide across threads.
        /// </summary>
        [Conditional("DEBUG")]
        private void AssertQueuePartitionDisjoint()
        {
            Span<NeighborsToRender> a = CollectionsMarshal.AsSpan(threadStateA.SectorQueue);
            Span<NeighborsToRender> b = CollectionsMarshal.AsSpan(threadStateB.SectorQueue);

            for (int i = 0; i < a.Length; i++)
            {
                (int aFromX, int aToX) = GetEntryWindow(a[i]);

                for (int j = 0; j < b.Length; j++)
                {
                    (int bFromX, int bToX) = GetEntryWindow(b[j]);

                    Debug.Assert(aToX < bFromX || bToX < aFromX,
                        "Thread A/B sector queue partitions must have disjoint column windows.");
                }
            }
        }

        private void RenderSpritesAndTransparentWalls(PortalPlayerSnapshot player)
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
                    HashSet<int> mirroredSectorsForDepth = mirroredSectors[renderDepth];
                    Span<float> nextDistance =
                        (--renderDepth) >= 0 ? depthBuffer[(PixelWidth * renderDepth)..] : default;

                    List<RenderableSprite> sprites = SpriteHelper.FilterOutSpritesOutsideDepth(playerVisibleSprites,
                        renderedSectors, currentDistance, nextDistance);

                    foreach (RenderableSprite s in sprites)
                    {
                        DrawSprite(player, sectors, s, sectorSprites);
                    }

                    Span<RenderableSprite> mirroredSprites = SpriteHelper.GetMirroredSprites(player, Sprites, Sectors,
                        mirroredSectorsForDepth, sectorSprites);
                    sprites = SpriteHelper.FilterOutSpritesOutsideDepth(mirroredSprites, renderedSectors,
                        currentDistance, nextDistance);

                    foreach (RenderableSprite s in sprites)
                    {
                        DrawSprite(player, sectors, s, sectorSprites);
                    }
                }
            }
        }
        
        private (RenderColumnStatus Status, int SectorFromX, int SectorToX) CalculateRenderWindow(
            NeighborsToRender sectorInfo,
            ReadOnlySpan<RenderableSector> sectors,
            RenderableSector sector,
            List<RenderablePortalWall> renderableWalls,
            Span<RenderableWall> walls,
            DynamicObjectPool<RenderablePortalWall> renderablePortalWallPool)
        {
            renderableWalls.Clear();

            if (sector.Floor == sector.Ceil)
            {
                return default;
            }

            RenderColumnStatus sectorStatus = default;

            int sectorFromX, sectorToX;

            if (sectorInfo.RenderableWall is { } renderableWall)
            {
                (sectorFromX, sectorToX) = (renderableWall.XLeft, Math.Min(renderableWall.XRight, PixelWidth - 1));
            }
            else
            {
                (sectorFromX, sectorToX) = (0, PixelWidth - 1);
            }

            for (int s = 0; s < walls.Length; s++)
            {
                RenderableWall wall = walls[s];

                RenderColumnStatus status = CalculateRenderWindow(wall, sector, sectors, sectorFromX, sectorToX, renderableWalls, renderablePortalWallPool);
                sectorStatus |= status;
            }

            return (sectorStatus & RenderColumnStatus.NewRender, sectorFromX, sectorToX);

        }

        // AI Assisted: reused across calls so RenderSector's floor/ceiling and wall-rendering splits
        // don't allocate (Parallel.Invoke allocates a params array, delegates and Tasks on every call)
        private readonly SemaphoreSlim concurrentWorkAvailableSemaphore = new(0, 1);
        private readonly SemaphoreSlim parallelRenderingDoneSemaphore = new(0, 1);

        private Span<RenderablePortalWall> RenderSector(
            PortalPlayerSnapshot player,
            RenderableSector sector,
            int sectorFromX, int sectorToX,
            List<RenderablePortalWall> renderableWalls,
            RenderColumnStatus sectorStatus,
            QuickArrayPool<RenderablePortalWall> renderablePortalWallArrayPool,
            bool usePrimaryTempBuckets)
        {
            ReadOnlySpan<RenderableSector> sectors = this.Sectors;

            if (sectorStatus.HasFlag(RenderColumnStatus.CanRenderFloor))
            {
                if (sector.FloorTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox))
                {
                    RenderSkyboxFloorVector(player, sector, sectorFromX, sectorToX, usePrimaryTempBuckets);
                }
                else
                {
                    RenderFloorVector(player, sector, sectorFromX, sectorToX, usePrimaryTempBuckets);
                }
            }

            if (sectorStatus.HasFlag(RenderColumnStatus.CanRenderCeiling))
            {
                if (sector.CeilTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox))
                {
                    RenderSkyboxVector(player, sector, sectorFromX, sectorToX, usePrimaryTempBuckets);
                }
                else
                {
                    RenderCeilingVector(player, sector, sectorFromX, sectorToX, usePrimaryTempBuckets);
                }
            }

            Span<RenderablePortalWall> pool;

            if (sectorStatus.HasFlag(RenderColumnStatus.CanRenderWall))
            {
                pool = renderablePortalWallArrayPool.Request(renderableWalls.Count);

                int j = 0;
                for (int s = 0; s < renderableWalls.Count; s++)
                {
                    RenderablePortalWall renderableWall = renderableWalls[s];

                    if (!renderableWall.RenderColumnStatus.HasFlag(RenderColumnStatus.CanRenderWall))
                    {
                        continue;
                    }

                    RenderableWall wall = renderableWall.Wall;

                    bool wallDrawn = wall.IsPortal ?
                        DrawPortalWall(player, sectors, renderableWall, usePrimaryTempBuckets) :
                        DrawBasicWall(player, renderableWall, usePrimaryTempBuckets);

                    if (wallDrawn && wall.IsPortal)
                    {
                        pool[j++] = renderableWall;
                    }
                }

                pool = pool[..j];
            }
            else
            {
                pool = [];
            }

            CalculateNewFloorCeiling(sectorFromX, sectorToX);

            return pool;
        }
        
        // AI Assisted: body of the dedicated long-running worker thread started in the constructor.
        // Waits for DrawScreenStep to hand off thread B's half of the current depth's sector queue via
        // concurrentWorkAvailableSemaphore, processes it against threadStateB, and signals the matching
        // completion semaphore, instead of round-tripping through the ThreadPool.
        private void ConcurrentWorkerLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    concurrentWorkAvailableSemaphore.Wait(cancellationToken);

                    // AI Assisted: try/finally so a fault in ProcessSectorEntry always releases the completion
                    // semaphore -- otherwise the main thread would hang forever in DrawScreenStep's Wait().
                    try
                    {
                        ReadOnlySpan<RenderableSector> sectors = Sectors;
                        Span<NeighborsToRender> queueB = CollectionsMarshal.AsSpan(threadStateB.SectorQueue);

                        for (int s = 0; s < queueB.Length; s++)
                        {
                            ProcessSectorEntry(queueB[s], Snapshot!, sectors, threadStateB);
                        }
                    }
                    finally
                    {
                        _ = parallelRenderingDoneSemaphore.Release();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private RenderColumnStatus CalculateRenderWindow(
            RenderableWall wall,
            RenderableSector sector,
            ReadOnlySpan<RenderableSector> sectors,
            int sectorFromX, int sectorToX,
            List<RenderablePortalWall> renderableWalls,
            DynamicObjectPool<RenderablePortalWall> renderablePortalWallPool)
        {
            Span<RenderColumnStatus> renderStatus = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            (bool canRender, int wallFromX, int wallToX) = SetWallToCalculate(renderStatus, sectorFromX, sectorToX, wall);
            
            if (!canRender)
            {
                // don't render this wall, as it's not within the window or is fully obscured by other walls
                return default;
            }
            
            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);

            Span<int> portalFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFrom);
            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);
            Span<int> portalFromClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFromClamped);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);


            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            
            int offset = wallFromX > wall.XLeft ? wallFromX - wall.XLeft : 0;

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

                        // AI Assisted
                        var rw = renderablePortalWallPool.GetOrCreate();
                        rw.Initialize(wall, renderableFromX, x, offset, status);
                        renderableWalls.Add(rw);
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

                // AI Assisted
                var rw = renderablePortalWallPool.GetOrCreate();
                rw.Initialize(wall, renderableFromX, renderableToX, offset, status);
                renderableWalls.Add(rw);
            }

            return wallStatus;
        }
        
        public static (bool CanRender, int WallFromX, int WallToX) SetWallToCalculate(
            ReadOnlySpan<RenderColumnStatus> status,
            int sectorFromX, int sectorToX,
            RenderableWall wall)
        {
            //this.wall = wall;
            int wallFromX = wall.XLeft;
            int wallToX = wall.XRight;

            // clamp to sector window
            wallFromX = Math.Max(sectorFromX, wall.XLeft);
            wallToX = Math.Min(sectorToX, wall.XRight);

            int i, j;

            for (i = wallFromX; i <= wallToX; i++)
            {
                RenderColumnStatus columnStatus = status[i];

                if (!columnStatus.IsFinished && !columnStatus.IsCalculated)
                {
                    break;
                }
            }

            for (j = wallToX; j >= wallFromX; j--)
            {
                RenderColumnStatus columnStatus = status[j];

                if (!columnStatus.IsFinished && !columnStatus.IsCalculated)
                {
                    break;
                }
            }

            (wallFromX, wallToX) = (i, j);

            // wall has been rendered over for this sector
            return (wallFromX < wallToX, wallFromX, wallToX);
        }

        [MemberNotNull(nameof(Snapshot))]
        public void* DrawFrame(PortalPlayerSnapshot snapShot)
        {
            Snapshot = snapShot;

            DrawScreen(snapShot);

            return this.Buffer;
        }
    }
}