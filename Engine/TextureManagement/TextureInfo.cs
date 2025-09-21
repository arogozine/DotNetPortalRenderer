using RenderingEngine.Models;

namespace RenderingEngine.TextureManagement;

public readonly ref struct TextureInfo
{
    public readonly int Width;
    public readonly int Height;
    public readonly ref BGRA Texture;

    public TextureInfo(int width, int height, ref BGRA texture)
    {
        Width = width;
        Height = height;
        Texture = ref texture;
    }
}
