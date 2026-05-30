namespace SoftwareRendererModels;

/// <summary>
/// Render Window Snapshot for a certain depth. For sprite rendering.
/// </summary>
public sealed class RenderWindowSpriteSnapshot : RenderableSpriteSnapshot
{
    public required int RenderDepth { get; init; }
    public required HashSet<int> RenderedSectors { get; init; }
    public HashSet<RenderableWall>? MirroredWalls { get; set; }
}
