namespace SoftwareRendererModels;

public sealed class DoomTexture : GameTexture
{
    public BGRA[] Texture { get; }

    public DoomTexture(string name, int width, int height, BGRA[] texture) : base(name, width, height)
    {
        Texture = texture;
    }
}
