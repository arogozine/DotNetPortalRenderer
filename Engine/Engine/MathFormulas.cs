using RenderingEngine.Models;
using RenderingEngine.Models.Rendering;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal static class MathFormulas
    {
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
        internal static float CalculateDistance2(float cameraRay, float t1, float d2y, float d2x)
        {
            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;

            return fromToYDist;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            return new RenderablePlaneInfo(wallStartY, wallEndY, ceilDistIncr, floorDistIncr);
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
