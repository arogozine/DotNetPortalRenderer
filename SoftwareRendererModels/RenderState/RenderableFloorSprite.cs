using System.Numerics;

namespace SoftwareRendererModels;

public sealed class RenderableFloorSprite : RenderableSprite
{
    public Vector2 PointC => ((FloorSprite)Sprite).PointC;
    public Vector2 PointD => ((FloorSprite)Sprite).PointD;

    public Vector2 R3 { get; set; }
    public Vector2 R4 { get; set; }

    public FloorSpriteWallInfo? Wall1 { get; set; }
    public FloorSpriteWallInfo? Wall2 { get; set; }
    public FloorSpriteWallInfo? Wall3 { get; set; }
    public FloorSpriteWallInfo? Wall4 { get; set; }
}
