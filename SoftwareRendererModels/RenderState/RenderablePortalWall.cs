namespace SoftwareRendererModels;

public sealed class RenderablePortalWall
{
    private int _length;
    private RenderableWall[]? _parentWalls;

    public RenderableWall Wall { get; set; } = null!;
    public int XLeft { get; set; }
    public int XRight { get; set; }
    public int Offset { get; set; }
    public ReadOnlySpan<RenderableWall> ParentWalls => _parentWalls.AsSpan()[.._length];
    public RenderColumnStatus RenderColumnStatus { get; set; }
    public RenderableWall? MirrorWall { get; set; }

    public bool IsPortalWithMiddleTexture => Wall.IsPortal && Wall.MiddleTexture != null;

    // AI Assisted
    public void Initialize(RenderableWall wall, int xLeft, int xRight, int offset, RenderColumnStatus renderColumnStatus)
    {
        Wall = wall;
        XLeft = xLeft;
        XRight = xRight;
        Offset = offset;
        RenderColumnStatus = renderColumnStatus;
        _parentWalls = null;
        _length = 0;
        MirrorWall = null;
    }

    // AI Assisted
    public void Reset()
    {
        Wall = null!;
        XLeft = 0;
        XRight = 0;
        Offset = 0;
        _parentWalls = null;
        _length = 0;
        RenderColumnStatus = default;
        MirrorWall = null;
    }

    public void SetParentWalls(RenderableWall[]? parentWalls, int length)
    {
        _parentWalls = parentWalls;
        _length = length;
    }
}
