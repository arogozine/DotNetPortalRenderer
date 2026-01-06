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
                Wall wall = a.Walls[i];

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

        public Span<Sprite> GetSpritesForPlayer(PortalPlayerSnapshot player, Span<Sprite> sprites, ReadOnlySpan<Sector> sectors)
        {
            Span<Sprite> rotatedSprites = RotateSprites(sprites, player);

            FilterOutSpritesBehindPlayer(ref rotatedSprites);
            FilterOutSpritesWithoutSector(ref rotatedSprites);

            float yaw = player.Yaw;
            float pz = player.Z;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                Sprite sprite = rotatedSprites[i];

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

        public static void AssignSectors(scoped ReadOnlySpan<Sprite> sprites, scoped ReadOnlySpan<Sector> sectors)
        {
            for (int j = 0; j < sprites.Length; j++)
            {
                Sprite sprite = sprites[j];
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
                    sprite.SectorId = -1;
                    continue;
                    //throw new Exception();
                }

                potentialSectors.Sort(new SectorInSectorComparer());
                sprite.SectorId = potentialSectors[0].Id;
            }

        }

        private void AssignDistance(scoped ReadOnlySpan<Sprite> sprites)
        {
            int width = this.width;
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;


            for (int j = 0; j < sprites.Length; j++)
            {
                Sprite sprite = sprites[j];
                bool wallSprite = sprite.Texture.RenderingOptions.HasFlag(TextureRenderingOptions.RenderAsWall);

                sprite.Distance = wallSprite ? CalculateDistanceForWallSprite(sprite) : CalculateDistance(sprite);
            }

            static float CalculateDistance(Sprite sprite)
            {
                Texture texture = TextureCache.GetTexture(sprite.Texture);

                int textureHeight = texture.Width;

                float ry = sprite.Rotated.Y;

                float d2x = textureHeight;
                float t1 = -ry * d2x;

                return t1 / -textureHeight;
            }

            float CalculateDistanceForWallSprite(Sprite sprite)
            {
                float rx1 = sprite.R1.X;
                float ry1 = sprite.R1.Y;
                float d2x = sprite.R2.X - rx1;
                float d2y = sprite.R2.Y - ry1;
                float t1 = rx1 * d2y - ry1 * d2x;

                float cameraRayA = -1f * EngineConstants.CameraPlaneX;
                cameraRayA += cameraWidthIncr * sprite.XLeft;

                float cameraRayB = -1f * EngineConstants.CameraPlaneX;
                cameraRayB += cameraWidthIncr * sprite.XRight;

                float fromToYDistA = t1 / (cameraRayA * d2y - d2x);
                float fromToYDistB = t1 / (cameraRayB * d2y - d2x);

                return MathF.Min(fromToYDistA, fromToYDistB);
            }
        }

        public static List<Sprite> FilterOutSpritesOutsideDepth(
            scoped Span<Sprite> rotatedSprites,
            HashSet<int> sectors,
            float[] depth, float[]? parentDepth)
        {
            List<Sprite> sprites = [];

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                Sprite sprite = rotatedSprites[i];

                if (WithinDepth(sprite))
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;


            bool WithinDepth(Sprite sprite)
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

        public static Span<Sprite> RotateSprites(scoped ReadOnlySpan<Sprite> sprites, PortalPlayerSnapshot player)
        {
            var rotatedSprites = new Sprite[sprites.Length];

            float pSin = player.Sin;
            float pCos = player.Cos;
            float px = player.X;
            float py = player.Y;

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite s = sprites[i];
                TextureInfo textureInfo = s.Texture;

                Point rotated = RotateVertex(s.Location);

                Point r1, r2;

                if (textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.RenderAsWall))
                {
                    r1 = RotateVertex(s.PointA);
                    r2 = RotateVertex(s.PointB);
                }
                else
                {
                    Texture texture = TextureCache.GetTexture(textureInfo);

                    float textureWidth = texture.Width * (textureInfo.XScale ?? 1f);

                    float rx1 = rotated.X - textureWidth / 2;
                    float rx2 = rotated.X + textureWidth / 2;
                    float ry1 = rotated.Y;
                    float ry2 = rotated.Y;

                    r1 = new Point(rx1, ry1);
                    r2 = new Point(rx2, ry2);
                }

                rotatedSprites[i] = new Sprite
                {
                    Angle = s.Angle,
                    Location = s.Location,
                    Rotated = rotated,
                    R1 = r1,
                    R2 = r2,
                    Height = s.Height,
                    Texture = s.Texture,
                    SectorId = s.SectorId,
                    Length = s.Length
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

        public static void FilterOutSpritesBehindPlayer(ref Span<Sprite> rotatedSprites)
        {
            // in-place sort out sprites and trim the span

            int j = 0;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                Sprite sprite = rotatedSprites[i];

                if (sprite.R1.Y <= 0f && sprite.R2.Y <= 0f)
                {
                    continue;
                }

                rotatedSprites[j] = sprite;
                j++;
            }

            rotatedSprites = rotatedSprites[..j];
        }

        public static void FilterOutSpritesWithoutSector(ref Span<Sprite> rotatedSprites)
        {
            // in-place sort out sprites and trim the span

            int j = 0;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                Sprite sprite = rotatedSprites[i];

                if (sprite.SectorId == -1)
                {
                    continue;
                }

                rotatedSprites[j] = sprite;
                j++;
            }

            rotatedSprites = rotatedSprites[..j];
        }

        public void FilterOutNonIntersectingSprites(ref Span<Sprite> rotatedSprites)
        {
            // in-place sort out sprites and trim the span

            int j = 0;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                Sprite sprite = rotatedSprites[i];

                if (!IntersectsView(sprite))
                {
                    continue;
                }

                rotatedSprites[j] = sprite;
                j++;
            }

            rotatedSprites = rotatedSprites[..j];


            bool IntersectsView(Sprite s)
            {
                if (!s.IntersectsView)
                {
                    return false;
                }

                return ((0 <= s.XLeft) && (s.XLeft <= this.width)) || ((0 <= s.XRight) && (s.XRight <= this.width));
            }
        }

        public void CalculateSpritePlane(Sprite sprite, float yCeil, float yFloor, float yaw)
        {
            TextureInfo textureInfo = sprite.Texture;
            Texture texture = TextureCache.GetTexture(textureInfo);
            float textureHeight = texture.Height * (textureInfo.YScale ?? 1f);

            (float rx1, float ry1) = sprite.R1;
            (float rx2, float ry2) = sprite.R2;

            float xLeft, xRight, yLeftCeil, yLeftFloor, yRightCeil, yRightFloor;
            float scale = width * -EngineConstants.HeightToWidthRatio;
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            xLeft = halfWidth - rx1 / ry1 * scale;
            xRight = halfWidth - rx2 / ry2 * scale;

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

                bool intersectsL = TryGetSegmentIntersectionZero2(-EngineConstants.CameraPlaneX, rx1, ry1, d2x, d2y,
                    out float xDistanceL, out float yDistanceL);

                bool intersectsR = TryGetSegmentIntersectionZero2(EngineConstants.CameraPlaneX, rx2, ry2, -d2x, -d2y,
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

            sprite.IntersectsView |= CalculatePlaneIntersectionsForWall(xLeft, xRight, ref rx1, ref ry1, ref rx2, ref ry2);

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Clamp(ref float xLeft, ref float xRight)
            {
                xLeft = Math.Clamp(xLeft, 0f, width - 1f);
                xRight = Math.Clamp(xRight, 0f, width - 1f);
            }
        }

        private bool CalculatePlaneIntersectionsForWall(float xLeft, float xRight, ref float rx1, ref float ry1, ref float rx2, ref float ry2)
        {
            // Nothing To Render
            if (float.ConvertToIntegerNative<int>(xLeft) == float.ConvertToIntegerNative<int>(xRight))
            {
                return false;
            }

            float cameraWidthIncr = 2.0f / width;

            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;

            float rayDirLeft = EngineConstants.CameraPlaneX * (cameraWidthIncr * xLeft - 1f);
            float rayDirRight = EngineConstants.CameraPlaneX * (cameraWidthIncr * xRight - 1f);

            bool intersectsL = TryGetSegmentIntersectionZero2(rayDirLeft, rx1, ry1, d2x, d2y,
                out float xDistanceL, out float yDistanceL);

            bool intersectsR = TryGetSegmentIntersectionZero2(rayDirRight, rx1, ry1, d2x, d2y,
                out float xDistanceR, out float yDistanceR);

            if (intersectsL && intersectsR)
            {
                rx1 = xDistanceL;
                ry1 = yDistanceL;

                rx2 = xDistanceR;
                ry2 = yDistanceR;

                return true;
            }
            else if (intersectsL)
            {
                rx1 = xDistanceL;
                ry1 = yDistanceL;
            }
            else if (intersectsR)
            {
                rx2 = xDistanceR;
                ry2 = yDistanceR;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetSegmentIntersectionZero2(
            float rayDirX,
            float rx1, float ry1,
            float d2x, float d2y,
            out float distanceX,
            out float distanceY)
        {
            distanceY = default;
            distanceX = default;

            float denominator = rayDirX * d2y - d2x;

            if (MathF.Abs(denominator) < float.Epsilon)
            {
                return false;
            }

            float u = (rx1 - ry1 * rayDirX) / denominator;

            if (u < 0f || u > 1f)
            {
                return false;
            }

            float t = (rx1 * d2y - ry1 * d2x) / denominator;

            if (t < 0f)
            {
                return false;
            }

            distanceY = t;
            distanceX = t * rayDirX;

            return true;
        }
    }
}
