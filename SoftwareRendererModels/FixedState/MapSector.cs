using System.Diagnostics;

namespace SoftwareRendererModels;

[DebuggerDisplay("Id = {Id}")]
public sealed class MapSector : IFixedState
{
    public required int Id { get; init; }
    public required MapSectorSettings Settings { get; init; }
    public List<Line> Walls { get; } = [];
    public required int Floor { get; init; }
    public required int Ceiling { get; init; }
    public required GameTextureInfo FloorTexture { get; init; }
    public required GameTextureInfo CeilingTexture { get; init; }
    public required short FloorShade { get; init; }
    public required short CeilingShade { get; set; }
    public float? RotationCeiling { get; set; }
    public float? RotationFloor { get; set; }
    public float? CeilingSlope { get; init; }
    public float? FloorSlope { get; init; }
}
