namespace RenderingEngine.Models.Json
{
    public sealed class Line
    {
        public required int WallId { get; set; }
        public required Vector PointA { get; set; }
        public required Vector PointB { get; set; }
        public required int? SectorTo { get; set; }
    }
}
