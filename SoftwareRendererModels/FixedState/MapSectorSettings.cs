namespace SoftwareRendererModels;

[Flags, FixedState]
public enum MapSectorSettings
{
    None = 0,
    RotateCeiling = 1,
    RotateFloor = 2,
    SlopeCeiling = 4,
    SlopeFloor = 8
}