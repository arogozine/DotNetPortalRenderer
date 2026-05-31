namespace SoftwareRendererModels;

public sealed class RenderableSector : IRenderState
{
    public required MapSector MapSector { get; init; }
    public required RenderableWall[] Walls { get; init; }

    public int Id => MapSector.Id;
    public GameTextureInfo FloorTexture => MapSector.FloorTexture;
    public GameTextureInfo CeilTexture => MapSector.CeilingTexture;
    public MapSectorSettings Settings => MapSector.Settings;
    public int Floor => MapSector.Floor;
    public int Ceil => MapSector.Ceiling;
    public short FloorShade => MapSector.FloorShade;
    public short CeilingShade => MapSector.CeilingShade;
    public float? RotationFloor => MapSector.RotationFloor;
    public float? RotationCeiling => MapSector.RotationCeiling;
    public float? CeilingSlope => MapSector.CeilingSlope;
    public float? FloorSlope => MapSector.FloorSlope;
}
