using System.Numerics;

namespace SoftwareRendererModels;

public class Sprite
{
    public required int Id { get; set; }
    public int SectorId { get; set; }
    public required Vector2 Location { get; init; }
    public Vector2 PointA { get; set; }
    public Vector2 PointB { get; set; }
    public required float Angle { get; init; }
    public required float Height { get; init; }
    public required GameTextureInfo Texture { get; init; }
    public GameSpriteAnimation? AnimationAngle { get; set; }
    public float Length { get; set; }
    public short? Shade { get; set; }
}

public sealed class WallSprite : Sprite
{
    public bool TwoSided { get; set; }
}

public sealed class FloorSprite : Sprite
{
    public Vector2 PointC { get; set; }
    public Vector2 PointD { get; set; }
}
