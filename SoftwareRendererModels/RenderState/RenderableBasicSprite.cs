namespace SoftwareRendererModels;

public sealed class RenderableBasicSprite : RenderableSprite, IWallLike
{
    public int YLeftCeil { get; set; }
    public int YLeftFloor { get; set; }
    public int YRightCeil { get; set; }
    public int YRightFloor { get; set; }
}
