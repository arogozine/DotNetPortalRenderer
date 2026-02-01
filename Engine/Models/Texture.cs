using RenderingEngine.Engine;

namespace RenderingEngine.Models;

internal abstract class Texture
{
    public int Width { get; }
    public int Height { get; }

    protected readonly Dictionary<int, BGRA[]> PalletteToImage = [];
    protected readonly Dictionary<int, BGRA[]> PalletteToImageRotated = [];

    public Texture(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public ref T GetBinaryRef<T>(bool rotated, int shade)
        where T : unmanaged
    {
        Span<T> span = MemoryMarshal.Cast<BGRA, T>(GetBinary(rotated, shade));
        return ref MemoryMarshal.GetReference(span);
    }

    public Span<BGRA> GetBinary(bool rotated, int shade)
    {
        if (rotated)
        {
            if (PalletteToImageRotated.TryGetValue(shade, out BGRA[]? value))
            {
                return value;
            }

            value = CalculateRotatedTexture(shade);
            PalletteToImageRotated[shade] = value;
            return value;
        }
        else
        {
            if (PalletteToImage.TryGetValue(shade, out BGRA[]? value))
            {
                return value;
            }

            value = CalculateTexture(shade);
            PalletteToImage[shade] = value;
            return value;
        }
    }

    protected abstract BGRA[] CalculateTexture(int palletteId);
    protected abstract BGRA[] CalculateRotatedTexture(int palletteId);
}

internal abstract class PalletteTexture : Texture
{
    protected readonly byte[] _lookup;

    public PalletteTexture(int width, int height, byte[] lookup)
        : base (width, height)
    {;
        _lookup = lookup;
    }
}

internal class DoomTexture : Texture
{
    private readonly BGRA[] _texture;

    public DoomTexture(int width, int height, BGRA[] texture)
        : base(width, height)
    {
        _texture = texture;
    }

    protected override BGRA[] CalculateRotatedTexture(int brightness)
    {
        BGRA[] texture = TextureCache.RotateTexture(Height, Width, _texture);

        Shade(texture, brightness);

        return texture;
    }

    protected override BGRA[] CalculateTexture(int brightness)
    {
        BGRA[] texture = new BGRA[_texture.Length];
        _texture.AsSpan().CopyTo(texture);

        Shade(texture, brightness);

        return texture;
    }

    private static void Shade(Span<BGRA> texture, int brightness)
    {
        const uint Alpha = (uint)byte.MaxValue << 24;

        uint scale = (uint)brightness;

        for (int i = 0; i < texture.Length; i++)
        {
            BGRA value = texture[i];

            if (value.Value == 0L)
            {
                continue;
            }

            uint b = value.B * scale >> 8;
            uint g = value.G * scale >> 8 << 8;
            uint r = value.R * scale >> 8 << 16;
            texture[i] = new BGRA(b | g | r | Alpha);
        }
    }
}

internal class BuildTexture : PalletteTexture
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