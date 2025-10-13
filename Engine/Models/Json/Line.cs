namespace RenderingEngine.Models.Json
{
    public sealed class Line
    {
        public required int Id { get; set; }
        public required Point PointA { get; set; }
        public required Point PointB { get; set; }
        public required int? SectorTo { get; set; }

        public required string? UpperTexture { get; set; }
        public required string? MiddleTexture { get; set; }
        public required string? LowerTexture { get; set; }
        public int YOffset { get; set; }
        public int XOffset { get; set; }
    }
}
