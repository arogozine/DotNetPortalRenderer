namespace RenderingEngine.Models;

internal sealed class Sprite
{
    public required int Id { get; set; }
    public int SectorId { get; set; }
    public required Point Location { get; init; }
    public Point PointA { get; set; }
    public Point PointB { get; set; }
    public required float Angle { get; init; }
    public required float Height { get; init; }
    public required TextureInfo Texture { get; init; }

    public float Length { get; set; }

}
