using RenderingEngine.Engine;

namespace RenderingEngine.Models
{
    internal sealed class TextureInfo
    {
        public Texture Texture {
            get {
                return (field ??= TextureCache.GetTexture(Name));
            }
        }

        public int Width => Texture.Width;
        public int Height => Texture.Height;

        public required string Name { get; set; }
        public int XOffset { get; set; }
        public int YOffset { get; set; }
        public float Alpha { get; set; }
        public float? XScale { get; set; }
        public float? YScale { get; set; }
        public TextureRenderingOptions RenderingOptions { get; set; }

        public (float Width, float Height) GetScaledDemensions()
        {
            (float xScale, float yScale) = GetScale();

            return (Width * xScale, Height * yScale);
        }

        public (float XScale, float YScale) GetScale()
        {
            return (XScale ?? 1f, YScale ?? 1f);
        }
    }

    [Flags]
    public enum TextureRenderingOptions
    {
        None = 0,
        FromTop = 1,
        FromSectorTop = 2,
        FromBottom = 4,
        FromSectorBottom = 8,
        Skybox = 16,
        FlipX = 32,
        FlipY = 64,
        SwapXY = 128,
        AlignWithFirstWall = 256
    }

    internal static class TextureRenderingOptionsExtensions
    {
        extension(TextureRenderingOptions options)
        {
            public bool IsSkybox => options.HasFlag(TextureRenderingOptions.Skybox);
            public bool IsFlippedX => options.HasFlag(TextureRenderingOptions.FlipX);
            public bool IsFlippedY => options.HasFlag(TextureRenderingOptions.FlipY);
            public bool IsSwappedXY => options.HasFlag(TextureRenderingOptions.SwapXY);
            public bool IsAlignedWithWall => options.HasFlag(TextureRenderingOptions.AlignWithFirstWall);
        }
    }
}
