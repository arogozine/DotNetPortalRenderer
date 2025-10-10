namespace RenderingEngine.Models.Json
{
    public sealed class MapSector
    {
        public required int Id { get; set; }
        public List<int> Children { get; set; } = [];
        public List<Line> Walls { get; set; } = [];
        public required float Floor { get; set; }
        public required float Ceiling { get; set; }
        public required string FloorTexture { get; set; }
        public required string CeilingTexture { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is MapSector sector &&
                   Id == sector.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}
