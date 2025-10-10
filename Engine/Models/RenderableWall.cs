namespace RenderingEngine.Models
{
    internal sealed class RenderableWall
    {
        public required Wall Wall { get; set; }
        public required int XLeft { get; set; }
        public required int XRight { get; set; }
        public required int Offset { get; set; }
    }
}
