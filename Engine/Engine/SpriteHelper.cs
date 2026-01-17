using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed class SectorInSectorComparer : IComparer<Sector>
    {
        public int Compare(Sector? x, Sector? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            if (IsSectorInSector(x, y))
            {
                return -1;
            }

            if (IsSectorInSector(y, x))
            {
                return 1;
            }

            return 0;
        }

        public static bool IsSectorInSector(Sector a, Sector b)
        {
            for (int i = 0; i < a.Walls.Length; i++)
            {
                RenderableWall wall = a.Walls[i];

                if (!SharedHelpers.IsPointInPolygon(b.Walls, wall.R1))
                {
                    return false;
                }

                if (!SharedHelpers.IsPointInPolygon(b.Walls, wall.R2))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal class SpriteHelper
    {
        private readonly int width;
        private readonly int height;
        private readonly float cameraPlaneX;

        public SpriteHelper(
            int width,
            int height,
            float cameraPlaneX)
        {
            this.width = width;
            this.height = height;
            this.cameraPlaneX = cameraPlaneX;
        }

        public Span<RenderableSprite> GetSpritesForPlayer(PortalPlayerSnapshot player, Span<RenderableSprite> sprites, ReadOnlySpan<Sector> sectors)
        {
            Span<RenderableSprite> rotatedSprites = RotateSprites(sprites, player);

            FilterOutSpritesBehindPlayer(ref rotatedSprites);
            FilterOutSpritesWithoutSector(ref rotatedSprites);

            float yaw = player.Yaw;
            float pz = player.Z;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                RenderableSprite sprite = rotatedSprites[i];

                Sector sector = sectors[sprite.SectorId];
                float yCeil = sector.Ceil - pz + sprite.Height;
                float yFloor = sector.Floor - pz + sprite.Height;

                CalculateSpritePlane(sprite, yCeil, yFloor, yaw);
            }

            FilterOutNonIntersectingSprites(ref rotatedSprites);
            AssignDistance(rotatedSprites);

            rotatedSprites.Sort(new SpriteComparer());

            return rotatedSprites;
        }

        public static void AssignSectors(scoped ReadOnlySpan<RenderableSprite> sprites, scoped ReadOnlySpan<Sector> sectors)
        {
            for (int j = 0; j < sprites.Length; j++)
            {
                RenderableSprite sprite = sprites[j];
                List<Sector> potentialSectors = [];

                for (int i = sectors.Length - 1; i >= 0; i--)
                {
                    Sector sector = sectors[i];

                    if (SharedHelpers.IsPointInPolygon(sector.Walls, sprite.Location))
                    {
                        potentialSectors.Add(sector);
                    }
                }

                if (potentialSectors.Count == 0)
                {
                    sprite.Sprite.SectorId = -1;
                    continue;
                    //throw new Exception();
                }

                potentialSectors.Sort(new SectorInSectorComparer());
                sprite.Sprite.SectorId = potentialSectors[0].Id;
            }

        }

        private void AssignDistance(scoped ReadOnlySpan<RenderableSprite> sprites)
        {
            int width = this.width;
            float cameraWidthIncr = 2.0f / width;

            for (int j = 0; j < sprites.Length; j++)
            {
                RenderableSprite sprite = sprites[j];
                bool wallSprite = sprite.Texture.RenderingOptions.HasFlag(TextureRenderingOptions.RenderAsWall);

                sprite.Distance = wallSprite ? CalculateDistanceForWallSprite(sprite) : CalculateDistance(sprite);
            }

            return;

            static float CalculateDistance(RenderableSprite sprite)
            {
                (float textureWidth, _) = sprite.Texture.GetScaledDemensions();

                float ry = sprite.Rotated.Y;

                float d2x = textureWidth;
                float t1 = -ry * d2x;

                return t1 / -textureWidth;
            }

            float CalculateDistanceForWallSprite(RenderableSprite sprite)
            {
                float rx1 = sprite.R1.X;
                float ry1 = sprite.R1.Y;
                float d2x = sprite.R2.X - rx1;
                float d2y = sprite.R2.Y - ry1;
                float t1 = rx1 * d2y - ry1 * d2x;

                float cameraRayA = -EngineConstants.CameraPlaneX;
                cameraRayA += cameraWidthIncr * sprite.XLeft;

                float cameraRayB = -EngineConstants.CameraPlaneX;
                cameraRayB += cameraWidthIncr * sprite.XRight;

                float fromToYDistA = t1 / (cameraRayA * d2y - d2x);
                float fromToYDistB = t1 / (cameraRayB * d2y - d2x);

                return MathF.Min(fromToYDistA, fromToYDistB);
            }
        }

        public static List<RenderableSprite> FilterOutSpritesOutsideDepth(
            scoped Span<RenderableSprite> rotatedSprites,
            HashSet<int> sectors,
            float[] depth, float[]? parentDepth)
        {
            List<RenderableSprite> sprites = [];

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                RenderableSprite sprite = rotatedSprites[i];

                if (WithinDepth(sprite))
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;


            bool WithinDepth(RenderableSprite sprite)
            {
                float fromToYDist = sprite.Distance;

                for (int x = sprite.XLeft; x <= sprite.XRight; x++)
                {
                    if (depth[x] >= fromToYDist && (parentDepth == null || parentDepth[x] <= fromToYDist))
                    {
                        return sectors.Contains(sprite.SectorId);
                    }
                }

                return false;
            }
        }

        public static Span<RenderableSprite> RotateSprites(scoped ReadOnlySpan<RenderableSprite> sprites, PortalPlayerSnapshot player)
        {
            var rotatedSprites = new RenderableSprite[sprites.Length];

            float pSin = player.Sin;
            float pCos = player.Cos;
            float px = player.X;
            float py = player.Y;

            for (int i = 0; i < sprites.Length; i++)
            {
                RenderableSprite s = sprites[i];
                TextureInfo texture = s.Texture;

                Point rotated = RotateVertex(s.Location);

                Point r1, r2;

                if (texture.RenderingOptions.IsWall)
                {
                    r1 = RotateVertex(s.PointA);
                    r2 = RotateVertex(s.PointB);
                }
                else
                {
                    float textureWidth = texture.Width * (texture.XScale ?? 1f);

                    float rx1 = rotated.X - textureWidth / 2;
                    float rx2 = rotated.X + textureWidth / 2;
                    float ry1 = rotated.Y;
                    float ry2 = rotated.Y;

                    r1 = new Point(rx1, ry1);
                    r2 = new Point(rx2, ry2);
                }

                rotatedSprites[i] = new RenderableSprite
                {
                    Sprite = s.Sprite,
                    Rotated = rotated,
                    R1 = r1,
                    R2 = r2
                };
            }

            return rotatedSprites;

            (float x, float y) RotateVertex(Point p)
            {
                // offset by player coordinates for easier calculations
                // rotate vertex points to face 'up' from player at (0, 0)
                return SharedHelpers.RotateVertex(p.X, p.Y, pSin, pCos, px, py);
            }
        }

        public static void FilterOutSpritesBehindPlayer(ref Span<RenderableSprite> rotatedSprites)
        {
            // in-place sort out sprites and trim the span

            int j = 0;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                RenderableSprite sprite = rotatedSprites[i];

                (float x1, float y1) = sprite.R1;
                (float x2, float y2) = sprite.R2;

                if (y1 <= 0f && y2 <= 0f)
                {
                    continue;
                }

                // Render cone culling from https://theforceengine.github.io/2020/05/16/DFRender1.html
                if ((x1 < -y1 && x2 < -y2) || (x1 > y1 && x2 > y2))
                {
                    continue;
                }

                rotatedSprites[j] = sprite;
                j++;
            }

            rotatedSprites = rotatedSprites[..j];
        }

        public static void FilterOutSpritesWithoutSector(ref Span<RenderableSprite> rotatedSprites)
        {
            // in-place sort out sprites and trim the span

            int j = 0;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                RenderableSprite sprite = rotatedSprites[i];

                if (sprite.SectorId == -1)
                {
                    continue;
                }

                rotatedSprites[j] = sprite;
                j++;
            }

            rotatedSprites = rotatedSprites[..j];
        }

        public void FilterOutNonIntersectingSprites(ref Span<RenderableSprite> rotatedSprites)
        {
            // in-place sort out sprites and trim the span

            int j = 0;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                RenderableSprite sprite = rotatedSprites[i];

                if (!IntersectsView(sprite))
                {
                    continue;
                }

                if (sprite.YLeftCeil == sprite.YLeftFloor || sprite.YRightCeil == sprite.YRightFloor)
                {
                    continue;
                }

                rotatedSprites[j] = sprite;
                j++;
            }

            rotatedSprites = rotatedSprites[..j];
            return;

            bool IntersectsView(RenderableSprite s)
            {
                if (!s.IntersectsView)
                {
                    return false;
                }

                return ((0 <= s.XLeft) && (s.XLeft <= this.width)) || ((0 <= s.XRight) && (s.XRight <= this.width));
            }
        }

        public void CalculateSpritePlane(RenderableSprite sprite, float yCeil, float yFloor, float yaw)
        {
            TextureInfo textureInfo = sprite.Texture;
            Texture texture = TextureCache.GetTexture(textureInfo);
            float textureHeight = texture.Height * (textureInfo.YScale ?? 1f);

            (float rx1, float ry1) = sprite.R1;
            (float rx2, float ry2) = sprite.R2;

            float yLeftCeil, yLeftFloor, yRightCeil, yRightFloor;
            float scale = width * -EngineConstants.HeightToWidthRatio;
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            float xLeft = halfWidth - rx1 / ry1 * scale;
            float xRight = halfWidth - rx2 / ry2 * scale;

            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                (sprite.R1, sprite.R2) = (sprite.R2, sprite.R1);
            }

            // part of the wall is in the back
            if (ry1 <= 0f || ry2 <= 0f)
            {
                float d2x = rx2 - rx1;
                float d2y = ry2 - ry1;

                bool intersectsL = MathFormulas.TryGetSegmentIntersectionZero2(-EngineConstants.CameraPlaneX, rx1, ry1, d2x, d2y,
                    out float xDistanceL, out float yDistanceL);

                bool intersectsR = MathFormulas.TryGetSegmentIntersectionZero2(EngineConstants.CameraPlaneX, rx2, ry2, -d2x, -d2y,
                    out float xDistanceR, out float yDistanceR);

                if (intersectsL && intersectsR)
                {
                    rx1 = xDistanceL;
                    ry1 = yDistanceL;

                    rx2 = xDistanceR;
                    ry2 = yDistanceR;

                    xLeft = 0;
                    xRight = width - 1;

                    sprite.IntersectsView = true;
                    sprite.Flipped = true;
                }
                else if (intersectsL || intersectsR)
                {
                    float xDistance = intersectsL ? xDistanceL : xDistanceR;
                    float yDistance = intersectsL ? yDistanceL : yDistanceR;

                    if (ry1 <= 0f)
                    {
                        rx1 = xDistance;
                        ry1 = yDistance;
                        xLeft = halfWidth - rx1 / ry1 * scale;
                    }
                    else
                    {
                        rx2 = xDistance;
                        ry2 = yDistance;
                        xRight = halfWidth - rx2 / ry2 * scale;
                    }

                    sprite.Flipped = true;
                }
                else
                {
                    // despite one the wall going behind the player's view
                    // player's view doesn't intersect at corners
                    // we assume wall can't be drawn
                    sprite.IntersectsView = false;
                    return;
                }
            }

            if (xLeft == xRight)
            {
                sprite.IntersectsView = false;
                return;
            }

            Clamp(ref xLeft, ref xRight);

            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                (sprite.R1, sprite.R2) = (sprite.R2, sprite.R1);

                sprite.Flipped = !sprite.Flipped;
            }

            sprite.IntersectsView |= MathFormulas.CalculatePlaneIntersectionsForWall(width, xLeft, xRight, ref rx1, ref ry1, ref rx2, ref ry2);

            if (sprite.IntersectsView)
            {
                yCeil = yFloor + textureHeight;
                yLeftCeil = halfHeight - (yCeil / ry1 - yaw) * height;
                yLeftFloor = halfHeight - (yFloor / ry1 - yaw) * height;
                yRightCeil = halfHeight - (yCeil / ry2 - yaw) * height;
                yRightFloor = halfHeight - (yFloor / ry2 - yaw) * height;

                // sprite.C1 = new(rx1, ry1);
                // sprite.C2 = new(rx2, ry2);

                sprite.XLeft = float.ConvertToIntegerNative<int>(xLeft);
                sprite.XRight = float.ConvertToIntegerNative<int>(xRight);
                sprite.YLeftCeil = float.ConvertToIntegerNative<int>(yLeftCeil);
                sprite.YLeftFloor = float.ConvertToIntegerNative<int>(yLeftFloor);
                sprite.YRightCeil = float.ConvertToIntegerNative<int>(yRightCeil);
                sprite.YRightFloor = float.ConvertToIntegerNative<int>(yRightFloor);
            }

            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Clamp(ref float xLeft, ref float xRight)
            {
                xLeft = Math.Clamp(xLeft, 0f, width - 1f);
                xRight = Math.Clamp(xRight, 0f, width - 1f);
            }
        }
    }
}
