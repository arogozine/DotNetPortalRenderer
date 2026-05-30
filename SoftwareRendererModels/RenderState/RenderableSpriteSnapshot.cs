namespace SoftwareRendererModels;

public abstract class RenderableSpriteSnapshot : IRenderState
{
    public required int XLeft { get; init; }
    public required int XRight { get; init; }
    public required int Depth { get; init; }
}