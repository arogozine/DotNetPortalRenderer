namespace RenderingEngine.Models
{
    public sealed class Line : IEquatable<Line>
    {
        public required int Id { get; init; }
        public required Point PointA { get; set; }
        public required Point PointB { get; set; }
        public required int? SectorTo { get; set; }

        public required string? UpperTexture { get; init; }
        public required string? MiddleTexture { get; init; }
        public required string? LowerTexture { get; init; }
        public required int YOffset { get; init; }
        public required int XOffset { get; init; }
        public required bool LowerUnpegged { get; init; }
        public required bool UpperUnpegged { get; init; }

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
