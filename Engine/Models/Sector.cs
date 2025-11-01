namespace RenderingEngine.Models
{
    internal class Sector
    {
        public required int Id { get; internal set; }

        public required bool HasSkybox { get; init; }
        public required string FloorTexture { get; init; }
        public required string CeilTexture { get; init; }

        public required float Floor { get; init; }
        public required float Ceil { get; init; }
        public required Wall[] Walls { get; init; }
        public required byte LightLevel { get; set; }
    }
}
