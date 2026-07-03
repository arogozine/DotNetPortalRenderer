using RenderingEngine.Engine;
using SoftwareRendererModels;

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

    // AI Assisted
    public static readonly DynamicObjectPool<RenderablePortalWall> RenderablePortalWall
        = new(static r => r.Reset());

    internal static void Clear()
    {
        HashSet.Clear();
        RenderWindowSpriteSnapshot.Reset();
        FloorSpriteWallInfo.Reset();
        RenderWindowWallSnapshot.Reset();
        NeighborsToRender.Reset();
        RenderablePortalWall.Reset();
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

internal sealed class SimpleObjectPool<T>
    where T : class, new()
{
    private readonly T?[] _objects;
    private readonly Action<T> _reset;

    public SimpleObjectPool(int count, Action<T> reset)
    {
        _objects = new T[count];
        _reset = reset;
    }

    public T GetOrCreate(int index)
    {
        T? obj = _objects[index];

        if (obj == null)
        {
            obj = new();
            _objects[index] = obj;
        }

        return obj;
    }

    public void Reset()
    {
        for (int i = 0; i < _objects.Length; i++)
        {
            if (_objects[i] is { } obj)
            {
                _reset(obj);
            }
        }
    }
}

internal sealed class DynamicObjectPool<T>
        where T : class, new()
{
    private int _requestedObjectCount = 0;
    private T?[] _objects;
    private readonly Action<T> _reset;

    public DynamicObjectPool(Action<T> reset)
    {
        _objects = new T[32];
        _reset = reset;
    }

    public void Clear() => _objects.AsSpan().Clear();

    public T GetOrCreate()
    {
        int index = ++_requestedObjectCount;

        if (index >= _objects.Length)
        {
            Array.Resize(ref _objects, _objects.Length * 2);
        }

        T? obj = _objects[index];

        if (obj == null)
        {
            obj = new();
            _objects[index] = obj;
        }

        return obj;
    }

    public void Reset()
    {
        _requestedObjectCount = 0;

        for (int i = 0; i < _objects.Length; i++)
        {
            if (_objects[i] is { } obj)
            {
                _reset(obj);
            }
        }
    }
}
