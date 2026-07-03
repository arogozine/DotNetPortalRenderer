namespace SoftwareRendererModels;

public sealed class GameTextureInfo : IFixedState
{
    public required GameTexture Texture { get; set; }
    public string Name => Texture.Name;
    public int Width => Texture.Width;
    public int Height => Texture.Height;
    public required int XOffset { get; set; }
    public required int YOffset { get; set; }
    public required float Alpha { get; set; }
    public required float? XScale { get; set; }
    public required float? YScale { get; set; }
    public bool YUntiled { get; set; }
    public required int Palette { get; set; }
    public required TextureRenderingOptions RenderingOptions { get; set; }
}
