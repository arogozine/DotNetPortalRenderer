using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SoftwareRendererModels;

[DebuggerDisplay("SectorId = {SectorId}")]
public sealed class NeighborsToRender : IRenderState
{
    public RenderableWall? MirrorWall { get; set; }
    public int SectorId { get; set; }
    public RenderablePortalWall? RenderableWall { get; private set; }
    public Tooling.ArraySegment<RenderableWall> ParentWalls { get; set; } = Tooling.ArraySegment<RenderableWall>.Empty;

    public void Initialize(int sectorId)
    {
        SectorId = sectorId;
        RenderableWall = null;
        MirrorWall = null;
    }

    [MemberNotNull(nameof(RenderableWall))]
    public void Initialize(Tooling.ArraySegment<RenderableWall> parentWalls, RenderablePortalWall renderableWall, int sectorId)
    {
        ParentWalls = parentWalls;

        SectorId = sectorId;
        RenderableWall = renderableWall;
        MirrorWall = null;
    }

    public void Reset()
    {
        SectorId = 0;
        RenderableWall = null;
        MirrorWall = null;
        ParentWalls = Tooling.ArraySegment<RenderableWall>.Empty;
    }
}
