namespace RenderingEngine.Models
{
    internal sealed class TextureInfo
    {
        public required string Name { get; set; }
        public int XOffset { get; set; }
        public int YOffset { get; set; }
        public float Alpha { get; set; }
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
        Skybox = 16
    }
}
