using System.Diagnostics;

namespace SoftwareRendererModels;

[DebuggerDisplay("Sectors = {Sectors.Count}, Sprites = {Sprites.Length}")]
public sealed class Map : IFixedState
{
    public required PlayerStart Player { get; init; }
    public required List<MapSector> Sectors { get; init; }
    public required Sprite[] Sprites { get; init; }
}
