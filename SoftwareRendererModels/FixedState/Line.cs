using System.Diagnostics;

namespace SoftwareRendererModels;

[DebuggerDisplay("Id = {Id}, SectorTo = {SectorTo}")]
public sealed class Line : IFixedState
{
    public required int Id { get; init; }
    public required LineVector PointA { get; init; }
    public required LineVector PointB { get; init; }
    public required int? SectorTo { get; set; }

    public required GameTextureInfo? UpperTexture { get; set; }
    public required GameTextureInfo? MiddleTexture { get; set; }
    public required GameTextureInfo? LowerTexture { get; set; }

    public bool TwoSided { get; init; }
    public bool IsMirror { get; set; }
    public bool Traversable { get; init; }

    public required short UpperShade { get; set; }
    public required short LowerShade { get; init; }

}
