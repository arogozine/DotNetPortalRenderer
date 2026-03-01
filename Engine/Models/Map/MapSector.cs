namespace RenderingEngine.Models;

[Flags]
internal enum MapSectorSettings
{
    RotateCeiling = 1,
    RotateFloor = 2,
    SlopeCeiling = 4,
    SlopeFloor = 8
}

internal sealed class MapSector
{
    public required int Id { get; set; }
    public required MapSectorSettings Settings { get; set; }
    public List<Line> Walls { get; set; } = [];
    public required int Floor { get; set; }
    public required int Ceiling { get; set; }
    public required TextureInfo FloorTexture { get; set; }
    public required TextureInfo CeilingTexture { get; set; }
    public required short FloorShade { get; set; }
    public required short CeilingShade { get; set; }
    public float? RotationCeiling { get; set; }
    public float? RotationFloor { get; set; }
    public float? CeilingSlope { get; set; }
    public float? FloorSlope { get; set; }
}
