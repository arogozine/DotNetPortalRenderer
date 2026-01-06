namespace RenderingEngine.Models;

internal sealed class Texture
{
    public int Width { get; }
    public int Height { get; }
    public BGRA[] Data { get; }
    public BGRA[] Rotated { get; }

    public Texture(int width, int height, BGRA[] data, BGRA[] rotated)
    {
        Width = width;
        Height = height;
        Data = data;
        Rotated = rotated;
    }
}
