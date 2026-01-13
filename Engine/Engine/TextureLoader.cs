using RenderingEngine.Models;

namespace RenderingEngine.Engine;

[SkipLocalsInit]
internal static class TextureCache
{
    private const string FallBack = "-";
    private static readonly Dictionary<string, Texture> Cache = [];

    static TextureCache()
    {
        var data = new BGRA[128 * 128];
        data.AsSpan().Fill(BGRA.Green);

        Cache[FallBack] = new Texture(128, 128, data, data);
    }

    public static void Add(string name, int width, int height, BGRA[] data)
    {
        name = name.ToUpperInvariant();

        BGRA[] rotated = RotateTexture(height, width, data);
        Cache[name] = new Texture(width, height, data, rotated);
    }

    private static BGRA[] RotateTexture(int height, int width, scoped Span<BGRA> input)
    {
        // build engine rotates textures for better memory locality
        BGRA[] output = new BGRA[height * width];
        ref BGRA inputPtr = ref MemoryMarshal.GetReference(input);
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
}