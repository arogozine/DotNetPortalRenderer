using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed class SectorInSectorComparer : IComparer<RenderableSector>
    {
        public static readonly SectorInSectorComparer Default = new();

        public int Compare(RenderableSector? x, RenderableSector? y)
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

        public static bool IsSectorInSector(RenderableSector a, RenderableSector b)
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

    /// <summary>
    /// Determines which sprites can be seen
    /// </summary>
    internal sealed class SpriteHelper
    {
        private readonly int width;
        private readonly int height;

        public SpriteHelper(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public Span<RenderableSprite> GetSpritesForPlayer(PortalPlayerSnapshot player, Span<RenderableSprite> sprites,
            RenderableSector[] sectors)
        {
            Span<RenderableSprite> rotatedSprites = RotateSprites(sprites, player);

            return GetSpritesForPlayerShared(player, rotatedSprites, sectors);
        }

        public Span<RenderableSprite> GetMirroredSprites(PortalPlayerSnapshot player, Span<RenderableSprite> sprites,
            RenderableSector[] sectors, HashSet<int> mirroredSectorsSet, RenderWindowSpriteSnapshot sectorSprites)
        {
            if (mirroredSectorsSet.Count == 0 || sectorSprites.MirroredWalls is null || sectorSprites.MirroredWalls.Count == 0)
            {
                return [];
            }

            List<RenderableSprite> spritesToMirror = [];
            foreach (RenderableSprite sprite in sprites)
            {
                if (mirroredSectorsSet.Contains(sprite.SectorId))
                {
                    spritesToMirror.Add(sprite);
                }
            }

            if (spritesToMirror.Count == 0)
            {
                return [];
            }

            Span<RenderableSprite> rotatedSprites = RotateMirrorSprites(CollectionsMarshal.AsSpan(spritesToMirror), player, sectorSprites.MirroredWalls.First());

            return GetSpritesForPlayerShared(player, rotatedSprites, sectors);
        }

        private Span<RenderableSprite> GetSpritesForPlayerShared(PortalPlayerSnapshot player, Span<RenderableSprite> rotatedSprites,
            RenderableSector[] sectors)
        {
            rotatedSprites = FilterOutSpritesBehindPlayer(rotatedSprites);
            rotatedSprites = FilterOutSpritesWithoutSector(rotatedSprites);

            float yaw = player.Yaw;
            float pz = player.Z;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                RenderableSprite sprite = rotatedSprites[i];

                RenderableSector sector = sectors[sprite.SectorId];
                float yCeil = sector.Ceil - pz + sprite.Height;
                float yFloor = sector.Floor - pz + sprite.Height;

                if (sprite is RenderableFloorSprite floorSprite)
                {
                    CalculateFloorPlane(floorSprite, yCeil, yFloor, yaw);
                }
                else if (sprite is RenderableWallSprite renderableWall)
                {
                    CalculateSpritePlane(renderableWall, yCeil, yFloor, yaw);
                }
                else if (sprite is RenderableBasicSprite renderableSprite)
                {
                    CalculateSpritePlane(renderableSprite, yCeil, yFloor, yaw);
                }
            }

            FilterOutNonIntersectingSprites(ref rotatedSprites);
            AssignDistance(rotatedSprites);

            ArraySortHelper.Sort(rotatedSprites, SpriteComparer.Instance);

            return rotatedSprites;
        }

        private static readonly List<RenderableSprite> sprites = [];

        public static List<RenderableSprite> FilterOutSpritesOutsideDepth(
            scoped Span<RenderableSprite> rotatedSprites,
            IReadOnlySet<int> sectors,
            Span<float> maxDepth, Span<float> minDepth)
        {
            sprites.Clear();

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                RenderableSprite sprite = rotatedSprites[i];
                
                if (WithinDepth(sprite, maxDepth, minDepth))
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;


            bool WithinDepth(RenderableSprite sprite, Span<float> maxDepth, Span<float> minDepth)
            {
                float distanceMin = sprite.DistanceMin;
                float distanceMax = sprite.DistanceMax;

                if (!minDepth.IsEmpty)
                {
                    for (int x = sprite.XLeft; x <= sprite.XRight; x++)
                    {
                        float minDepthX = minDepth[x];
                        float maxDepthX = maxDepth[x];

                        if (minDepthX >= maxDepthX)
                            continue;

                        if (SharedHelpers.WithinInclusive(distanceMin, minDepthX, maxDepthX) ||
                            SharedHelpers.WithinInclusive(distanceMax, minDepthX, maxDepthX) ||
                            SharedHelpers.WithinInclusive(minDepthX, distanceMin, distanceMax) ||
                            SharedHelpers.WithinInclusive(maxDepthX, distanceMin, distanceMax))
                        {
                            return sectors.Contains(sprite.SectorId);
                        }
                    }

                    return false;
                }

                for (int x = sprite.XLeft; x <= sprite.XRight; x++)
                {
                    if (maxDepth[x] >= distanceMin || maxDepth[x] >= distanceMax)
                    {
                        return sectors.Contains(sprite.SectorId);
                    }
                }

                return false;
            }
        }

        public static Span<RenderableSprite> FilterOutSpritesBehindPlayer(Span<RenderableSprite> rotatedSprites)
        {
            // in-place sort out sprites and trim the span

            int j = 0;

            for (int i = 0; i < rotatedSprites.Length; i++)
            {
                RenderableSprite sprite = rotatedSprites[i];

                (float x1, float y1) = sprite.R1;
                (float x2, float y2) = sprite.R2;

                // Similar to Walls
                // 1. We cull sprites that are behind the player
                // 2. We cull sprites where all points are outside the view cone
                // 3. We performn backface culling where appropriate

                if (sprite is RenderableFloorSprite floorSprite)
                {
                    (float x3, float y3) = floorSprite.R3;
                    (float x4, float y4) = floorSprite.R4;

                    if (y1 <= 0f && y2 <= 0f && y3 <= 0f && y4 <= 0f)
                    {
                        continue;
                    }

                    if ((x1 < -y1 && x2 < -y2 && x3 < -y3 && x4 < -y4) || (x1 > y1 && x2 > y2 && x3 > y3 && x4 > y4))
                    {
                        continue;
                    }

                }
                else // Basic or Wall Sprite
                {
                    if (y1 <= 0f && y2 <= 0f)
                    {
                        continue;
                    }

                    if ((x1 < -y1 && x2 < -y2) || (x1 > y1 && x2 > y2))
                    {
                        continue;
                    }

                    if ((sprite is RenderableWallSprite wallSprite) && wallSprite.TwoSided == false &&
                        (sprite.Flipped ? (x2 * y1 < y2 * x1) : (x2 * y1 > y2 * x1)))
                    {
                        continue;
                    }

                }

                rotatedSprites[j] = sprite;
                j++;
            }

            return rotatedSprites[..j];
        }

        public static Span<RenderableSprite> FilterOutSpritesWithoutSector(Span<RenderableSprite> rotatedSprites)
        {
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

            return rotatedSprites[..j];
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

                if (sprite is IWallLike wallLikeSprite)
                {
                    if (wallLikeSprite.YLeftCeil == wallLikeSprite.YLeftFloor || wallLikeSprite.YRightCeil == wallLikeSprite.YRightFloor)
                    {
                        continue;
                    }

                    if (wallLikeSprite.YLeftFloor < 0 && wallLikeSprite.YRightFloor < 0)
                    {
                        continue;
                    }

                    if (wallLikeSprite.YLeftCeil > this.height && wallLikeSprite.YRightCeil > this.height)
                    {
                        continue;
                    }
                }
                else if (sprite is RenderableFloorSprite floorSprite)
                {
                    bool canRender = false;

                    ReadOnlySpan<FloorSpriteWallInfo> walls = [floorSprite.Wall1!, floorSprite.Wall2!, floorSprite.Wall3!, floorSprite.Wall4!];

                    foreach (var wall in walls)
                    {
                        if (wall.YLeftFloor < 0 && wall.YRightFloor < 0)
                        {
                            continue;
                        }

                        canRender = true;
                        break;
                    }

                    if (!canRender)
                    {
                        continue;
                    }
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

        public static void AssignSectors(scoped ReadOnlySpan<RenderableSprite> sprites, scoped ReadOnlySpan<RenderableSector> sectors)
        {
            for (int j = 0; j < sprites.Length; j++)
            {
                RenderableSprite sprite = sprites[j];
                List<RenderableSector> potentialSectors = [];

                for (int i = sectors.Length - 1; i >= 0; i--)
                {
                    RenderableSector sector = sectors[i];

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

                ArraySortHelper.Sort(CollectionsMarshal.AsSpan(potentialSectors), SectorInSectorComparer.Default);

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

                if (sprite is RenderableBasicSprite basicSprite)
                {
                    sprite.DistanceMin = CalculateDistance(basicSprite);
                    sprite.DistanceMax = sprite.DistanceMin;
                }
                else if (sprite is RenderableWallSprite wallSprite)
                {
                    (sprite.DistanceMin, sprite.DistanceMax) = CalculateDistanceForWallSprite(wallSprite);
                }
                else if (sprite is RenderableFloorSprite floorSprite)
                {
                    (sprite.DistanceMin, sprite.DistanceMax) = CalculateDistanceForFloorSprite(floorSprite);
                }
            }

            return;

            static float CalculateDistance(RenderableBasicSprite sprite)
            {
                (float textureWidth, _) = sprite.Texture.GetScaledDimensions();

                float ry = sprite.Rotated.Y;

                float d2x = textureWidth;
                float t1 = -ry * d2x;

                return t1 / -textureWidth;
            }

            (float DistanceMin, float DistanceMax) CalculateDistanceForWallSprite(RenderableWallSprite sprite)
            {
                float fromToYDistA = CalculateDistance2(sprite.R1, sprite.R2, sprite.XLeft);
                float fromToYDistB = CalculateDistance2(sprite.R1, sprite.R2, sprite.XRight);

                float distanceMin = MathF.Min(fromToYDistA, fromToYDistB);
                float distanceMax = MathF.Max(fromToYDistA, fromToYDistB);

                return (distanceMin, distanceMax);
            }

            (float DistanceMin, float DistanceMax) CalculateDistanceForFloorSprite(RenderableFloorSprite sprite)
            {
                int len = 0;
                Span<float> dist = stackalloc float[4];

                if (sprite.Wall1!.IntersectsView)
                {
                    dist[len++] = CalculateDistance2(sprite.R1, sprite.R2, sprite.Wall1!.XLeft);
                }

                if (sprite.Wall2!.IntersectsView)
                {
                    dist[len++] = CalculateDistance2(sprite.R2, sprite.R3, sprite.Wall2!.XLeft);
                }

                if (sprite.Wall3!.IntersectsView)
                {
                    dist[len++] = CalculateDistance2(sprite.R3, sprite.R4, sprite.Wall3!.XLeft);
                }

                if (sprite.Wall4!.IntersectsView)
                {
                    dist[len++] = CalculateDistance2(sprite.R4, sprite.R1, sprite.Wall4!.XLeft);
                }

                float distanceMin = float.MaxValue;
                float distanceMax = float.MinValue;

                for (int i = 0; i < len; i++)
                {
                    distanceMin = MathF.Min(distanceMin, dist[i]);
                    distanceMax = MathF.Max(distanceMax, dist[i]);
                }

                return (distanceMin, distanceMax);
            }

            float CalculateDistance2(Vector2 r1, Vector2 r2, int x)
            {
                float rx1 = r1.X;
                float ry1 = r1.Y;
                float d2x = r2.X - rx1;
                float d2y = r2.Y - ry1;
                float t1 = MathF.FusedMultiplyAdd(rx1, d2y, -ry1 * d2x);

                float cameraRay = -EngineConstants.CameraPlaneX;
                cameraRay += cameraWidthIncr * x;

                return t1 / MathF.FusedMultiplyAdd(cameraRay, d2y, -d2x);
            }
        }

        private static RenderableSprite[] _mirrorSprites = new RenderableSprite[32];

        public static Span<RenderableSprite> RotateMirrorSprites(scoped ReadOnlySpan<RenderableSprite> sprites, PortalPlayerSnapshot player, RenderableWall flippedWall)
        {
            if (_mirrorSprites.Length < sprites.Length)
            {
                Array.Resize(ref _mirrorSprites, sprites.Length);
            }

            Span<RenderableSprite> rotatedSprites = _mirrorSprites.AsSpan();
            rotatedSprites.Clear();
            rotatedSprites = rotatedSprites[..sprites.Length];

            float pSin = player.Sin;
            float pCos = player.Cos;
            float px = player.X;
            float py = player.Y;

            for (int i = 0; i < sprites.Length; i++)
            {
                RenderableSprite s = sprites[i];
                DetermineAngle(s, player);

                rotatedSprites[i] = CreateMirroredRotatedCopy(s);
            }

            return rotatedSprites;

            RenderableSprite CreateMirroredRotatedCopy(RenderableSprite s)
            {
                GameTextureInfo texture = s.Texture;

                Vector2 rotated = RotateVertex(MathFormulas.ReflectPoint(s.Location, flippedWall.PointA, flippedWall.PointB));

                if (s is RenderableFloorSprite floorSprite)
                {
                    Vector2 r1 = SharedHelpers.RotateVertex(
                        MathFormulas.ReflectPoint(floorSprite.PointA, flippedWall.PointA, flippedWall.PointB), pSin, pCos, px, py);
                    Vector2 r2 = SharedHelpers.RotateVertex(
                        MathFormulas.ReflectPoint(floorSprite.PointB, flippedWall.PointA, flippedWall.PointB), pSin, pCos, px, py);
                    Vector2 r3 = SharedHelpers.RotateVertex(
                        MathFormulas.ReflectPoint(floorSprite.PointC, flippedWall.PointA, flippedWall.PointB), pSin, pCos, px, py);
                    Vector2 r4 = SharedHelpers.RotateVertex(
                        MathFormulas.ReflectPoint(floorSprite.PointD, flippedWall.PointA, flippedWall.PointB), pSin, pCos, px, py);

                    return new RenderableFloorSprite
                    {
                        Sprite = s.Sprite,
                        Rotated = rotated,
                        R1 = r1,
                        R2 = r2,
                        R3 = r3,
                        R4 = r4,
                        Flipped = !s.Flipped
                    };
                }
                else if (s is RenderableWallSprite)
                {
                    Vector2 r1 = RotateVertex(MathFormulas.ReflectPoint(s.PointA, flippedWall.PointA, flippedWall.PointB));
                    Vector2 r2 = RotateVertex(MathFormulas.ReflectPoint(s.PointB, flippedWall.PointA, flippedWall.PointB));

                    return new RenderableWallSprite
                    {
                        Sprite = s.Sprite,
                        Rotated = rotated,
                        R1 = r1,
                        R2 = r2,
                        Flipped = !s.Flipped
                    };
                }
                else
                {
                    float textureWidth = texture.Width * (texture.XScale ?? 1f);

                    float rx1 = rotated.X - textureWidth / 2;
                    float rx2 = rotated.X + textureWidth / 2;
                    float ry1 = rotated.Y;
                    float ry2 = rotated.Y;

                    Vector2 r1 = new(rx1, ry1);
                    Vector2 r2 = new(rx2, ry2);

                    return new RenderableBasicSprite
                    {
                        Sprite = s.Sprite,
                        Rotated = rotated,
                        R1 = r1,
                        R2 = r2,
                        Flipped = !s.Flipped
                    };
                }
            }

            Vector2 RotateVertex(Vector2 p)
            {
                // offset by player coordinates for easier calculations
                // rotate vertex points to face 'up' from player at (0, 0)
                return SharedHelpers.RotateVertex(p.X, p.Y, pSin, pCos, px, py);
            }
        }

        private static RenderableSprite[] _rotatedSprites = new RenderableSprite[32];

        public static Span<RenderableSprite> RotateSprites(scoped ReadOnlySpan<RenderableSprite> sprites, PortalPlayerSnapshot player)
        {
            if (_rotatedSprites.Length < sprites.Length)
            {
                Array.Resize(ref _rotatedSprites, sprites.Length);
            }

            Span<RenderableSprite> rotatedSprites = _rotatedSprites.AsSpan();
            rotatedSprites.Clear();
            rotatedSprites = rotatedSprites[..sprites.Length];

            float pSin = player.Sin;
            float pCos = player.Cos;
            float px = player.X;
            float py = player.Y;

            for (int i = 0; i < sprites.Length; i++)
            {
                RenderableSprite s = sprites[i];
                RotateSprite(s);
                rotatedSprites[i] = s;
            }

            return rotatedSprites;

            void RotateSprite(RenderableSprite s)
            {
                GameTextureInfo texture = s.Texture;

                Vector2 rotated = RotateVertex(s.Location);

                DetermineAngle(s, player);

                if (s is RenderableFloorSprite floorSprite)
                {
                    Vector2 r1 = SharedHelpers.RotateVertex(floorSprite.PointA, pSin, pCos, px, py);
                    Vector2 r2 = SharedHelpers.RotateVertex(floorSprite.PointB, pSin, pCos, px, py);
                    Vector2 r3 = SharedHelpers.RotateVertex(floorSprite.PointC, pSin, pCos, px, py);
                    Vector2 r4 = SharedHelpers.RotateVertex(floorSprite.PointD, pSin, pCos, px, py);

                    floorSprite.Rotated = rotated;
                    floorSprite.R1 = r1;
                    floorSprite.R2 = r2;
                    floorSprite.R3 = r3;
                    floorSprite.R4 = r4;
                    floorSprite.Flipped = false;
                }
                else if (s is RenderableWallSprite wallSprite)
                {
                    Vector2 r1 = RotateVertex(s.PointA);
                    Vector2 r2 = RotateVertex(s.PointB);

                    wallSprite.Rotated = rotated;
                    wallSprite.R1 = r1;
                    wallSprite.R2 = r2;
                    wallSprite.Flipped = false;
                }
                else
                {
                    float textureWidth = texture.Width * (texture.XScale ?? 1f);

                    float rx1 = rotated.X - textureWidth / 2;
                    float rx2 = rotated.X + textureWidth / 2;
                    float ry1 = rotated.Y;
                    float ry2 = rotated.Y;

                    Vector2 r1 = new(rx1, ry1);
                    Vector2 r2 = new(rx2, ry2);

                    s.Rotated = rotated;
                    s.R1 = r1;
                    s.R2 = r2;
                }
            }

            Vector2 RotateVertex(Vector2 p)
            {
                // offset by player coordinates for easier calculations
                // rotate vertex points to face 'up' from player at (0, 0)
                return SharedHelpers.RotateVertex(p.X, p.Y, pSin, pCos, px, py);
            }
        }

        private static void DetermineAngle(RenderableSprite s, PortalPlayerSnapshot player)
        {
            if (s.Sprite.AnimationAngle?.AnimationToAngleToTexture?[0] is { } animationAngle)
            {
                GameTextureInfo texture = s.Texture;

                float px = player.X;
                float py = player.Y;

                (float mx, float my) = s.Location;
                float dx = px - mx;
                float dy = py - my;
                s.AngleToPlayer = MathFormulas.ClampAngle(MathF.Atan2(dy, dx) - 0.5f * MathF.PI);

                TextureAngle selectedAngle = animationAngle[0];
                float dist = float.MaxValue;
                float angleToPlayer = s.AngleToPlayer;

                for (int i = 0; i < animationAngle.Length; i++)
                {
                    TextureAngle textureAngle = animationAngle[i];

                    float dist2 = MathF.Abs(angleToPlayer - textureAngle.Angle);

                    if (dist2 < dist)
                    {
                        dist = dist2;
                        selectedAngle = textureAngle;
                    }
                }

                if (s.Sprite.Texture.Texture != selectedAngle.Texture)
                {
                    s.Flipped = selectedAngle.Flipped;
                    s.Sprite.Texture.Texture = selectedAngle.Texture;

                    (float width, _) = texture.GetScaledDimensions();

                    Sprite sprite = s.Sprite;

                    (float x, float y) = s.Sprite.Location;

                    float rx1 = x - width * 0.5f;
                    float rx2 = x + width * 0.5f;
                    float ry1 = y;
                    float ry2 = y;

                    sprite.Length = width;
                    sprite.PointA = new Vector2(rx1, ry1);
                    sprite.PointB = new Vector2(rx2, ry2);
                }
            }
        }

        #region Calculate Plane

        public void CalculateSpritePlane(RenderableBasicSprite sprite, float yCeil, float yFloor, float yaw)
        {
            GameTextureInfo textureInfo = sprite.Texture;
            GameTexture texture = TextureCache.GetTexture(textureInfo);
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

                // sprite.Flipped = true;
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
                    sprite.Flipped = !sprite.Flipped;
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

                    sprite.Flipped = !sprite.Flipped;
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

        public void CalculateSpritePlane(RenderableWallSprite sprite, float yCeil, float yFloor, float yaw)
        {
            GameTextureInfo textureInfo = sprite.Texture;
            GameTexture texture = TextureCache.GetTexture(textureInfo);
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

                sprite.Flipped = true;
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
                    //sprite.Flipped = true;
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

                    //sprite.Flipped = true;
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

            {
                sprite.Flipped = sprite.Flipped ? rx2 * ry1 < ry2 * rx1 : rx2 * ry1 > ry2 * rx1;
            }

            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                (sprite.R1, sprite.R2) = (sprite.R2, sprite.R1);

                //sprite.Flipped = !sprite.Flipped;
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

        public void CalculateFloorPlane(RenderableFloorSprite sprite, float yCeil, float yFloor, float yaw)
        {
            (Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight) = (sprite.R1, sprite.R2, sprite.R3, sprite.R4);

            FloorSpriteWallInfo topWall = CalculateWallPlane(topLeft, topRight, yCeil, yFloor, yaw);
            FloorSpriteWallInfo rightWall = CalculateWallPlane(topRight, bottomRight, yCeil, yFloor, yaw);
            FloorSpriteWallInfo bottomWall = CalculateWallPlane(bottomRight, bottomLeft, yCeil, yFloor, yaw);
            FloorSpriteWallInfo leftWall = CalculateWallPlane(bottomLeft, topLeft, yCeil, yFloor, yaw);

            if (!topWall.IntersectsView && !rightWall.IntersectsView && !bottomWall.IntersectsView && !leftWall.IntersectsView)
            {
                return;
            }

            (int xLeft, int xRight) = DetermineBounds(topWall, rightWall, bottomWall, leftWall);

            sprite.Wall1 = topWall;
            sprite.Wall2 = rightWall;
            sprite.Wall3 = bottomWall;
            sprite.Wall4 = leftWall;
            sprite.XLeft = xLeft;
            sprite.XRight = xRight;
            sprite.IntersectsView = true;

            return;

            static (int from, int to) DetermineBounds(params ReadOnlySpan<FloorSpriteWallInfo> spriteBounds)
            {
                int from = int.MaxValue;
                int to = int.MinValue;

                for (int i = 0; i < spriteBounds.Length; i++)
                {
                    var spriteBound = spriteBounds[i];

                    if (spriteBound.IntersectsView)
                    {
                        from = Math.Min(from, spriteBound.XLeft);
                        to = Math.Max(to, spriteBound.XRight);
                    }
                }

                return (from, to);
            }
        }

        private FloorSpriteWallInfo CalculateWallPlane(Vector2 r1, Vector2 r2, float yCeil, float yFloor, float yaw)
        {
            // calculate the x, y for the wall on the screen for both points
            (float rx1, float ry1) = r1;
            (float rx2, float ry2) = r2;

            float xLeft, xRight, yLeftFloor, yRightFloor;
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

                // (wall.R1, wall.R2) = (wall.R2, wall.R1);
            }

            FloorSpriteWallInfo spriteWallInfo = ObjectPool.FloorSpriteWallInfo.GetOrCreate();

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

                    // wall.IntersectsView = true;
                    // wall.Flipped = true;
                    spriteWallInfo.IntersectsView = true;
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

                    // wall.Flipped = true;
                }
                else
                {
                    // despite one the wall going behind the player's view
                    // player's view doesn't intersect at corners
                    // we assume wall can't be drawn
                    // wall.IntersectsView = false;
                    return spriteWallInfo;
                }
            }

            if (xLeft == xRight)
            {
                // wall.IntersectsView = false;
                return spriteWallInfo;
            }

            Clamp(ref xLeft, ref xRight);

            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                // (wall.R1, wall.R2) = (wall.R2, wall.R1);
                // wall.Flipped = !wall.Flipped;
            }

            spriteWallInfo.IntersectsView |= MathFormulas.CalculatePlaneIntersectionsForWall(width, xLeft, xRight, ref rx1, ref ry1, ref rx2, ref ry2);

            if (spriteWallInfo.IntersectsView)
            {
                yLeftFloor = halfHeight - (yFloor / ry1 - yaw) * height;
                yRightFloor = halfHeight - (yFloor / ry2 - yaw) * height;

                // wall.C1 = new(rx1, ry1);
                // wall.C2 = new(rx2, ry2);

                spriteWallInfo.XLeft = float.ConvertToIntegerNative<int>(xLeft);
                spriteWallInfo.XRight = float.ConvertToIntegerNative<int>(xRight);
                spriteWallInfo.YLeftFloor = float.ConvertToIntegerNative<int>(yLeftFloor);
                spriteWallInfo.YRightFloor = float.ConvertToIntegerNative<int>(yRightFloor);

            }

            return spriteWallInfo;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Clamp(ref float xLeft, ref float xRight)
            {
                xLeft = Math.Clamp(xLeft, 0f, width - 1f);
                xRight = Math.Clamp(xRight, 0f, width - 1f);
            }
        }

        #endregion
    }
}
