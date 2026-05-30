namespace SoftwareRendererModels;

public sealed class RenderableWallSprite : RenderableSprite, IWallLike
{
    public int YLeftCeil { get; set; }
    public int YLeftFloor { get; set; }
    public int YRightCeil { get; set; }
    public int YRightFloor { get; set; }
    public bool? TwoSided => ((WallSprite)Sprite).TwoSided;
}
