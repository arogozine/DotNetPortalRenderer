using RenderingEngine.Engine;

namespace RenderingEngine.Models;

[Flags]
internal enum TextureTransform : byte
{
    Normal = 0,
    Rotated = 1,
    FlippedY = 2,
    FlippedX = 4,
    RotatedFlipped = Rotated | FlippedY,
    All = Rotated | FlippedX | FlippedY

}

internal abstract class Texture
{
    public int Width { get; }
    public int Height { get; }

    protected readonly Dictionary<int, BGRA[]>[] TransformToPallette = new Dictionary<int, BGRA[]>[1 + (int)TextureTransform.All];

    public Texture(int width, int height)
    {
        Width = width;
        Height = height;

        for (int i = 0; i < TransformToPallette.Length; i++)
        {
            TransformToPallette[i] = [];
        }
    }

    public ref T GetBinaryRef<T>(int shade, TextureTransform transform)
        where T : unmanaged
    {
        Span<T> span = MemoryMarshal.Cast<BGRA, T>(GetBinary(shade, transform));
        return ref MemoryMarshal.GetReference(span);
    }

    private BGRA[] GetOrAddNormal(int shade)
    {
        Dictionary<int, BGRA[]> pallette = TransformToPallette[(int)TextureTransform.Normal];

        if (pallette.TryGetValue(shade, out BGRA[]? value))
        {
            return value;
        }

        value = CalculateTexture(shade);
        pallette[shade] = value;

        return value;
    }

    private Span<BGRA> GetBinary(int shade, TextureTransform transform)
    {
        Dictionary<int, BGRA[]> pallette = TransformToPallette[(int)transform];

        if (pallette.TryGetValue(shade, out BGRA[]? texture))
        {
            return texture;
        }

        texture = GetOrAddNormal(shade);

        if (transform == TextureTransform.Normal)
        {
            pallette[shade] = texture;
            return texture;
        }

        if (transform.HasFlag(TextureTransform.FlippedX))
        {
            texture = TextureTransformHelper.FlipTextureX(Height, Width, texture);
        }

        if (transform.HasFlag(TextureTransform.FlippedY))
        {
            texture = TextureTransformHelper.FlipTextureY(Height, Width, texture);
        }

        if (transform.HasFlag(TextureTransform.Rotated))
        {
            texture = TextureTransformHelper.RotateTexture(Height, Width, texture);
        }

        pallette[shade] = texture;
        return texture;
    }

    protected abstract BGRA[] CalculateTexture(int palletteId);
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

    protected override BGRA[] CalculateTexture(int palletteId)
    {
        return TextureCache.GetTexture(_lookup, palletteId);
    }
}