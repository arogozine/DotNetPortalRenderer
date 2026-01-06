namespace RenderingEngine.Models;

internal sealed class Map
{
    public required Player Player { get; set; }
    public List<MapSector> Sectors { get; set; } = [];
    public required Sprite[] Sprites { get; set; }
}
