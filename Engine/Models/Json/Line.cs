namespace RenderingEngine.Models.Json
{
    public sealed class Line
    {
        public required int Id { get; set; }
        public required Vector PointA { get; set; }
        public required Vector PointB { get; set; }
        public required int? SectorTo { get; set; }

        public required string? UpperTexture { get; set; }
        public required string? MiddleTexture { get; set; }
        public required string? LowerTexture { get; set; }
    }
}
