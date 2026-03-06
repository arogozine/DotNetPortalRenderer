using RenderingEngine.Models;
using RenderingEngine.Models.Rendering;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal static class MathFormulas
    {
        internal static Vector3 ToVector3(Point p, float z)
        {
            return new Vector3(p.X, p.Y, z);
        }

        internal static (Vector3 Point1, Vector3 Normal) CalculatePlaneNormalFloor(Sector sector)
        {
            Point p3 = sector.Walls[2].R1;
            Point p2 = sector.Walls[0].R2;
            Point p1 = sector.Walls[0].R1;

            Debug.Assert(p1 != p2);
            Debug.Assert(p1 != p3);
            Debug.Assert(p2 != p3);

            (float p3z, _) = CalculateZAtPoint(sector, p3);

            Vector3 p3v = ToVector3(p3, p3z);
            Vector3 p2v = ToVector3(p2, sector.Floor);
            Vector3 p1v = ToVector3(p1, sector.Floor);


            Vector3 vec1 = p2v - p1v;
            Vector3 vec2 = p3v - p1v;


            return (p1v, Vector3.Cross(vec1, vec2));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="planePoint">A point on the plane</param>
        /// <param name="planeNormal">The normal vector of the plane</param>
        /// <param name="linePoint">A point on the line (ray origin)</param>
        /// <param name="intersectionPoint">The resulting intersection point</param>
        /// <returns></returns>
        internal static bool FindIntersection(
            Vector3 planePoint,
            Vector3 planeNormal,
            Vector3 linePoint,
            Vector3 lineDirection,
            out Vector3 intersectionPoint)
        {
            float denominator = Vector3.Dot(lineDirection, planeNormal);

            if (MathF.Abs(denominator) < 0.00001f)
            {
                intersectionPoint = Vector3.Zero;
                return false;
            }

            Vector3 pointToPlaneVector = planePoint - linePoint;

            float t = Vector3.Dot(pointToPlaneVector, planeNormal) / denominator;

            intersectionPoint = linePoint + lineDirection * t;

            return true;
        }

        /// <summary>
        /// Try get intersection when player is at (0, 0)
        /// </summary>
        /// <param name="rayDirX">Camera X Direction from -1 to 1</param>
        /// <param name="rx1"></param>
        /// <param name="ry1"></param>
        /// <param name="d2x"></param>
        /// <param name="d2y"></param>
        /// <param name="distanceX">Intersection X Coordinate</param>
        /// <param name="distanceY">Intersection Y Coordinate</param>
        /// <returns>Whether or not ray intersects wall</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetSegmentIntersectionZero2(
            float rayDirX,
            float rx1, float ry1,
            float d2x, float d2y,
            out float distanceX,
            out float distanceY)
        {
            Unsafe.SkipInit(out distanceX);
            Unsafe.SkipInit(out distanceY);

            float denominator = MathF.FusedMultiplyAdd(rayDirX, d2y, - d2x);

            if (MathF.Abs(denominator) < float.Epsilon)
            {
                return false;
            }

            float u = (rx1 - ry1 * rayDirX) / denominator;

            if (u < 0f || u > 1f)
            {
                return false;
            }

            float t = MathF.FusedMultiplyAdd(rx1, d2y, - ry1 * d2x) / denominator;

            if (t < 0f)
            {
                return false;
            }

            distanceY = t;
            distanceX = t * rayDirX;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (float CameraRay, float CameraRayIncr, float t1, float d2y, float d2x) CalculateCameraRay(IWallLike wall, int width, int wallFromX)
        {
            float cameraWidthIncr = 2.0f / width;
            float rx1 = wall.R1.X;
            float ry1 = wall.R1.Y;
            float d2x = wall.R2.X - rx1;
            float d2y = wall.R2.Y - ry1;
            float t1 = MathF.FusedMultiplyAdd(rx1, d2y, - ry1 * d2x);
            float cameraRay = -1f;
            cameraRay += cameraWidthIncr * wallFromX;

            return (cameraRay, cameraWidthIncr, t1, d2y, d2x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (float TextureLocation, float FromToYDist) CalculateDistance(
            IWallLike sprite,
            float cameraRay,
            float t1, float d2y, float d2x,
            bool flipX)
        {
            bool flipped = flipX ? !sprite.Flipped : sprite.Flipped;

            float denominator = MathF.FusedMultiplyAdd(cameraRay, d2y, - d2x);
            float fromToYDist = t1 / denominator;
            float fromToXDist = fromToYDist * cameraRay;

            float distX = flipped ? (sprite.R2.X - fromToXDist) : (fromToXDist - sprite.R1.X);
            float distY = flipped ? (sprite.R2.Y - fromToYDist) : (fromToYDist - sprite.R1.Y);

            float textureXLocation = MathF.Sqrt(distX * distX + distY * distY);

            return (textureXLocation, fromToYDist);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (float X, float Y) CalculateRayIntersection(float cameraRay, float t1, float d2y, float d2x)
        {
            float denominator = MathF.FusedMultiplyAdd(cameraRay, d2y, - d2x);
            float fromToYDist = t1 / denominator;
            float fromToXDist = fromToYDist * cameraRay;

            return (fromToXDist, fromToYDist);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<float> X, Vector<float> Y) CalculateRayIntersection(Vector<float> cameraRay, Vector<float> t1, Vector<float> d2y, Vector<float> d2x)
        {
            Vector<float> denominator = Vector.FusedMultiplyAdd(cameraRay, d2y, - d2x);
            Vector<float> fromToYDist = t1 / denominator;
            Vector<float> fromToXDist = fromToYDist * cameraRay;

            return (fromToXDist, fromToYDist);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<float> X, Vector<float> Y, Vector<float> Z) NormalizeVector(Vector<float> x, Vector<float> y, Vector<float> z)
        {
            // compute length = sqrt(x*x + y*y + z*z)
            Vector<float> sum = x * x + y * y + z * z;
            Vector<float> length = Vector.SquareRoot(sum);

            // Avoid division by zero: where length is very small, set inverse to zero
            Vector<float> inv = Vector<float>.One / length;
            Vector<int> smallLengthMask = Vector.LessThanOrEqual(length, new Vector<float>(float.Epsilon));
            inv = Vector.ConditionalSelect(smallLengthMask, Vector<float>.Zero, inv);

            return (x * inv, y * inv, z * inv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float CalculateDistance2(float cameraRay, float t1, float d2y, float d2x)
        {
            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;

            return fromToYDist;
        }

        internal static (float FloorZ, float CeilingZ) CalculateZAtPoint(Sector sector, Point point)
        {
            float ceilZ = sector.Ceil;
            float floorZ = sector.Floor;
            float floorSlope = sector.FloorSlope ?? 0f;
            float ceilingSlope = sector.CeilingSlope ?? 0f;

            if (floorSlope == 0f && ceilingSlope == 0f)
            {
                return (floorZ, ceilZ);
            }

            // PointA and PointB of first line
            RenderableWall firstWall = sector.Walls[0];
            Point pointA = firstWall.R1;
            Point pointB = firstWall.R2;

            float dx = pointB.X - pointA.X;
            float dy = pointB.Y - pointA.Y;

            float distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance == 0f)
            {
                return (floorZ, ceilZ);
            }

            // compute signed perpendicular from the reference line
            (float x, float y) = point;
            float offset = dx * (y - pointA.Y) - dy * (x - pointA.X);

            if (sector.Settings.HasFlag(MapSectorSettings.SlopeCeiling))
            {
                ceilZ += (ceilingSlope * offset) / distance;
            }

            if (sector.Settings.HasFlag(MapSectorSettings.SlopeFloor))
            {
                floorZ += (floorSlope * offset) / distance;
            }

            return (floorZ, ceilZ);
        }

        public sealed class FloorCeilSlope
        {
            public required float FloorZ { get; set; }
            public required float CeilZ { get; set; }
            public required float FloorZIncrament { get; init; }
            public required float CeilZIncrament { get; init; }
        }

        internal static (float floorZ_a, float ceilingZ_a, float floorZ_b, float ceilingZ_b) Test2(Sector sector, RenderableWall parentWall, bool flipped)
        {

            flipped = flipped ? !parentWall.Flipped : parentWall.Flipped;

            (float floorZ_a, float ceilingZ_a) = CalculateZAtPoint(sector, flipped ? parentWall.C2 : parentWall.C1);
            (float floorZ_b, float ceilingZ_b) = CalculateZAtPoint(sector, flipped ? parentWall.C1 : parentWall.C2);

            return (floorZ_a, ceilingZ_a, floorZ_b, ceilingZ_b);
        }

        internal static FloorCeilSlope Test(Sector sector, RenderableWall parentWall, int wallFromXOffset, bool flipped)
        {

            flipped = flipped ? !parentWall.Flipped : parentWall.Flipped;

            (float floorZ_a, float ceilingZ_a) = CalculateZAtPoint(sector, flipped ? parentWall.C2 : parentWall.C1);
            (float floorZ_b, float ceilingZ_b) = CalculateZAtPoint(sector, flipped ? parentWall.C1 : parentWall.C2);

            float wallLengthX = parentWall.XRight - parentWall.XLeft;

            float floorSlopeIncr = (floorZ_b - floorZ_a) / wallLengthX;
            float ceilingSlopeIncr = (ceilingZ_b - ceilingZ_a) / wallLengthX;

            if (wallFromXOffset != 0f)
            {
                if (!flipped)
                {
                    floorZ_a -= wallFromXOffset * floorSlopeIncr;
                    ceilingZ_a -= wallFromXOffset * ceilingSlopeIncr;
                }
                else
                {
                    floorZ_a += wallFromXOffset * floorSlopeIncr;
                    ceilingZ_a += wallFromXOffset * ceilingSlopeIncr;
                }
            }

            return new FloorCeilSlope {
                CeilZ = ceilingZ_a,
                FloorZ = floorZ_a,
                CeilZIncrament = ceilingSlopeIncr,
                FloorZIncrament = floorSlopeIncr
            };
        }

        internal static RenderablePlaneInfo CalculateLeftWallYPlaneInfo2(ReadOnlySpan<Sector> sectors, RenderableWall wall, int wallFromXOffset)
        {
            float wallLengthX = wall.XRight - wall.XLeft;
            float wallStartY = wall.YLeftCeil;
            float ceilDistIncr = (wall.YRightCeil - wallStartY) / wallLengthX;

            float wallEndY = wall.YLeftFloor;
            float floorDistIncr = (wall.YRightFloor - wallEndY) / wallLengthX;

            float? portalFromStartY = null, portalToStartY = null;
            float? portalFromIncr = null, portalToIncr = null;

            Sector? neighborSector = wall.IsPortal ? sectors[wall.Neighbor] : null;

            bool wallSloped = wall.IsPortal && ((wall.Sector.FloorSlope != null || wall.Sector.CeilingSlope != null) ||
                (neighborSector!.FloorSlope != null || neighborSector.CeilingSlope != null));

            if (wallSloped)
            {
                Sector sector = wall.Sector;
                float sectorHeight = sector.Ceil - sector.Floor;

                float portalFromEndY;
                float portalToEndY;

                // starting slope
                {

                    (float floorZ_a, float ceilingZ_a) = CalculateZAtPoint(sector, wall.C2);
                    (float floorZ_b, float ceilingZ_b) = CalculateZAtPoint(sector, wall.C1);

                    (float p_floorZ_a, float p_ceilingZ_a) = CalculateZAtPoint(neighborSector!, wall.C1);
                    (float p_floorZ_b, float p_ceilingZ_b) = CalculateZAtPoint(neighborSector!, wall.C2);

                    float pixelsPerHeightStart = (wall.YLeftFloor - wall.YLeftCeil) / sectorHeight;
                    float pixelsPerHeightEnd = (wall.YRightFloor - wall.YRightCeil) / sectorHeight;

                    float ceilOffsetStart = p_ceilingZ_a - ceilingZ_a;
                    float floorOffsetStart = p_floorZ_a - floorZ_a;

                    float ceilOffsetEnd = p_ceilingZ_b - ceilingZ_b;
                    float floorOffsetEnd = p_floorZ_b - floorZ_b;

                    float ceilPixelOffsetStart = pixelsPerHeightStart * ceilOffsetStart;
                    float floorPixelOffsetStart = pixelsPerHeightStart * floorOffsetStart;

                    float ceilPixelOffsetEnd = pixelsPerHeightEnd * ceilOffsetEnd;
                    float floorPixelOffsetEnd = pixelsPerHeightEnd * floorOffsetEnd;

                    portalFromStartY = wall.YLeftCeil - ceilPixelOffsetStart;
                    portalToStartY = wall.YLeftFloor - floorPixelOffsetStart;
                    portalFromEndY = wall.YRightCeil - ceilPixelOffsetEnd;
                    portalToEndY = wall.YRightFloor - floorPixelOffsetEnd;
                }

                portalFromIncr = (portalFromEndY - portalFromStartY.Value) / wallLengthX;
                portalToIncr = (portalToEndY - portalToStartY.Value) / wallLengthX;

                if (wallFromXOffset != 0)
                {
                    portalFromStartY += wallFromXOffset * portalFromIncr;
                    portalToStartY += wallFromXOffset * portalToIncr;
                }

            }

            if (wallFromXOffset != 0)
            {
                wallEndY += wallFromXOffset * floorDistIncr;
                wallStartY += wallFromXOffset * ceilDistIncr;
            }

            return new RenderablePlaneInfo
            {
                WallStartY = wallStartY,
                WallEndY = wallEndY,
                CeilDistIncr = ceilDistIncr,
                FloorDistIncr = floorDistIncr,
                PortalStartY = portalFromStartY,
                PortalEndY = portalToStartY,
                PortalStartIncr = portalFromIncr,
                PortalEndIncr = portalToIncr
            };
        }

        internal static RenderablePlaneInfo CalculateLeftWallYPlaneInfo(RenderableWall wall, int wallFromXOffset)
        {
            float wallLengthX = wall.XRight - wall.XLeft;
            float wallStartY = wall.YLeftCeil;
            float ceilDistIncr = (wall.YRightCeil - wallStartY) / wallLengthX;

            float wallEndY = wall.YLeftFloor;
            float floorDistIncr = (wall.YRightFloor - wallEndY) / wallLengthX;

            if (wallFromXOffset != 0)
            {
                wallEndY += wallFromXOffset * floorDistIncr;
                wallStartY += wallFromXOffset * ceilDistIncr;
            }

            return new RenderablePlaneInfo
            {
                WallStartY = wallStartY,
                WallEndY = wallEndY,
                CeilDistIncr = ceilDistIncr,
                FloorDistIncr = floorDistIncr

            };
        }

        internal static RenderablePlaneInfo CalculateLeftWallYPlaneInfo(IWallLike sprite, int wallFromXOffset)
        {
            float wallLengthX = sprite.XRight - sprite.XLeft;
            float wallStartY = sprite.YLeftCeil;
            float ceilDistIncr = (sprite.YRightCeil - wallStartY) / wallLengthX;

            float wallEndY = sprite.YLeftFloor;
            float floorDistIncr = (sprite.YRightFloor - wallEndY) / wallLengthX;

            if (wallFromXOffset != 0)
            {
                wallEndY += wallFromXOffset * floorDistIncr;
                wallStartY += wallFromXOffset * ceilDistIncr;
            }

            return new RenderablePlaneInfo
            {
                WallStartY = wallStartY,
                WallEndY = wallEndY,
                CeilDistIncr = ceilDistIncr,
                FloorDistIncr = floorDistIncr
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float ClampAngle(float angle)
        {
            const float twoPi = 2 * MathF.PI;

            while (angle > twoPi)
            {
                angle -= twoPi;
            }

            while (angle < 0f)
            {
                angle += twoPi;
            }

            return angle;
        }

        internal static bool CalculatePlaneIntersectionsForWall(int width, float xLeft, float xRight, ref float rx1, ref float ry1, ref float rx2, ref float ry2)
        {
            // Nothing To Render
            if (float.ConvertToIntegerNative<int>(xLeft) == float.ConvertToIntegerNative<int>(xRight))
            {
                return false;
            }

            float cameraWidthIncr = 2.0f / width;

            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;

            float rayDirLeft = MathF.FusedMultiplyAdd(cameraWidthIncr, xLeft, -1f);
            float rayDirRight = MathF.FusedMultiplyAdd(cameraWidthIncr, xRight, -1f);

            bool intersectsL = TryGetSegmentIntersectionZero2(rayDirLeft, rx1, ry1, d2x, d2y,
                out float xDistanceL, out float yDistanceL);

            bool intersectsR = TryGetSegmentIntersectionZero2(rayDirRight, rx2, ry2, -d2x, -d2y,
                out float xDistanceR, out float yDistanceR);

            if (intersectsL && intersectsR)
            {
                rx1 = xDistanceL;
                ry1 = yDistanceL;

                rx2 = xDistanceR;
                ry2 = yDistanceR;
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

    }
}
