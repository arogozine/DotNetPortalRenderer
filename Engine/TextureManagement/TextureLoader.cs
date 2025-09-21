using RenderingEngine.Models;
using SkiaSharp;
using System.Runtime.CompilerServices;

namespace RenderingEngine.TextureManagement;

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
            RotatedTextureCache[i] = RotateTexture(TextureCache[i]);
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

    private static unsafe byte[] RotateTexture(byte[] input)
    {
        const int height = 64;
        const int width = 64;

        byte[] output = new byte[64 * 64 * sizeof(BGRA)];

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
