using System.Diagnostics;

namespace SoftwareRendererModels;

/// <summary>
/// Render Window Snapshot for a certain depth. For sprite rendering.
/// </summary>
[DebuggerDisplay("XLeft = {XLeft}, XRight = {XRight}, Depth = {Depth}, RenderDepth = {RenderDepth}")]
public sealed class RenderWindowSpriteSnapshot : RenderableSpriteSnapshot
{
    public int RenderDepth { get; set; }
    public HashSet<RenderableWall> MirroredWalls { get; } = [];
}
