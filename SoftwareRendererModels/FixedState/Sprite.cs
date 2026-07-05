using System.Diagnostics;
using System.Numerics;

namespace SoftwareRendererModels;

[DebuggerDisplay("Id = {Id}, Texture = {Texture.Name}")]
public class Sprite : IFixedState
{
    public required int Id { get; init; }
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