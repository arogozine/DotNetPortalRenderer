using RenderingEngine.Tooling;
using SoftwareRendererModels;
using Tooling;

namespace RenderingEngine.Engine
{
    /// <summary>
    /// Per-thread scratch state for one half of a parallelized <c>DrawScreenStep</c> call.
    /// PortalRenderer owns two instances (one per thread) so the two halves never share mutable state.
    /// </summary>
    internal sealed class RenderThreadState
    {
        public required WallHelper WallHelper { get; init; }

        /// <summary>
        /// Selects which physical memoryPool scratch buckets (Temp/Temp2 vs Temp3/Temp4) this thread's
        /// floor/ceiling/wall rendering uses. Two threads processing disjoint screen columns still share
        /// the same memoryPool instance, and some of its scratch buckets (unlike the per-column state
        /// buckets) are not safe for two concurrent execution contexts to touch at once -- each thread
        /// must get its own scratch bucket pair, mirroring the Temp/Temp2 vs Temp3/Temp4 split the
        /// codebase already uses to let ceiling and floor rendering of a single sector run concurrently.
        /// </summary>
        public required bool UsePrimaryTempBuckets { get; init; }

        public readonly List<NeighborsToRender> SectorQueue = [];
        public readonly List<RenderablePortalWall> RenderableWalls = [];
        public readonly List<RenderablePortalWall> NeighborsForDepth = [];

        public readonly DynamicObjectPool<RenderablePortalWall> RenderablePortalWallPool = new(static r => r.Reset());
        public readonly QuickArrayPool<RenderablePortalWall> RenderablePortalWallArrayPool = new();

        /// <summary>
        /// Recycles the pools that are only safe to reset once per frame (not per depth).
        /// </summary>
        public void ClearPoolsForNextFrame()
        {
            RenderablePortalWallPool.Reset();
            RenderablePortalWallArrayPool.ClearAndOptimize();
            WallHelper.ClearPool();
        }
    }
}
