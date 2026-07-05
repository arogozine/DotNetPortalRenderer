using System.Diagnostics;

namespace SoftwareRendererModels;

[DebuggerDisplay("XLeft = {XLeft}, XRight = {XRight}")]
public sealed class FloorSpriteWallInfo : IRenderState
{
    public bool IntersectsView { get; set; }
    public int XLeft { get; set; }
    public int XRight { get; set; }
    public int YLeftFloor { get; set; }
    public int YRightFloor { get; set; }
}
