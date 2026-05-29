using System.Collections.Frozen;

namespace RenderingEngine.Models;

internal sealed record TextureAngle(float Angle, Texture Texture, bool Flipped);

internal sealed class SpriteAnimationAngle
{
    public required TextureAngle[][] AnimationToAngleToTexture { get; init; }
}

internal class Sprite
{
    public required int Id { get; set; }
    public int SectorId { get; set; }
    public required Point Location { get; init; }
    public Point PointA { get; set; }
    public Point PointB { get; set; }
    public required float Angle { get; init; }
    public required float Height { get; init; }
    public required TextureInfo Texture { get; init; }
    public SpriteAnimationAngle? AnimationAngle { get; set; }
    public float Length { get; set; }
    public short? Shade { get; set; }

}

internal sealed class WallSprite : Sprite
{
    public bool TwoSided { get; set; }
}

internal sealed class FloorSprite : Sprite
{
    public Point PointC { get; set; }
    public Point PointD { get; set; }
}
