namespace RenderingEngine.Models
{
    public sealed class Map
    {
        public required Player Player { get; set; }
        public List<MapSector> Sectors { get; set; } = [];
    }
}
