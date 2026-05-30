namespace SoftwareRendererModels;

public sealed class FloorSpriteWallInfo : IRenderState
{
    public required bool IntersectsView { get; set; }
    public int XLeft { get; set; }
    public int XRight { get; set; }
    public int YLeftFloor { get; set; }
    public int YRightFloor { get; set; }
}
