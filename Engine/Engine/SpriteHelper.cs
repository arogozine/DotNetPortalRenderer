using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal class SpriteHelper
    {
        private readonly int width;
        private readonly int height;
        private readonly float cameraPlaneX;
        private readonly float vFov;

        public SpriteHelper(
            int width,
            int height,
            float cameraPlaneX, float vFov)
        {
            this.width = width;
            this.height = height;
            this.cameraPlaneX = cameraPlaneX;
            this.vFov = vFov;
        }

        public Span<Sprite> GetSpritesForPlayer(PortalPlayerSnapshot player, Sprite[] sprites)
        {
            Span<Sprite> rotatedSprites = RotateSprites(sprites, player);
            FilterOutSpritesBehindPlayer(ref rotatedSprites);
            rotatedSprites.Sort(new SpriteComparer());

            return rotatedSprites;
        }

        public List<Sprite> FilterOutSpritesOutsideSector(PortalPlayerSnapshot player, Sector sector, Span<Sprite> rotatedSprites)
        {
            float yaw = player.Yaw;
            float pz = player.Z;
            float yCeil = sector.Ceil - pz;
            float yFloor = sector.Floor - pz;

            CalculateWallPlanes(rotatedSprites, yCeil, yFloor, yaw);

            List<Sprite> sprites = [];

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                Sprite sprite = rotatedSprites[i];

                if (IsPointInSector(sector.Walls, sprite.Location) && sprite.IntersectsView)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;
        }

        public void CalculateWallPlanes(Span<Sprite> sprites, float yCeil, float yFloor, float yaw)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];

                CalculateWallPlane(sprite, yCeil, yFloor, yaw);
            }
        }

        public static Sprite[] RotateSprites(Sprite[] sprites, PortalPlayerSnapshot player)
        {
            Sprite[] rotatedSprites = new Sprite[sprites.Length];

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

        public static bool IsPointInSector(Span<Wall> walls, Point point)
        {
            int intersections = 0;

            for (int i = 0; i < walls.Length; i++)
            {
                Wall wall = walls[i];

                float pointA_X = wall.R1.X;
                float pointA_Y = wall.R1.Y;
                float pointB_X = wall.R2.X;
                float pointB_Y = wall.R2.Y;

                // Check if point is on the same horizontal level as the edge's y-coordinates
                if (point.Y > MathF.Min(pointA_Y, pointB_Y) && point.Y <= MathF.Max(pointA_Y, pointB_Y))
                {
                    // Calculate the x-coordinate of the intersection of the ray with the edge
                    if (point.Y != pointA_Y && point.Y != pointB_Y)
                    {
                        float intersectX = pointA_X + (point.Y - pointA_Y) * (pointB_X - pointA_X) / (pointB_Y - pointA_Y);
                        if (intersectX > point.X)
                        {
                            intersections++;
                        }
                    }
                }
            }

            // If the number of intersections is odd, the point is inside the polygon
            return intersections % 2 != 0;
        }

        public void CalculateWallPlane(Sprite sprite, float yCeil, float yFloor, float yaw)
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
            float scale = width * -0.7575231f;
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
            }

            // part of the wall is in the back
                if (ry1 <= 0f || ry2 <= 0f)
            {
                float d2x = rx2 - rx1;
                float d2y = ry2 - ry1;

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
                (ry1, ry2) = (ry2, ry1);

                //(wall.R1, wall.R2) = (wall.R2, wall.R1);

               // wall.Flipped = !wall.Flipped;
            }

            sprite.IntersectsView |= CalculatePlaneIntersectionsForWall(ref xLeft, ref xRight, ref rx1, ref ry1, ref rx2, ref ry2);

            if (sprite.IntersectsView)
            {
                yCeil = yFloor + textureHeight;
                yLeftCeil = halfHeight - (yCeil / ry1 - yaw) * vFov;
                yLeftFloor = halfHeight - (yFloor / ry1 - yaw) * vFov;
                yRightCeil = halfHeight - (yCeil / ry2 - yaw) * vFov;
                yRightFloor = halfHeight - (yFloor / ry2 - yaw) * vFov;

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
            float d2y = ry2 - ry1;

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
