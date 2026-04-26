using RenderingEngine.Models;

namespace RenderingEngine.Engine;

internal static class TextureTransformHelper
{
    internal static BGRA[] RotateTexture(int height, int width, scoped Span<BGRA> img)
    {
        // build engine rotates textures for better memory locality
        BGRA[] output = new BGRA[height * width];
        ref BGRA inputPtr = ref MemoryMarshal.GetReference(img);
        ref BGRA outputPtr = ref MemoryMarshal.GetArrayDataReference(output);

        for (int y = height; y > 0; y--)
        {
            int newY = 0;
            int newX = height - y;

            for (int x = 0; x < width; x++)
            {
                int dstIndex = newY + newX;
                Unsafe.Add(ref outputPtr, dstIndex) = inputPtr;
                inputPtr = ref Unsafe.Add(ref inputPtr, 1);

                newY += height;
            }
        }

        return output;
    }

    internal static void ShadeInPlace(Span<BGRA> texture, int brightness)
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

    internal static BGRA[] FlipTextureY(int height, int width, scoped Span<BGRA> img)
    {
        BGRA[] flippedImage = new BGRA[img.Length];

        for (int x = 0, xh = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int i = xh + y;
                int j = xh + (height - y - 1);
                flippedImage[i] = img[j];
            }

            xh += height;
        }

        return flippedImage;
    }
}

[SkipLocalsInit]
internal static partial class TextureCache
{
    private const string FallBack = "-";
    private static readonly Dictionary<string, Texture> Cache = [];
    private static readonly Dictionary<int, BGRA[]> PalletteLookup = [];

    static TextureCache()
    {
        var data = new BGRA[128 * 128];
        data.AsSpan().Fill(BGRA.Green);

        Cache[FallBack] = new DoomTexture(128, 128, data);
    }

    public static void Add(string name, Texture texture)
    {
        name = name.ToUpperInvariant();
        Cache[name] = texture;
    }

    public static bool TextureExists(string name)
    {
        name = name.ToUpperInvariant();
        return Cache.ContainsKey(name);
    }

    public static Texture GetTexture(TextureInfo? textureInfo) => GetTexture(textureInfo?.Name);

    public static Texture GetTexture(string? name)
    {
        if (name == null)
        {
            return Cache[FallBack];
        }

        name = name.ToUpperInvariant();

        if (!Cache.TryGetValue(name, out Texture? texture))
        {
            texture = Cache[FallBack];
        }

        return texture;
    }

    public static void AddPallette(int id, BGRA[] lookup)
    {
        PalletteLookup[id] = lookup;
    }

    public static BGRA[] GetTexture(ReadOnlySpan<byte> lookup, int palletteId)
    {
        if (!PalletteLookup.ContainsKey(palletteId))
        {
            palletteId = 0;
        }

        ReadOnlySpan<BGRA> pallette = PalletteLookup[palletteId];
        BGRA[] texture = new BGRA[lookup.Length];

        for (int i = 0; i < lookup.Length; i++)
        {
            byte index = lookup[i];
            texture[i] = pallette[index];
        }

        return texture;
    }
}