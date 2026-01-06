using RenderingEngine.Engine;

namespace RenderingEngine.Models
{
    internal sealed class RenderablePortalWall
    {
        public bool SpritesOnly { get; set; }

        public required RenderableWall Wall { get; set; }
        public required int XLeft { get; set; }
        public required int XRight { get; set; }
        public required int Offset { get; set; }
        public RenderWindow[]? RenderWindow { get; set; }
        public RenderableWall[]? ParentWalls { get; set; }
        public RenderColumnStatus RenderColumnStatus { get; set; }


        public bool IsPortalWithMiddleTexture => Wall.IsPortal && Wall.MiddleTexture != null;
    }
}
