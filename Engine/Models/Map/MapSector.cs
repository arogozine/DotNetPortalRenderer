namespace RenderingEngine.Models
{
    internal sealed class MapSector
    {
        public required int Id { get; set; }
        public List<Line> Walls { get; set; } = [];
        public required float Floor { get; set; }
        public required float Ceiling { get; set; }
        public required TextureInfo FloorTexture { get; set; }
        public required TextureInfo CeilingTexture { get; set; }
        public required short LightLevel { get; set; }
    }
}
