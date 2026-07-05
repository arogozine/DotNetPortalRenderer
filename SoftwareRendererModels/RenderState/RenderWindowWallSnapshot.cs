using System.Diagnostics;

namespace SoftwareRendererModels;

/// <summary>
/// Render Window Snapshot for a certain depth. For transparent wall rendering.
/// </summary>
[DebuggerDisplay("Id = {Id}, XLeft = {XLeft}, XRight = {XRight}")]
public sealed class RenderWindowWallSnapshot : RenderableSpriteSnapshot
{
    public int Id => Wall.Id;
    public int Offset { get; set; }
    public RenderableWall Wall { get; set; } = null!;
}
