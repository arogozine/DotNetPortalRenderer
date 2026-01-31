using RenderingEngine.Engine;

namespace RenderingEngine.Models;

internal abstract class Texture
{
    public int Width { get; }
    public int Height { get; }

    protected readonly Dictionary<int, BGRA[]> PalletteToImage = [];
    protected readonly Dictionary<int, BGRA[]> PalletteToImageRotated = [];
    protected readonly byte[] _lookup;

    public Texture(int width, int height, byte[] lookup)
    {
        Width = width;
        Height = height;
        _lookup = lookup;
    }

    public Span<BGRA> GetBinary(bool rotated, int palletteId)
    {
        if (rotated)
        {
            if (PalletteToImageRotated.TryGetValue(palletteId, out BGRA[]? value))
            {
                return value;
            }

            value = CalculateRotatedTexture(palletteId);
            PalletteToImageRotated[palletteId] = value;
            return value;
        }
        else
        {
            if (PalletteToImage.TryGetValue(palletteId, out BGRA[]? value))
            {
                return value;
            }

            value = CalculateTexture(palletteId);
            PalletteToImage[palletteId] = value;
            return value;
        }
    }

    protected abstract BGRA[] CalculateTexture(int palletteId);
    protected abstract BGRA[] CalculateRotatedTexture(int palletteId);
}

internal class DoomTexture : Texture
{
    public DoomTexture(int width, int height, byte[] lookup) : base(width, height, lookup)
    {
    }

    protected override BGRA[] CalculateRotatedTexture(int palletteId)
    {
        BGRA[] texture = TextureCache.GetTexture(_lookup, 0);
        Shade(texture, palletteId);

        return TextureCache.RotateTexture(Height, Width, texture);
    }

    protected override BGRA[] CalculateTexture(int palletteId)
    {
        BGRA[] texture = TextureCache.GetTexture(_lookup, 0);
        Shade(texture, palletteId);

        return texture;
    }

    private static void Shade(Span<BGRA> texture, int brightness)
    {
        const uint Alpha = (uint)byte.MaxValue << 24;

        if (brightness == byte.MinValue)
        {
            texture.Fill(Alpha);
            return;
        }

        uint scale = (uint)brightness;
        for (int i = 0; i < texture.Length; i++)
        {
            BGRA value = texture[i];
            uint b = value.B * scale >> 8;
            uint g = value.G * scale >> 8 << 8;
            uint r = value.R * scale >> 8 << 16;
            texture[i] = new BGRA(b | g | r | Alpha);
        }
    }
}

internal class BuildTexture : Texture
{
    public BuildTexture(int width, int height, byte[] lookup) : base(width, height, lookup)
    {
    }

    protected override BGRA[] CalculateRotatedTexture(int palletteId)
    {
        BGRA[] texture = TextureCache.GetTexture(_lookup, palletteId);
        return TextureCache.RotateTexture(Height, Width, texture);
    }

    protected override BGRA[] CalculateTexture(int palletteId)
    {
        return TextureCache.GetTexture(_lookup, palletteId);
    }
}