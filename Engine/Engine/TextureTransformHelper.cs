using SoftwareRendererModels;

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

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = x + y * width;
                int j = x + (height - y - 1) * width;
                flippedImage[i] = img[j];
            }
        }

        return flippedImage;
    }

    internal static BGRA[] FlipTextureX(int height, int width, scoped Span<BGRA> img)
    {
        BGRA[] flippedImage = new BGRA[img.Length];

        for (int y = 0, i = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = i + x;
                int j = i + (width - x - 1);
                flippedImage[index] = img[j];
            }

            i += width;
        }

        return flippedImage;
    }
}
