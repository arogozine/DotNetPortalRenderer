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

                if (!SpriteHelper.IsPointInPolygon(b.Walls, wall.R1))
                {
                    return false;
                }

                if (!SpriteHelper.IsPointInPolygon(b.Walls, wall.R2))
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

        public Span<Sprite> GetSpritesForPlayer(PortalPlayerSnapshot player, Sprite[] sprites, Sector[] sectors)
        {
            Span<Sprite> rotatedSprites = RotateSprites(sprites, player);

            FilterOutSpritesBehindPlayer(ref rotatedSprites);
            AssignSectors(rotatedSprites, sectors);

            return rotatedSprites;
        }

        private static void AssignSectors(ReadOnlySpan<Sprite> sprites, ReadOnlySpan<Sector> sectors)
        {
            for (int j = 0; j < sprites.Length; j++)
            {
                Sprite sprite = sprites[j];
                List<Sector> potentialSectors = [];

                for (int i = sectors.Length - 1; i >= 0; i--)
                {
                    Sector sector = sectors[i];

                    if (IsPointInPolygon(sector.Walls, sprite.Location))
                    {
                        potentialSectors.Add(sector);
                    }
                }

                if (potentialSectors.Count == 0)
                {
                    throw new Exception();
                }

                potentialSectors.Sort(new SectorInSectorComparer());
                sprite.SectorId = potentialSectors[0].Id;
            }

        }

        public List<Sprite> FilterOutSpritesOutsideSector(PortalPlayerSnapshot player, SectorSprites sectorSprites, Span<Sprite> rotatedSprites)
        {
            Sector sector = sectorSprites.Sector;
            float yaw = player.Yaw;
            float pz = player.Z;
            float yCeil = sector.Ceil - pz;
            float yFloor = sector.Floor - pz;

            List<Sprite> sprites = [];

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                Sprite sprite = rotatedSprites[i];

                if (sector.Id == sprite.SectorId)
                {
                    CalculateSpritePlane(sprite, yCeil, yFloor, yaw);

                    if (IntersectsView(sprite))
                    {
                        sprites.Add(sprite);
                    }
                }
            }

            sprites.Sort(new SpriteComparer());

            return sprites;

            bool IntersectsView(Sprite s)
            {
                if (!s.IntersectsView)
                {
                    return false;
                }

                return ((sectorSprites.XLeft <= s.XLeft) && (s.XLeft <= sectorSprites.XRight)) || ((sectorSprites.XLeft <= s.XRight) && (s.XRight <= sectorSprites.XRight));
            }
        }

        public void CalculateWallPlanes(Span<Sprite> sprites, float yCeil, float yFloor, float yaw)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];

                CalculateSpritePlane(sprite, yCeil, yFloor, yaw);
            }
        }

        public static Span<Sprite> RotateSprites(Sprite[] sprites, PortalPlayerSnapshot player)
        {
            var rotatedSprites = new Sprite[sprites.Length];

            float pSin = player.Sin;
            float pCos = player.Cos;
            float px = player.X;
            float py = player.Y;

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite s = sprites[i];

                float vx1 = s.Location.X;
                float vy1 = s.Location.Y;

                // offset by player coordinates for easier calculations
                float tx1 = vx1 - px;
                float ty1 = vy1 - py;

                // rotate vertex points to face 'up' from player at (0, 0)
                float rx1 = tx1 * pSin - ty1 * pCos;
                float ry1 = tx1 * pCos + ty1 * pSin;

                rotatedSprites[i] = new Sprite {
                    Angle = s.Angle,
                    Location = s.Location,
                    Rotated = new Point(rx1, ry1),
                    TextureName = s.TextureName
                };

            }

            return rotatedSprites;
        }

        public static void FilterOutSpritesBehindPlayer(ref Span<Sprite> rotatedSprites)
        {
            // in-place sort out sprites and trim the span

            int j = 0;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                Sprite sprite = rotatedSprites[i];

                if (sprite.Rotated.Y <= 0f)
                {
                    continue;
                }

                rotatedSprites[j] = sprite;
                j++;
            }

            rotatedSprites = rotatedSprites[..j];
        }

        public static bool IsPointInPolygon(ReadOnlySpan<Wall> walls, Point point)
        {
            float x = point.X;
            float y = point.Y;
            bool inside = false;

            for (int i = 0; i < walls.Length; i++)
            {
                Wall wall = walls[i];

                float x1 = wall.R1.X;
                float y1 = wall.R1.Y;
                float x2 = wall.R2.X;
                float y2 = wall.R2.Y;

                if (MathF.Min(y1, y2) <= y && y < MathF.Max(y1, y2) && x <= MathF.Max(x1, x2))
                {
                    float xinters = default;

                    if (y1 != y2)
                    {
                        xinters = (y - y1) * (x2 - x1) / (y2 - y1) + x1;
                    }

                    if (x1 == x2 || x <= xinters)
                    {
                        inside = !inside;
                    }
                }
            }

            return inside;
        }

        public void CalculateSpritePlane(Sprite sprite, float yCeil, float yFloor, float yaw)
        {
            ref Texture texture = ref TextureCache.GetTexture(sprite.TextureName);

            int textureWidth = texture.Width;
            int textureHeight = texture.Height;

            // calculate the x, y for the wall on the screen for both points
            float rx1 = sprite.Rotated.X - textureWidth / 2;
            float rx2 = sprite.Rotated.X + textureWidth / 2;
            float ry1 = sprite.Rotated.Y;
            float ry2 = sprite.Rotated.Y;

            sprite.R1 = new Point(rx1, ry1);
            sprite.R2 = new Point(rx2, ry2);

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
                //(ry1, ry2) = (ry2, ry1);
            }

            // part of the wall is in the back
            if (ry1 <= 0f)
            {
                float d2x = rx2 - rx1;
                float d2y = 0f;// ry2 - ry1;

                bool intersectsL = TryGetSegmentIntersectionZero2(-EngineConstants.CameraPlaneX, rx1, ry1, d2x, d2y,
                    out float xDistanceL, out float yDistanceL);

                bool intersectsR = TryGetSegmentIntersectionZero2(EngineConstants.CameraPlaneX, rx1, ry1, d2x, d2y,
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
                // (ry1, ry2) = (ry2, ry1);
            }

            sprite.IntersectsView |= CalculatePlaneIntersectionsForWall(ref xLeft, ref xRight, ref rx1, ref ry1, ref rx2, ref ry2);

            if (sprite.IntersectsView)
            {
                yCeil = yFloor + textureHeight;
                yLeftCeil = halfHeight - (yCeil / ry1 - yaw) * height;
                yLeftFloor = halfHeight - (yFloor / ry1 - yaw) * height;
                yRightCeil = halfHeight - (yCeil / ry2 - yaw) * height;
                yRightFloor = halfHeight - (yFloor / ry2 - yaw) * height;

                // wall.C1 = new(rx1, ry1);
                // wall.C2 = new(rx2, ry2);

                sprite.XLeft = (int)xLeft;
                sprite.XRight = (int)xRight;
                sprite.YLeftCeil = (int)yLeftCeil;
                sprite.YLeftFloor = (int)yLeftFloor;
                sprite.YRightCeil = (int)yRightCeil;
                sprite.YRightFloor = (int)yRightFloor;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Clamp(ref float xLeft, ref float xRight)
            {
                xLeft = Math.Clamp(xLeft, 0f, width - 1f);
                xRight = Math.Clamp(xRight, 0f, width - 1f);
            }
        }

        private bool CalculatePlaneIntersectionsForWall(ref float xLeft, ref float xRight, ref float rx1, ref float ry1, ref float rx2, ref float ry2)
        {
            int xLeftInt = (int)xLeft;
            int xRightInt = (int)xRight;

            // Nothing To Render
            if (xLeftInt == xRightInt)
            {
                return false;
            }

            float cameraPlaneX = this.cameraPlaneX;

            float cameraWidthIncr = 2.0f / width;

            float d2x = rx2 - rx1;
            float d2y = 0f; //ry2 - ry1;

            float rayDirLeft = cameraPlaneX * (cameraWidthIncr * xLeftInt - 1f);
            float rayDirRight = cameraPlaneX * (cameraWidthIncr * xRightInt - 1f);

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

            xLeft = xLeftInt;
            xRight = xRightInt;

            return true;

            // Debugging Code Below

            float cameraX = -1f;

            if (!intersectsL)
            {
                cameraX += (cameraWidthIncr * (xLeftInt - 1));

                for (int x = xLeftInt - 1; x <= xRightInt; x++, cameraX += cameraWidthIncr)
                {
                    float rayDirX = cameraPlaneX * cameraX;

                    bool intersects = TryGetSegmentIntersectionZero2(rayDirX, rx1, ry1, d2x, d2y,
                        out float xDistance, out float yDistance);

                    if (intersects)
                    {
                        if (x != xLeftInt)
                            Debug.WriteLine($"intersectsL {Math.Abs(x - xLeftInt)}");

                        rx1 = xDistance;
                        ry1 = yDistance;
                        xLeftInt = x;
                        intersectsL = true;

                        break;
                    }
                }

                if (!intersectsL)
                {
                    return false;
                }
            }

            if (!intersectsR)
            {
                cameraX = -1f;
                cameraX += (cameraWidthIncr * xRightInt);

                for (int x = xRightInt; x >= xLeftInt; x--, cameraX -= cameraWidthIncr)
                {
                    float rayDirX = cameraPlaneX * cameraX;

                    bool intersects = TryGetSegmentIntersectionZero2(rayDirX, rx1, ry1, d2x, d2y,
                        out float xDistance, out float yDistance);

                    if (intersects)
                    {
                        if (x != xRightInt)
                            Debug.WriteLine($"intersectsR {Math.Abs(x - xRightInt)}");

                        rx2 = xDistance;
                        ry2 = yDistance;
                        xRightInt = x;
                        intersectsR = true;
                        break;
                    }
                }

                if (!intersectsR)
                {
                    return false;
                }
            }

            xLeft = xLeftInt;
            xRight = xRightInt;

            return intersectsL || intersectsR;

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
