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

    private static unsafe BGRA[] RotateTexture(int height, int width, Span<BGRA> input)
    {
        // build engine rotates textures for faster access or something like that
        BGRA[] output = new BGRA[height * width];
        ref BGRA inputPtr = ref MemoryMarshal.GetReference(input);
        ref BGRA outputPtr = ref MemoryMarshal.GetArrayDataReference(output);

        for (int y = height; y > 0; y--)
        {
            int newY = 0;

            for (int x = 0; x < width; x++)
            {
                int newX = height - y;
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
        ref Texture texture = ref CollectionsMarshal.GetValueRefOrNullRef(Cache, name);
        return !Unsafe.IsNullRef(ref texture);
    }

    public static ref Texture GetTextureOrNullRef(string? name)
    {
        if (name == null)
        {
            return ref GetFallBack();
        }

        name = name.ToUpperInvariant();

        return ref CollectionsMarshal.GetValueRefOrNullRef(Cache, name);
    }

    public static ref Texture GetTexture(string? name)
    {
        if (name == null)
        {
            return ref GetFallBack();
        }

        name = name.ToUpperInvariant();

        ref Texture texture = ref CollectionsMarshal.GetValueRefOrNullRef(Cache, name);

        if (Unsafe.IsNullRef(ref texture))
        {
            return ref GetFallBack();
        }

        return ref texture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref Texture GetFallBack() => ref CollectionsMarshal.GetValueRefOrNullRef(Cache, FallBack);
}