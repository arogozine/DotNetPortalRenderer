using RenderingEngine.Engine;

namespace RenderingEngine.Models
{
    internal sealed class RenderablePortalWall
    {
        public required RenderableWall Wall { get; set; }
        public required int XLeft { get; set; }
        public required int XRight { get; set; }
        public required int Offset { get; set; }
        public RenderableWall[]? ParentWalls { get; set; }
        public RenderColumnStatus RenderColumnStatus { get; set; }
        public RenderableWall? MirrorWall { get; set; }


        public bool IsPortalWithMiddleTexture => Wall.IsPortal && Wall.MiddleTexture != null;
    }
}
