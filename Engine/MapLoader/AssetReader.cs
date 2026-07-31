using RenderingEngine.Engine;
using SkiaSharp;
using SoftwareRendererModels;
using System.Numerics;

namespace RenderingEngine.MapLoader;

internal static class Precalculations
{
    public static void PrecalculateWallSprites(scoped ReadOnlySpan<Sprite> sprites)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            GameTextureInfo texture = sprite.Texture;

            (float width, float height) = texture.GetScaledDimensions();

            (float x, float y) = sprite.Location;

            // calculate the x, y for the wall on the screen for both points
            float rx1 = x - width * 0.5f;
            float rx2 = x + width * 0.5f;
            float ry1 = y;
            float ry2 = y;

            if (sprite is WallSprite wallSprite)
            {
                (float sin, float cos) = MathF.SinCos(wallSprite.Angle);

                (rx1, ry1) = SharedHelpers.RotateVertex(rx1, ry1, sin, cos, x, y);
                (rx2, ry2) = SharedHelpers.RotateVertex(rx2, ry2, sin, cos, x, y);

                rx1 += x;
                ry1 += y;
                rx2 += x;
                ry2 += y;
            }
            else if (sprite is FloorSprite floorSprite)
            {
                (float sin, float cos) = MathF.SinCos(floorSprite.Angle);

                float xFrom = x - width * 0.5f;
                float xTo = x + width * 0.5f;
                float yTo = y - height * 0.5f;
                float yFrom = y + height * 0.5f;

                floorSprite.PointA = SharedHelpers.RotateVertexAroundPoint(xFrom, yFrom, sin, cos, x, y);
                floorSprite.PointB = SharedHelpers.RotateVertexAroundPoint(xTo, yFrom, sin, cos, x, y);
                floorSprite.PointC = SharedHelpers.RotateVertexAroundPoint(xFrom, yTo, sin, cos, x, y);
                floorSprite.PointD = SharedHelpers.RotateVertexAroundPoint(xTo, yTo, sin, cos, x, y);

                continue;
            }

            sprite.Length = width;
            sprite.PointA = new Vector2(rx1, ry1);
            sprite.PointB = new Vector2(rx2, ry2);
        }
    }

    [Conditional("DEBUG")]
    private static unsafe void DebugTexture(int width, int height, Span<BGRA> texture, string textureName)
    {
        textureName = textureName.Replace("\\", "_");

        var info = new SKImageInfo(width, height)
        {
            AlphaType = SKAlphaType.Premul,
            ColorType = SKColorType.Bgra8888,
        };

        fixed (BGRA* bgraPtr = &texture[0])
        {
            SKImage? image = SKImage.FromPixels(info, (nint)bgraPtr, info.RowBytes);

            using SKData? data = image.Encode(SKEncodedImageFormat.Png, 100); // 100 = max quality
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"temp\\{textureName}.PNG");
            using (FileStream stream = File.OpenWrite(outputPath))
            {
                data.SaveTo(stream);
            }

            Console.WriteLine($"Image saved to {outputPath}");
        }
    }
}
