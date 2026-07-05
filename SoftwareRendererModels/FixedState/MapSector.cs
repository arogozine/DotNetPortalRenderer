using System.Diagnostics;

namespace SoftwareRendererModels;

[DebuggerDisplay("Id = {Id}")]
public sealed class MapSector : IFixedState
{
    public required int Id { get; init; }
    public required MapSectorSettings Settings { get; set; }
    public List<Line> Walls { get; set; } = [];
    public required int Floor { get; set; }
    public required int Ceiling { get; set; }
    public required GameTextureInfo FloorTexture { get; set; }
    public required GameTextureInfo CeilingTexture { get; set; }
    public required short FloorShade { get; set; }
    public required short CeilingShade { get; set; }
    public float? RotationCeiling { get; set; }
    public float? RotationFloor { get; set; }
    public float? CeilingSlope { get; set; }
    public float? FloorSlope { get; set; }
}
