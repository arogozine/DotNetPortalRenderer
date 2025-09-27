namespace RenderingEngine.Models
{
    internal sealed class RenderableWall
    {
        public required int XLeft { get; set; }
        public required int XRight { get; set; }
        public required Wall Wall { get; set; }

        public int Offset { get; set; }

        public static RenderableWall FromWall(Wall wall)
        {
            return new RenderableWall { Wall = wall, XLeft = wall.XLeft, XRight = wall.XRight };
        }
    }
}
