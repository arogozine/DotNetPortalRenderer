namespace RenderingEngine.Models
{
    public sealed class MapSector
    {
        public required int Id { get; set; }
        public List<Line> Walls { get; set; } = [];
        public required float Floor { get; set; }
        public required float Ceiling { get; set; }
        public required string FloorTexture { get; set; }
        public required string CeilingTexture { get; set; }
        public required bool HasSkybox { get; set; }
    }
}
