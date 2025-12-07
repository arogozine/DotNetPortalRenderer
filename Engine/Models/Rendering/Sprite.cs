namespace RenderingEngine.Models
{
    public sealed class Sprite
    {
        public int SectorId { get; set; }
        public required Point Location { get; init; }

        public Point Rotated { get; set; }

        public Point R1 { get; set; }
        public Point R2 { get; set; }

        public required float Angle { get; init; }
        public required float Height { get; init; }
        public required string TextureName { get; init; }

        public bool IntersectsView { get; set; }
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
