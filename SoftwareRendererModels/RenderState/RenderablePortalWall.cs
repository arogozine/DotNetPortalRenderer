using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SoftwareRendererModels;

[DebuggerDisplay("Id = {Id}, XLeft = {XLeft}, XRight = {XRight}")]
public sealed class RenderablePortalWall
{
    public int Id => Wall.Id;
    public RenderableWall Wall { get; private set; } = null!;
    public int XLeft { get; set; }
    public int XRight { get; set; }
    public int Offset { get; set; }
    public Tooling.ArraySegment<RenderableWall> ParentWalls { get; set; } = Tooling.ArraySegment<RenderableWall>.Empty;
    public RenderColumnStatus RenderColumnStatus { get; private set; }
    public RenderableWall? MirrorWall { get; set; }

    public bool IsPortalWithMiddleTexture => Wall.IsPortal && Wall.MiddleTexture != null;

    [MemberNotNull(nameof(Wall))]
    public void Initialize(RenderableWall wall, int xLeft, int xRight, int offset, RenderColumnStatus renderColumnStatus)
    {
        Wall = wall;
        XLeft = xLeft;
        XRight = xRight;
        Offset = offset;
        RenderColumnStatus = renderColumnStatus;
        MirrorWall = null;
    }

    public void Reset()
    {
        Wall = null!;
        XLeft = 0;
        XRight = 0;
        Offset = 0;
        RenderColumnStatus = default;
        MirrorWall = null;
        ParentWalls = Tooling.ArraySegment<RenderableWall>.Empty;
    }
}
