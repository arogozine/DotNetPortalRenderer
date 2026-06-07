namespace SoftwareRendererModels;

public abstract class GameTexture : IFixedState, IUniqueName
{
    public Dictionary<int, Dictionary<int, BGRA[]>[]> PaletteToTransformToImage { get; }

    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public short LeftOffset { get; init; }
    public short TopOffset { get; init; }

    public GameTexture(string name, int width, int height)
    {
        Name = name;
        Width = width;
        Height = height;

        PaletteToTransformToImage = [];
        PaletteToTransformToImage[0] = new Dictionary<int, BGRA[]>[1 + (int)TextureTransform.All];

        for (int i = 0; i < PaletteToTransformToImage[0].Length; i++)
        {
            PaletteToTransformToImage[0][i] = [];
        }
    }
}