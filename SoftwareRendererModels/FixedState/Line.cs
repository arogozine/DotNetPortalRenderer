namespace SoftwareRendererModels;

public sealed class Line : IFixedState
{
    public required int Id { get; init; }
    public required LineVector PointA { get; set; }
    public required LineVector PointB { get; set; }
    public required int? SectorTo { get; set; }

    public required GameTextureInfo? UpperTexture { get; set; }
    public required GameTextureInfo? MiddleTexture { get; set; }
    public required GameTextureInfo? LowerTexture { get; set; }

    public bool TwoSided { get; set; }
    public bool IsMirror { get; set; }

    public required short UpperShade { get; set; }
    public required short LowerShade { get; set; }

}
