namespace RenderingEngine.Models;

internal class Sector
{
    public required MapSector MapSector { get; init; }

    public int Id => MapSector.Id;
    public TextureInfo FloorTexture => MapSector.FloorTexture;
    public TextureInfo CeilTexture => MapSector.CeilingTexture;
    public MapSectorSettings Settings => MapSector.Settings;
    public int Floor => MapSector.Floor;
    public int Ceil => MapSector.Ceiling;
    public required short FloorShade { get; init; }
    public required short CeilingShade { get; init; }
    public float? RotationFloor => MapSector.RotationFloor;
    public float? RotationCeiling => MapSector.RotationCeiling;
    public float? CeilingSlope => MapSector.CeilingSlope;
    public float? FloorSlope => MapSector.FloorSlope;

    public required RenderableWall[] Walls { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is Sector otherSector && otherSector.Id == this.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
