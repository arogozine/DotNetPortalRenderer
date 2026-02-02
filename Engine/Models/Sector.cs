namespace RenderingEngine.Models
{
    internal class Sector
    {
        public required int Id { get; internal set; }

        public required TextureInfo FloorTexture { get; init; }
        public required TextureInfo CeilTexture { get; init; }

        public required int Floor { get; init; }
        public required int Ceil { get; init; }
        public required RenderableWall[] Walls { get; init; }
        public required short FloorShade { get; set; }
        public required short CeilingShade { get; set; }
        public required float? RotationFloor { get; set; }
        public required float? RotationCeiling { get; set; }
    }
}
