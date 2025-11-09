using RenderingEngine.Engine;

namespace RenderingEngine.Models
{
    internal sealed class RenderableWall
    {
        public bool SpritesOnly { get; set; }

        public required Wall Wall { get; set; }
        public required int XLeft { get; set; }
        public required int XRight { get; set; }
        public required int Offset { get; set; }
        public required Sector Sector { get; set; }
        public RenderWindow[]? RenderWindow { get; set; }

        public bool IsPortalWithMiddleTexture => Wall.IsPortal && Wall.Line.MiddleTexture != null;
    }
}
