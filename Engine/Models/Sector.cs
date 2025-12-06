namespace RenderingEngine.Models
{
    internal class Sector
    {
        public required int Id { get; internal set; }

        public required TextureInfo FloorTexture { get; init; }
        public required TextureInfo CeilTexture { get; init; }

        public required float Floor { get; init; }
        public required float Ceil { get; init; }
        public required Wall[] Walls { get; init; }
        public required byte LightLevel { get; set; }
        public required float RotationFloor { get; set; }
        public required float RotationCeiling { get; set; }
    }
}
