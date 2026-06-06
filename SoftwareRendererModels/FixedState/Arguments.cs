namespace SoftwareRendererModels;

public sealed class Arguments : IFixedState
{
    public required string? IWad { get; init; }
    public required string? PWad { get; init; }
    public required string Map { get; init; }
    public required string? DukePath { get; init; }
}
