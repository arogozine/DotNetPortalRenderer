namespace RenderingEngine.Models.Json
{

    public sealed class MapSector
    {
        public required int SectorId { get; set; }
        public List<int> Children { get; set; } = [];
        public List<Line> Walls { get; set; } = [];
        public required float Floor { get; set; }
        public required float Ceiling { get; set; }
    }

    public sealed class MapPlayer
    {
        public required float XPosition { get; set; }
        public required float YPosition { get; set; }
        public required float ZPosition { get; set; }
        public required float Angle { get; set; }
    }

    public sealed class Map
    {
        public required MapPlayer Player { get; set; }
        public List<MapSector> Sectors { get; set; } = [];
    }
}
