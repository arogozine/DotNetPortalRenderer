namespace SoftwareRendererModels;

public sealed class Map : IFixedState
{
    public required PlayerStart Player { get; set; }
    public List<MapSector> Sectors { get; set; } = [];
    public required Sprite[] Sprites { get; set; }
}
