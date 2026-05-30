namespace SoftwareRendererModels;

public sealed class PlayerStart : IFixedState
{
    public required XyzTuple Where { get; init; }
    public required float ViewAngle { get; init; }
    public required int Sector { get; init; }
}
