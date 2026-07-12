using RenderingEngine.Engine;
using SoftwareRendererModels;
using Tooling;

namespace RenderingEngine.Tooling;

/// <summary>
/// PortalRender needs a lot of temporary objects.
/// Object Pool is used to prevent GC pauses by re-using temporary objects.
/// </summary>
internal static class ObjectPool
{
    public static readonly SimpleObjectPool<RenderWindowSpriteSnapshot> RenderWindowSpriteSnapshot
        = new(EngineConstants.MaxRenderDepth, ClearSnapshot);

    public static readonly DynamicObjectPool<HashSet<int>> HashSet = new(static (x) => x.Clear());

    public static readonly DynamicObjectPool<Queue<int>> Queue = new(static (x) => x.Clear());

    public static readonly PortalPlayerSnapshot PortalPlayerSnapshot = new();

    public static readonly DynamicObjectPool<FloorSpriteWallInfo> FloorSpriteWallInfo
        = new(ClearSnapshot);

    public static readonly DynamicObjectPool<RenderWindowWallSnapshot> RenderWindowWallSnapshot
        = new(ClearSnapshot);

    public static readonly DynamicObjectPool<NeighborsToRender> NeighborsToRender
        = new(static n => n.Reset());

    public static readonly QuickArrayPool<RenderableWall> RotatedWallArrayPool = new();

    public static readonly QuickArrayPool<RenderablePortalWall> RenderablePortalWall = new();

    public static readonly DynamicObjectPool<RenderablePortalWall> RenderablePortalWallPool = new(static r => r.Reset());

    internal static void Clear()
    {
        HashSet.Clear();
        RenderWindowSpriteSnapshot.Reset();
        FloorSpriteWallInfo.Reset();
        RenderWindowWallSnapshot.Reset();
        NeighborsToRender.Reset();
        RotatedWallArrayPool.ClearAndOptimize();
        RenderablePortalWall.ClearAndOptimize();
        RenderablePortalWallPool.Clear();
    }

    private static void ClearSnapshot(FloorSpriteWallInfo floorSpriteWallInfo)
    {
        floorSpriteWallInfo.IntersectsView = false;
        floorSpriteWallInfo.YLeftFloor = default;
        floorSpriteWallInfo.YRightFloor = default;
        floorSpriteWallInfo.XLeft = default;
        floorSpriteWallInfo.XRight = default;
    }

    private static void ClearSnapshot(RenderWindowSpriteSnapshot s)
    {
        s.MirroredWalls?.Clear();
        s.Depth = s.XRight = s.XLeft = s.RenderDepth = default;
    }

    private static void ClearSnapshot(RenderWindowWallSnapshot rw)
    {
        rw.Depth = 0;
        rw.Wall = null!;
        rw.XLeft = 0;
        rw.Offset = 0;
        rw.XRight = 0;
    }
}