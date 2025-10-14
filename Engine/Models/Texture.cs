namespace RenderingEngine.Models;

internal readonly struct Texture
{
    public readonly int Width;
    public readonly int Height;
    public readonly BGRA[] Data;
    public readonly BGRA[] Rotated;

    public Texture(int width, int height, BGRA[] data, BGRA[] rotated)
    {
        Width = width;
        Height = height;
        Data = data;
        Rotated = rotated;
    }
}
