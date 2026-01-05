namespace RenderingEngine.Models
{
    internal sealed class Sprite
    {
        public int SectorId { get; set; }

        public required Point Location { get; init; }
        public Point PointA { get; set; }
        public Point PointB { get; set; }

        public bool IntersectsView { get; set; }
        public bool Flipped { get; set; }

        public Point Rotated { get; set; }
        public Point R1 { get; set; }
        public Point R2 { get; set; }

        public float Length { get; set; }

        public required float Angle { get; init; }
        public required float Height { get; init; }
        public required TextureInfo Texture { get; init; }

        // Plane
        public int XLeft { get; set; }
        public int XRight { get; set; }
        public int YLeftCeil { get; set; }
        public int YLeftFloor { get; set; }
        public int YRightCeil { get; set; }
        public int YRightFloor { get; set; }
        public float Distance { get; set; }

    }
}
