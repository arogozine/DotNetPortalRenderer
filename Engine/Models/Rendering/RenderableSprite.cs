namespace RenderingEngine.Models
{
    internal sealed class RenderableSprite
    {
        public required Sprite Sprite { get; set; }


        public int Id => Sprite.Id;
        public int SectorId => Sprite.SectorId;
        public Point Location => Sprite.Location;
        public Point PointA => Sprite.PointA;
        public Point PointB => Sprite.PointB;
        public float Angle => Sprite.Angle;
        public float Height => Sprite.Height;
        public TextureInfo Texture => Sprite.Texture;
        public float Length => Sprite.Length;


        public bool IntersectsView { get; set; }
        public bool Flipped { get; set; }

        public Point Rotated { get; set; }
        public Point R1 { get; set; }
        public Point R2 { get; set; }


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
