namespace SoftwareRendererModels;

/// <summary>
/// Render Window Snapshot for a certain depth. For transparent wall rendering.
/// </summary>
public sealed class RenderWindowWallSnapshot : RenderableSpriteSnapshot
{
    public int Offset { get; set; }
    public RenderableWall Wall { get; set; } = null!;
}
