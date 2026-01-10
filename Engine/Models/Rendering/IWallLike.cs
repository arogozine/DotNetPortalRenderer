namespace RenderingEngine.Models.Rendering
{
    internal interface IWallLike
    {
        public Point R1 { get; }
        public Point R2 { get; }
        public bool IntersectsView { get; }
        public float Length { get; }
        public bool Flipped { get; }
        public int XLeft { get; }
        public int XRight { get; }
        public int YLeftCeil { get; }
        public int YLeftFloor { get; }
        public int YRightCeil { get; }
        public int YRightFloor { get; }
    }
}
