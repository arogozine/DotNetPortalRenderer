using RenderingEngine.Engine;

namespace RenderingEngine.Models
{
    internal abstract class RenderableSprite
    {
        public required int XLeft { get; init; }
        public required int XRight { get; init; }
    }

    internal class TransparentWall : RenderableSprite
    {
        public required int Offset { get; init; }
        public required Wall Wall { get; set; }
        public required Sector Sector { get; init; }
        public required RenderWindow[] RenderWindow { get; init; }
    }

    internal class SectorSprites : RenderableSprite
    {
        public required int RenderDepth { get; init; }
        public required int[] CeilingStart { get; init; }
        public required int[] FloorEnd { get; init; }
        public required float[] Distance { get; init; }
    }
}
