using RenderingEngine.Engine;

namespace RenderingEngine.Models
{
    internal abstract class RenderableSpriteSnapshot
    {
        public required int XLeft { get; init; }
        public required int XRight { get; init; }
    }

    /// <summary>
    /// Render Window Snapshot for a certain depth. For transparent wall rendering.
    /// </summary>
    internal class RenderWindowWallSnapshot : RenderableSpriteSnapshot
    {
        public required int Offset { get; init; }
        public required RenderableWall Wall { get; set; }
        public required RenderWindow[] RenderWindow { get; init; }
    }

    /// <summary>
    /// Render Window Snapshot for a certain depth. For sprite rendering.
    /// </summary>
    internal class RenderWindowSpriteSnapshot : RenderableSpriteSnapshot
    {
        public required int RenderDepth { get; init; }
        public required int[] CeilingStart { get; init; }
        public required int[] FloorEnd { get; init; }
        public required float[] Distance { get; init; }
    }
}
