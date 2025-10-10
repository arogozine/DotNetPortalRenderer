namespace RenderingEngine.Models.Json
{
    public sealed class Map
    {
        public required PlayerStart PlayerStart { get; set; }
        public List<MapSector> Sectors { get; set; } = [];
    }
}
