using RenderingEngine.Engine;

namespace RenderingEngine.Models;

internal enum TextureTransform : byte
{
    Normal,
    Rotated,
    RotatedFlipped
}

internal abstract class Texture
{
    public int Width { get; }
    public int Height { get; }

    protected readonly Dictionary<int, BGRA[]> PalletteToImage = [];
    protected readonly Dictionary<int, BGRA[]> PalletteToImageRotated = [];
    protected readonly Dictionary<int, BGRA[]> PalletteToImageRotatedFlipped = [];

    public Texture(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public ref T GetBinaryRef<T>(int shade, TextureTransform transform)
        where T : unmanaged
    {
        Span<T> span = MemoryMarshal.Cast<BGRA, T>(GetBinary(shade, transform));
        return ref MemoryMarshal.GetReference(span);
    }

    private BGRA[] GetOrAddNormal(int shade)
    {
        if (PalletteToImage.TryGetValue(shade, out BGRA[]? value))
        {
            return value;
        }

        value = CalculateTexture(shade);
        PalletteToImage[shade] = value;

        return value;
    }

    private BGRA[] GetOrAddRotated(int shade)
    {
        if (PalletteToImageRotated.TryGetValue(shade, out BGRA[]? value))
        {
            return value;
        }

        value = CalculateRotatedTexture(shade);
        PalletteToImageRotated[shade] = value;

        return value;
    }

    private BGRA[] GetOrAddRotatedFlipped(int shade)
    {
        if (PalletteToImageRotatedFlipped.TryGetValue(shade, out BGRA[]? value))
        {
            return value;
        }

        value = CalculateRotatedFlippedTexture(shade);
        PalletteToImageRotatedFlipped[shade] = value;

        return value;
    }

    public Span<BGRA> GetBinary(int shade, TextureTransform transform)
    {
        return transform switch
        {
            TextureTransform.Normal => (Span<BGRA>)GetOrAddNormal(shade),
            TextureTransform.Rotated => (Span<BGRA>)GetOrAddRotated(shade),
            TextureTransform.RotatedFlipped => (Span<BGRA>)GetOrAddRotatedFlipped(shade),
            _ => throw new NotImplementedException(),
        };
    }

    public virtual BGRA[] CalculateRotatedFlippedTexture(int palletteId)
    {
        Span<BGRA> img = GetOrAddRotated(palletteId);

        return TextureTransformHelper.FlipTextureY(this.Height, this.Width, img);
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
        BGRA[] texture = TextureTransformHelper.RotateTexture(Height, Width, _texture);

        TextureTransformHelper.ShadeInPlace(texture, brightness);

        return texture;
    }

    protected override BGRA[] CalculateTexture(int brightness)
    {
        BGRA[] texture = new BGRA[_texture.Length];
        _texture.AsSpan().CopyTo(texture);

        TextureTransformHelper.ShadeInPlace(texture, brightness);

        return texture;
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
        return TextureTransformHelper.RotateTexture(Height, Width, texture);
    }

    protected override BGRA[] CalculateTexture(int palletteId)
    {
        return TextureCache.GetTexture(_lookup, palletteId);
    }
}