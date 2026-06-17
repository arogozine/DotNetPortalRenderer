namespace SoftwareRendererModels;

public abstract class RenderableSpriteSnapshot : IRenderState
{
    public int XLeft { get; set; }
    public int XRight { get; set; }
    public int Depth { get; set; }
}