namespace SoftwareRendererModels;

public sealed class BuildTexture : GameTexture
{
    public byte[] Lookup { get; }


    public BuildTexture(string name, int width, int height, byte[] lookup) : base(name, width, height)
    {
        Lookup = lookup ;
    }
}