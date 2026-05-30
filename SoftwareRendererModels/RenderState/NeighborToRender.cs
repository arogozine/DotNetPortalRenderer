namespace SoftwareRendererModels;

public sealed class NeighborsToRender : IRenderState
{
    public NeighborsToRender() {
        ParentWalls = [];
    }

    public NeighborsToRender(RenderablePortalWall renderableWall, scoped ReadOnlySpan<RenderableWall> walls)
    {
        this.RenderableWall = renderableWall;
        ParentWalls = new RenderableWall[walls.Length + 1];
        walls.CopyTo(ParentWalls);
        ParentWalls[^1] = renderableWall.Wall;
    }

    public RenderableWall? MirrorWall { get; set; }
    public required int SectorId { get; init; }
    public RenderablePortalWall? RenderableWall { get; init; }
    public RenderableWall[] ParentWalls { get; }
}
