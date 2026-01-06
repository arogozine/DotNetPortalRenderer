namespace RenderingEngine.Models;

internal sealed class Line : IEquatable<Line>
{
    public required int Id { get; init; }
    public required LineVector PointA { get; set; }
    public required LineVector PointB { get; set; }
    public required int? SectorTo { get; set; }

    public required TextureInfo? UpperTexture { get; set; }
    public required TextureInfo? MiddleTexture { get; set; }
    public required TextureInfo? LowerTexture { get; set; }

    public override bool Equals(object? obj) => Equals(obj as Line);
    public bool Equals(Line? other) => other?.Id == Id;
    public override int GetHashCode() => Id.GetHashCode();
}
