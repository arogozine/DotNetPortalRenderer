namespace RenderingEngine.Models
{
    internal sealed class Line : IEquatable<Line>
    {
        public required int Id { get; init; }
        public required Point PointA { get; set; }
        public required Point PointB { get; set; }
        public required int? SectorTo { get; set; }

        public required TextureInfo? UpperTexture { get; set; }
        public required TextureInfo? MiddleTexture { get; set; }
        public required TextureInfo? LowerTexture { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Line line &&
                   Id == line.Id;
        }

        public bool Equals(Line? other)
        {
            return other?.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
