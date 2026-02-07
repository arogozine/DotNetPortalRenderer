using RenderingEngine.Models.Rendering;

namespace RenderingEngine.Models
{
    internal sealed class FloorSpriteWallInfo
    {
        public required bool IntersectsView { get; set; }
        public int XLeft { get; set; }
        public int XRight { get; set; }
        public int YLeftFloor { get; set; }
        public int YRightFloor { get; set; }
    }

    internal abstract class RenderableSprite
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
        public short? Shade => Sprite.Shade;


        public bool IntersectsView { get; set; }
        public Point Rotated { get; set; }

        public Point R1 { get; set; }
        public Point R2 { get; set; }

        public float DistanceMax { get; set; }
        public float DistanceMin { get; set; }
        public bool Flipped { get; set; }
        public int XLeft { get; set; }
        public int XRight { get; set; }
    }

    internal sealed class RenderableBasicSprite : RenderableSprite, IWallLike
    {
        public int YLeftCeil { get; set; }
        public int YLeftFloor { get; set; }
        public int YRightCeil { get; set; }
        public int YRightFloor { get; set; }
    }

    internal sealed class RenderableWallSprite : RenderableSprite, IWallLike
    {
        public int YLeftCeil { get; set; }
        public int YLeftFloor { get; set; }
        public int YRightCeil { get; set; }
        public int YRightFloor { get; set; }
        public bool? TwoSided => ((WallSprite)Sprite).TwoSided;
    }

    internal class RenderableFloorSprite : RenderableSprite
    {
        public Point PointC => ((FloorSprite)Sprite).PointC;
        public Point PointD => ((FloorSprite)Sprite).PointD;

        public Point R3 { get; set; }
        public Point R4 { get; set; }

        public FloorSpriteWallInfo? Wall1 { get; set; }
        public FloorSpriteWallInfo? Wall2 { get; set; }
        public FloorSpriteWallInfo? Wall3 { get; set; }
        public FloorSpriteWallInfo? Wall4 { get; set; }
    }
}
