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

    public static readonly DynamicObjectPool<RenderablePortalWall> RenderablePortalWall
        = new(static r => r.Reset());

    public static readonly QuickArrayPool<RenderableWall> RenderableWallPool = new();

    public static readonly QuickArrayPool<RenderablePortalWall> RenderablePortalWallPool = new();
    
    internal static void Clear()
    {
        HashSet.Clear();
        RenderWindowSpriteSnapshot.Reset();
        FloorSpriteWallInfo.Reset();
        RenderWindowWallSnapshot.Reset();
        NeighborsToRender.Reset();
        RenderablePortalWall.Reset();
        RenderableWallPool.ClearAndOptimize();
        RenderablePortalWallPool.ClearAndOptimize();
    }

    static void ClearSnapshot(FloorSpriteWallInfo floorSpriteWallInfo)
    {
        floorSpriteWallInfo.IntersectsView = false;
        floorSpriteWallInfo.YLeftFloor = default;
        floorSpriteWallInfo.YRightFloor = default;
        floorSpriteWallInfo.XLeft = default;
        floorSpriteWallInfo.XRight = default;
    }

    static void ClearSnapshot(RenderWindowSpriteSnapshot s)
    {
        s.MirroredWalls?.Clear();
        s.Depth = s.XRight = s.XLeft = s.RenderDepth = default;
    }

    static void ClearSnapshot(RenderWindowWallSnapshot rw)
    {
        rw.Depth = 0;
        rw.Wall = null!;
        rw.XLeft = 0;
        rw.Offset = 0;
        rw.XRight = 0;
    }
}