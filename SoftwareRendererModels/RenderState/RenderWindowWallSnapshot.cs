namespace SoftwareRendererModels;

/// <summary>
/// Render Window Snapshot for a certain depth. For transparent wall rendering.
/// </summary>
public sealed class RenderWindowWallSnapshot : RenderableSpriteSnapshot
{
    public required int Offset { get; init; }
    public required RenderableWall Wall { get; init; }
}
