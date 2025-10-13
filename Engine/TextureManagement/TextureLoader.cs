using RenderingEngine.Models;
using SkiaSharp;

namespace RenderingEngine.TextureManagement;

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

internal static class TextureCache
{
    private static readonly Dictionary<string, Texture> Cache = [];

    public static void Add(string name, int width, int height, BGRA[] data)
    {
        BGRA[] rotated = RotateTexture(height, width, data);
        Cache[name] = new Texture(width, height, data, rotated);
    }

    private static unsafe BGRA[] RotateTexture(int height, int width, Span<BGRA> input)
    {
        BGRA[] output = new BGRA[height * width];
        ref BGRA inputPtr = ref MemoryMarshal.GetReference(input);
        ref BGRA outputPtr = ref MemoryMarshal.GetReference(output.AsSpan());

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int newX = height - 1 - y;
                int newY = x;
                int dstIndex = newY * height + newX;
                Unsafe.Add(ref outputPtr, dstIndex) = inputPtr;
                inputPtr = ref Unsafe.Add(ref inputPtr, 1);
                
            }
        }

        return output;
    }

    public static Texture GetTexture(string? name)
    {
        if (name == null)
        {
            return Cache.Values.First();
        }

        ref Texture texture = ref CollectionsMarshal.GetValueRefOrNullRef(Cache, name);

        if (Unsafe.IsNullRef(ref texture))
        {
            return Cache.Values.First();
        }

        return texture;
    }

    public static TextureInfo GetTexture(string? name, bool rotated)
    {
        if (name == null)
        {
            return TextureLoader.GetTexture(TextureName.Brick, rotated);
        }

        ref Texture texture = ref CollectionsMarshal.GetValueRefOrNullRef(Cache, name);

        if (Unsafe.IsNullRef(ref texture))
        {
            return TextureLoader.GetTexture(TextureName.Brick, rotated);
        }

        if (rotated)
        {
            return new TextureInfo(texture.Height, texture.Width, ref MemoryMarshal.GetArrayDataReference(texture.Rotated));
        }
        else
        {
            return new TextureInfo(texture.Width, texture.Height, ref MemoryMarshal.GetArrayDataReference(texture.Data));
        }
    }
}

internal static class TextureLoader
{
    private const string Textures = "Textures";
    private const string BrickPath = $"{Textures}/Brick.png";
    private const string RockPath = $"{Textures}/Rock.png";
    private const string CaveGroundPath = $"{Textures}/CaveGround.png";
    private const string CaveCeilingPath = $"{Textures}/CaveCeiling.png";
    private const string BarrelPath = $"{Textures}/Barrel.png";
    private const string CeilingOffice = $"{Textures}/CeilingOffice.png";

    private static readonly byte[][] TextureCache;
    private static readonly byte[][] RotatedTextureCache;

    static TextureLoader()
    {
        const int textureCount = 6;

        TextureCache = new byte[textureCount][];
        TextureCache[(int)TextureName.Brick] = LoadTexture(BrickPath);
        TextureCache[(int)TextureName.Rock] = LoadTexture(RockPath);
        TextureCache[(int)TextureName.CaveGround] = LoadTexture(CaveGroundPath);
        TextureCache[(int)TextureName.CaveCeiling] = LoadTexture(CaveCeilingPath);
        TextureCache[(int)TextureName.Barrel] = LoadTexture(BarrelPath);
        TextureCache[(int)TextureName.CeilingOffice] = LoadTexture(CeilingOffice);

        RotatedTextureCache = new byte[textureCount][];

        for (int i = 0; i < textureCount; i++)
        {
            RotatedTextureCache[i] = RotateTexture(64, 64, TextureCache[i]);
        }
    }

    public static TextureInfo GetTexture(TextureName name, bool rotated)
    {
        if (rotated)
        {
            ref byte[] texture = ref TextureCache[(int)name];
            return new TextureInfo(64, 64, ref Unsafe.As<byte, BGRA>(ref texture[0]));
        }
        else
        {
            ref byte[] texture = ref RotatedTextureCache[(int)name];
            return new TextureInfo(64, 64, ref Unsafe.As<byte, BGRA>(ref texture[0]));
        }
    }

    private static byte[] LoadTexture(string pngFilePath)
    {
        using Stream imageStreamSource = new FileStream(pngFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using SKBitmap bitmap = SKBitmap.Decode(imageStreamSource);
        return bitmap.Bytes;
    }

    private static unsafe byte[] RotateTexture(int height, int width, byte[] input)
    {
        byte[] output = new byte[height * width * sizeof(BGRA)];

        fixed (byte* outPtr = &output[0])
        fixed (byte* inPtr = &input[0])
        {
            BGRA* inBgraPtr = (BGRA*)inPtr;
            BGRA* outBgraPtr = (BGRA*)outPtr;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int newX = height - 1 - y;
                    int newY = x;
                    int dstIndex = newY * height + newX;
                    outBgraPtr[dstIndex] = *inBgraPtr;
                    inBgraPtr++;
                }
            }
        }

        return output;
    }
}
