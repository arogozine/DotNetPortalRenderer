namespace RenderingEngine.Models
{
    internal sealed class TextureInfo
    {
        public required string Name { get; set; }
        public int XOffset { get; set; }
        public int YOffset { get; set; }
        public float Alpha { get; set; }
        public float? XScale { get; set; }
        public float? YScale { get; set; }
        public TextureRenderingOptions RenderingOptions { get; set; }
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
        AlignWithFirstWall = 256,
        RenderAsWall = 512,
        RenderAsFloor = 1024
    }

    internal static class TextureRenderingOptionsExtensions
    {
        extension(TextureRenderingOptions options)
        {
            public bool IsWall => options.HasFlag(TextureRenderingOptions.RenderAsWall);
            public bool IsSkybox => options.HasFlag(TextureRenderingOptions.Skybox);
            public bool IsFlippedX => options.HasFlag(TextureRenderingOptions.FlipX);
            public bool IsFlippedY => options.HasFlag(TextureRenderingOptions.FlipY);
            public bool IsFloor => options.HasFlag(TextureRenderingOptions.RenderAsFloor);
        }
    }
}
