namespace RenderingEngine.Models
{
    internal sealed class MapSector
    {
        public required int Id { get; set; }
        public List<Line> Walls { get; set; } = [];
        public required int Floor { get; set; }
        public required int Ceiling { get; set; }
        public required TextureInfo FloorTexture { get; set; }
        public required TextureInfo CeilingTexture { get; set; }
        public required short FloorShade { get; set; }
        public required short CeilingShade { get; set; }
        public float? RotationCeiling { get; set; }
        public float? RotationFloor { get; set; }
    }
}
