using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        // pre-computed texture buffers
        private int columnABufferIndex = -1;
        private readonly uint[] columnA = new uint[512];
        private int columnBBufferIndex = -1;
        private readonly uint[] columnB = new uint[512];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetScreenPtr<T>()
            where T : struct
        {
            return ref Unsafe.As<BGRA, T>(ref MemoryMarshal.GetReference(this.buffer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref uint GetBufferA(int size, out Span<uint> buffer)
        {
            buffer = columnA.AsSpan(..size);
            return ref MemoryMarshal.GetReference(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref uint GetBufferB(int size, out Span<uint> buffer)
        {
            buffer = columnB.AsSpan(..size);
            return ref MemoryMarshal.GetReference(buffer);
        }

        private void CalculateDistance(RenderablePortalWall renderableWall)
        {
            int width = PixelWidth;
            var wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<float> distance = RenderWindowHelper.Distance;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                (int portalFromYClamped, int portalToYClamped) = RenderWindowHelper.GetClampedWallFromTo(x);
                distance[x] = CalculateDistance2(cameraRay, t1, d2y, d2x);
                ceilingStart[x] = portalFromYClamped;
                floorEnd[x] = portalToYClamped;
                status[x] ^= RenderColumnStatus.CanRenderWall;
            }
        }

        private bool DrawPortalWall(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            int sectorHeight = float.ConvertToIntegerNative<int>(sector.Ceil - sector.Floor);
            RenderableWall wall = renderableWall.Wall;

            Sector neighborSector = sectors[wall.Neighbor];
            float oneOverSectorHeight = 1f / sectorHeight;
            int floorOffset = float.ConvertToIntegerNative<int>(neighborSector.Floor - sector.Floor);
            int ceilOffset = float.ConvertToIntegerNative<int>(neighborSector.Ceil - sector.Ceil);
            byte lightLevel = sector.LightLevel;

            if (floorOffset < 0)
            {
                floorOffset = 0;
            }

            if (ceilOffset > 0)
            {
                ceilOffset = 0;
            }

            // don't draw beyond the bounds
            if (ceilOffset < -sectorHeight)
            {
                ceilOffset = -sectorHeight;
            }

            if (floorOffset > sectorHeight)
            {
                floorOffset = sectorHeight;
            }

            // ceiling and floor of the sector are the same
            // so no wall is drawn
            if (floorOffset == 0 && ceilOffset == 0)
            {
                CalculateDistance(renderableWall);
                return true;
            }

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            ReadOnlySpan<int> bottomTextureYLocation = RenderWindowHelper.BottomTextureYLocation;
            ReadOnlySpan<int> topTextureYLocation = RenderWindowHelper.TopTextureYLocation;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<int> bottomTextureXLocation = RenderWindowHelper.BottomTextureXLocation;
            Span<int> topTextureXLocation = RenderWindowHelper.TopTextureXLocation;

            if (floorOffset != 0 && ceilOffset != 0)
            {
                PrecalculatePortalWallDistance(sector, renderableWall);
            }
            else if (floorOffset != 0)
            {
                PrecalculateLowerWallDistance(sector, renderableWall);
            }
            else
            {
                PrecalculateUpperWallDistance(sector, renderableWall);
            }

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            TextureInfo upperTextureInfo = wall.UpperTexture!;
            TextureInfo lowerTextureInfo = wall.LowerTexture!;

            (bool upperSkybox, _, bool upperFlipY) = GetFlags(upperTextureInfo);
            (bool lowerSkybox, _, bool lowerFlipY) = GetFlags(lowerTextureInfo);

            Texture upperTexture = TextureCache.GetTexture(upperTextureInfo);
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetArrayDataReference(upperSkybox ? upperTexture.Data : upperTexture.Rotated);
            ref uint upperTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref upperTexturePtr);

            Texture lowerTexture = TextureCache.GetTexture(lowerTextureInfo);
            ref BGRA lowerTexturePtr = ref MemoryMarshal.GetArrayDataReference(lowerTexture.Rotated);

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ref uint lowerTextureBufferPtr = ref GetBufferA(lowerTexture.Height, out Span<uint> lowerTextureBuffer);
            ref uint upperTextureBufferPtr = ref GetBufferB(upperTexture.Height, out Span<uint> upperTextureBuffer);

            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            int lowerTextureStart = lowerTextureInfo.YOffset << 16;
            int upperTextureStart = upperTextureInfo.YOffset << 16;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = RenderWindowHelper.Status[x];

                if (!columnStatus.WallRenderable)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Wall Calculation
                (int fromYClamped, int toYClamped) = RenderWindowHelper.GetClampedWallFromTo(x);

                // Portal Calculation
                int floorPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * floorOffset);
                int ceilPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * ceilOffset);
                int portalFromY = wallStartY - ceilPixelOffset;
                int portalToY = wallEndY - floorPixelOffset;
                int portalFromYClamped = Math.Clamp(portalFromY, ceilingStartY, floorEndY);
                int portalToYClamped = Math.Clamp(portalToY, ceilingStartY, floorEndY);

                // draw upper wall / upper skybox
                if (ceilOffset != 0 && fromYClamped < portalFromYClamped)
                {
                    if (upperSkybox)
                    {
                        ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, fromYClamped * width + x);
                        ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, portalFromYClamped * width + x);

                        RenderSkyboxLine(player,
                            x,
                            in upperTexture,
                            ref upperTextureUintPtr,
                            ref angleCachePtr,
                            ref screenIndexPtr,
                            ref screenIndexPtrEnd);
                    }
                    else
                    {
                        int textureYPos = topTextureXLocation[x];
                        int textureXIncr = topTextureYLocation[x];

                        // Calculate Upper  Texture Position
                        int textureWidth = upperTexture.Height;
                        int textureXPos = upperTextureStart - textureXIncr * (wallStartY - fromYClamped);

                        textureXPos = EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                        CalculateAndCacheWallColumn(upperTextureBuffer, ref columnABufferIndex, ref upperTexturePtr, textureYPos, lightLevel, upperFlipY);

                        RenderWallLine(
                            width,
                            x,
                            textureWidth,
                            fromYClamped,
                            portalFromYClamped,
                            (uint)textureXPos,
                            (uint)textureXIncr,
                            ref screenPtr,
                            ref upperTextureBufferPtr
                        );
                    }
                }

                // draw lower wall
                if (floorOffset != 0 && portalToYClamped < toYClamped)
                {
                    if (lowerSkybox)
                    {
                        ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToYClamped * width + x);
                        ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, toYClamped * width + x);

                        RenderSkyboxLine(player,
                            x,
                            in upperTexture,
                            ref upperTextureUintPtr,
                            ref angleCachePtr,
                            ref screenIndexPtr,
                            in screenIndexPtrEnd);
                    }
                    else
                    {
                        int textureYPos = bottomTextureXLocation[x];
                        int textureXIncr = bottomTextureYLocation[x];

                        int textureWidth = lowerTexture.Height;

                        int textureXPos = textureXIncr * (portalToYClamped - portalToY) + lowerTextureStart;

                        textureXPos = EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                        CalculateAndCacheWallColumn(lowerTextureBuffer, ref columnBBufferIndex, ref lowerTexturePtr, textureYPos, lightLevel, lowerFlipY);

                        RenderWallLine(
                            width,
                            x,
                            textureWidth,
                            portalToYClamped,
                            toYClamped,
                            (uint)textureXPos,
                            (uint)textureXIncr,
                            ref screenPtr,
                            ref lowerTextureBufferPtr
                        );
                    }
                }

                ceilingStart[x] = portalFromYClamped;
                floorEnd[x] = portalToYClamped;
                status[x] ^= RenderColumnStatus.CanRenderWall;
            }

            columnABufferIndex = EngineConstants.Unset;
            columnBBufferIndex = EngineConstants.Unset;

            // if sector height matches top or bottom offset only top or bottom texture was drawn
            // no middle texture is possible, thus we can treat this as basic wall
            return !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);
        }

        private void PrecalculateUpperWallDistance(
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            Span<float> distance = RenderWindowHelper.Distance;
            Span<int> topXLocation = RenderWindowHelper.TopTextureXLocation;
            Span<int> topYLocation = RenderWindowHelper.TopTextureYLocation;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            ReadOnlySpan<RenderColumnStatus> status = RenderWindowHelper.Status;

            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;

            bool flipX = wall.UpperTexture!.RenderingOptions.IsFlippedX;
            flipX = wall.Flipped ? !flipX : flipX;

            int xOffsetUpper = wall.UpperTexture!.XOffset;
            (int upperTexWidth, int upperTexHeight, float upperXScale, float upperScaledTextureWidth) = CalculateScale(sector, wall, wall.UpperTexture);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            float rX1 = flipX ? wall.R2.X : wall.R1.X;
            float rY1 = flipX ? wall.R2.Y : wall.R1.Y;

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                (float fromToXdist, float fromToYdist) = CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];

                float distX = rX1 - fromToXdist;
                float distY = rY1 - fromToYdist;
                float textureDist = MathF.Sqrt(distX * distX + distY * distY);

                distance[x] = fromToYdist;
                topXLocation[x] = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, upperXScale, xOffsetUpper));
                topYLocation[x] = float.ConvertToIntegerNative<int>(upperScaledTextureWidth / (wallEndY - wallStartY));
                topXLocation[x] = (topXLocation[x] % upperTexHeight) * upperTexWidth;
            }
        }

        private void PrecalculateLowerWallDistance(
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            Span<float> distance = RenderWindowHelper.Distance;
            Span<int> bottomXLocation = RenderWindowHelper.BottomTextureXLocation;
            Span<int> bottomYLocation = RenderWindowHelper.BottomTextureYLocation;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            ReadOnlySpan<RenderColumnStatus> status = RenderWindowHelper.Status;

            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;

            bool flipX = wall.LowerTexture!.RenderingOptions.IsFlippedX;
            flipX = wall.Flipped ? !flipX : flipX;
            int xOffsetLower = wall.LowerTexture!.XOffset;

            (int lowerTexWidth, int lowerTexHeight, float lowerXScale, float lowerScaledTextureWidth) = CalculateScale(sector, wall, wall.LowerTexture);
            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            float rX2 = flipX ? wall.R2.X : wall.R1.X;
            float rY2 = flipX ? wall.R2.Y : wall.R1.Y;

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                (float fromToXdist, float fromToYdist) = CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];

                float distX = rX2 - fromToXdist;
                float distY = rY2 - fromToYdist;
                float textureDist = MathF.Sqrt(distX * distX + distY * distY);

                distance[x] = fromToYdist;

                bottomXLocation[x] = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, lowerXScale, xOffsetLower));
                bottomYLocation[x] = float.ConvertToIntegerNative<int>(lowerScaledTextureWidth / (wallEndY - wallStartY));
                bottomXLocation[x] = (bottomXLocation[x] % lowerTexHeight) * lowerTexWidth;
            }
        }

        private void PrecalculatePortalWallDistance(
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            Span<float> distance = RenderWindowHelper.Distance;
            Span<int> topXLocation = RenderWindowHelper.TopTextureXLocation;
            Span<int> bottomXLocation = RenderWindowHelper.BottomTextureXLocation;
            Span<int> topYLocation = RenderWindowHelper.TopTextureYLocation;
            Span<int> bottomYLocation = RenderWindowHelper.BottomTextureYLocation;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            ReadOnlySpan<RenderColumnStatus> status = RenderWindowHelper.Status;

            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;
            bool flipXUpper = wall.UpperTexture!.RenderingOptions.IsFlippedX;
            bool flipXLower = wall.LowerTexture!.RenderingOptions.IsFlippedX;

            flipXUpper = wall.Flipped ? !flipXUpper : flipXUpper;
            flipXLower = wall.Flipped ? !flipXLower : flipXLower;

            int xOffsetUpper = wall.UpperTexture!.XOffset;
            int xOffsetLower = wall.LowerTexture!.XOffset;

            (int upperTexWidth, int upperTexHeight, float upperXScale, float upperScaledTextureWidth) = CalculateScale(sector, wall, wall.UpperTexture);
            (int lowerTexWidth, int lowerTexHeight, float lowerXScale, float lowerScaledTextureWidth) = CalculateScale(sector, wall, wall.LowerTexture);


            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            float rX1 = flipXUpper ? wall.R2.X : wall.R1.X;
            float rY1 = flipXUpper ? wall.R2.Y : wall.R1.Y;
            float rX2 = flipXLower ? wall.R2.X : wall.R1.X;
            float rY2 = flipXLower ? wall.R2.Y : wall.R1.Y;

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                (float fromToXdist, float fromToYdist) = CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];

                float distX1 = rX1 - fromToXdist;
                float distY1 = rY1 - fromToYdist;
                float distX2 = rX2 - fromToXdist;
                float distY2 = rY2 - fromToYdist;

                distance[x] = fromToYdist;
                topXLocation[x] = float.ConvertToIntegerNative<int>(MathF.Sqrt(distX1 * distX1 + distY1 * distY1) * upperXScale + xOffsetUpper);
                bottomXLocation[x] = float.ConvertToIntegerNative<int>(MathF.Sqrt(distX2 * distX2 + distY2 * distY2) * lowerXScale + xOffsetLower);

                topYLocation[x] = float.ConvertToIntegerNative<int>(upperScaledTextureWidth / (wallEndY - wallStartY));
                bottomYLocation[x] = float.ConvertToIntegerNative<int>(lowerScaledTextureWidth / (wallEndY - wallStartY));

                topXLocation[x] = (topXLocation[x] % upperTexHeight) * upperTexWidth;
                bottomXLocation[x] = (bottomXLocation[x] % lowerTexHeight) * lowerTexWidth;
            }
        }

        private static (int Width, int Height, float XScale, float ScaledTextureWidth) CalculateScale(
            Sector sector,
            RenderableWall wall,
            TextureInfo textureInfo)
        {
            Texture wallTexture = TextureCache.GetTexture(textureInfo);
            int textureWidth = wallTexture.Height;
            int textureHeight = wallTexture.Width;

            if (textureInfo.XScale is float xScale)
            {
                float wallLength = wall.Length;
                xScale = xScale / wallLength * textureHeight;
            }
            else
            {
                xScale = 1f;
            }

            float scaledTextureWidth;

            if (textureInfo.YScale is float yScale)
            {
                yScale = (sector.Ceil - sector.Floor) * yScale;
                scaledTextureWidth = ((textureWidth << 16) * yScale);
            }
            else
            {
                scaledTextureWidth = float.ConvertToIntegerNative<int>(sector.Ceil - sector.Floor) << 16;
            }

            return (textureWidth, textureHeight, xScale, scaledTextureWidth);
        }

        private void PrecalculateBasicWallDistance(RenderablePortalWall renderableWall, Sector sector)
        {
            Span<float> distance = RenderWindowHelper.Distance;
            Span<int> topXLocation = RenderWindowHelper.TopTextureXLocation;
            Span<int> topYLocation = RenderWindowHelper.TopTextureYLocation;
            Span<int> wallStart = RenderWindowHelper.WallStart;
            Span<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<RenderColumnStatus> status = RenderWindowHelper.Status;

            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;
            TextureInfo textureInfo = wall.MiddleTexture!;
            bool flipX = textureInfo.RenderingOptions.IsFlippedX;
            flipX = wall.Flipped ? !flipX : flipX;
            float xOffset = textureInfo.XOffset;

            (int textureWidth, int textureHeight, float xScale, float scaledTextureWidth) = CalculateScale(sector, wall, textureInfo);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            float rX = flipX ? wall.R2.X : wall.R1.X;
            float rY = flipX ? wall.R2.Y : wall.R1.Y;

            int length = (wallToX - wallFromX);

            if (Vector<float>.IsSupported && length > Vector<float>.Count)
            {
                const int canRenderWallMask = (int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall);

                Vector<int> canRenderWallMaskV = Vector.Create(canRenderWallMask);

                Span<int> statusInt = MemoryMarshal.Cast<RenderColumnStatus, int>(status);

                Vector<float> t1V = Vector.Create(t1);
                Vector<float> d2yV = Vector.Create(d2y);
                Vector<float> d2xV = Vector.Create(d2x);
                Vector<float> rXV = Vector.Create(rX);
                Vector<float> rYV = Vector.Create(rY);

                Vector<float> xScaleV = Vector.Create(xScale);
                Vector<float> xOffsetV = Vector.Create(xOffset);
                Vector<float> scaledTextureWidthV = Vector.Create(scaledTextureWidth);
                Vector<int> textureWidthV = Vector.Create(textureWidth);


                Vector<float> cameraRayV = Vector.CreateSequence(cameraRay, cameraWidthIncr);
                Vector<float> cameraWidthIncrV = Vector.Create(cameraWidthIncr * Vector<float>.Count);

                int rem = (wallToX - wallFromX) % Vector<float>.Count;
                wallToX -= rem;

                bool even = SharedHelpers.IsPowerOfTwo(textureHeight);
                Vector<int> heightMask = even ? Vector.Create(textureHeight - 1) : default;

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> columnStatusV = Vector.LoadUnsafe(ref statusInt[x]);

                    if ((columnStatusV & canRenderWallMaskV) == Vector<int>.Zero)
                    {
                        continue;
                    }

                    (Vector<float> fromToXdist, Vector<float> fromToYdist) = CalculateRayIntersection(cameraRayV, t1V, d2yV, d2xV);
                    Vector<float> distX = rXV - fromToXdist;
                    Vector<float> distY = rYV - fromToYdist;

                    Vector<int> wallStartV = Vector.LoadUnsafe(ref wallStart[x]);
                    Vector<int> wallEndV = Vector.LoadUnsafe(ref wallEnd[x]);

                    Vector<float> textureDist = Vector.SquareRoot(distX * distX + distY * distY);
                    Vector<int> topXLocationV = Vector.ConvertToInt32Native(Vector.FusedMultiplyAdd(textureDist, xScaleV, xOffsetV));
                    Vector<int> topYLocationV = Vector.ConvertToInt32Native(scaledTextureWidthV / Vector.ConvertToSingle(wallEndV - wallStartV));

                    Vector.StoreUnsafe(fromToYdist, ref distance[x]);
                    Vector.StoreUnsafe(topYLocationV, ref topYLocation[x]);

                    if (even)
                    {
                        topXLocationV = (topXLocationV & heightMask) * textureWidthV;
                        Vector.StoreUnsafe(topXLocationV, ref topXLocation[x]);
                    }
                    else
                    {
                        for (int i = 0; i < Vector<float>.Count; i++)
                        {
                            topXLocation[x + i] = (topXLocationV[i] % textureHeight) * textureWidth;
                        }
                    }

                    cameraRayV += cameraWidthIncrV;
                }

                wallFromX = wallToX;
                wallToX += rem;
                cameraRay = cameraRayV[0];
            }

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                (float fromToXdist, float fromToYdist) = CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];

                float distX = rX - fromToXdist;
                float distY = rY - fromToYdist;
                float textureDist = MathF.Sqrt(distX * distX + distY * distY);

                distance[x] = fromToYdist;
                topXLocation[x] = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, xScale, xOffset));
                topYLocation[x] = float.ConvertToIntegerNative<int>(scaledTextureWidth / (wallEndY - wallStartY));
                topXLocation[x] = (topXLocation[x] % textureHeight) * textureWidth;
            }
        }

        private bool DrawBasicWall(
            PortalPlayerSnapshot player,
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            // separate path for skybox rendering
            RenderableWall wall = renderableWall.Wall;
            TextureInfo textureInfo = wall.MiddleTexture!;

            (bool skybox, _, bool flipY) = GetFlags(textureInfo);

            if (skybox)
            {
                return DrawBasicSkyboxWall(player, renderableWall);
            }

            PrecalculateBasicWallDistance(renderableWall, sector);

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            byte lightLevel = sector.LightLevel;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            Texture wallTexture = TextureCache.GetTexture(textureInfo);
            ref BGRA wallTexturePtr = ref MemoryMarshal.GetArrayDataReference(wallTexture.Rotated);
            int textureWidth = wallTexture.Height;

            int textureStart = textureInfo.YOffset << 16;
            ref uint columnBufferPtr = ref GetBufferA(textureWidth, out Span<uint> columnBuffer);

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            ReadOnlySpan<int> textureXLocation = RenderWindowHelper.TopTextureXLocation;
            ReadOnlySpan<int> textureYLocation = RenderWindowHelper.TopTextureYLocation;
            ReadOnlySpan<int> ceilingStart = RenderWindowHelper.CeilingStart;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            ReadOnlySpan<int> floorEnd = RenderWindowHelper.FloorEnd;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                int textureYPos = textureXLocation[x];
                int textureXIncr = textureYLocation[x];
                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);
                int textureXPos = textureStart - textureXIncr * (wallStartY - clamptedFromY);

                CalculateAndCacheWallColumn(columnBuffer, ref columnABufferIndex, ref wallTexturePtr, textureYPos, lightLevel, flipY);

                textureXPos = EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                RenderWallLine(
                    width,
                    x,
                    textureWidth,
                    clamptedFromY,
                    clamptedToY,
                    (uint)textureXPos,
                    (uint)textureXIncr,
                    ref screenPtr,
                    ref columnBufferPtr
                );

                status[x] = RenderColumnStatus.FinishedRendering;
            }

            columnABufferIndex = EngineConstants.Unset;

            return true;
        }

        private bool DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall)
        {
            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<float> distance = RenderWindowHelper.Distance;

            int width = PixelWidth;
            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            Texture wallTexture = TextureCache.GetTexture(wall.MiddleTexture);
            ref uint wallTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetArrayDataReference(wallTexture.Data));
            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    distance[x] = CalculateDistance2(cameraRay, t1, d2y, d2x);
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                (int clamptedFromY, int clamptedToY) = RenderWindowHelper.GetClampedWallFromTo(x);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, clamptedToY * width + x);

                float fromToYdist = CalculateDistance2(cameraRay, t1, d2y, d2x);

                RenderSkyboxLine(player,
                    x,
                    in wallTexture,
                    ref wallTextureUintPtr,
                    ref angleCachePtr,
                    ref screenIndexPtr,
                    ref screenIndexPtrEnd);

                distance[x] = fromToYdist;
                status[x] = RenderColumnStatus.FinishedRendering;
            }

            return true;
        }

        #region Render Line

        private void RenderSkyboxLine(PortalPlayerSnapshot player,
            int x,
            in Texture upperTexture,
            ref uint upperTextureUintPtr,
            ref float angleCachePtr,
            ref uint screenIndexPtr,
            ref readonly uint screenIndexPtrEnd)
        {
            const float twoPi = 2 * MathF.PI;
            const float oneOverTwoPi = 1f / (2 * MathF.PI);

            int width = PixelWidth;
            int height = PixelHeight;

            float viewAngle = player.Angle;

            int textureWidth = upperTexture.Width;
            int textureHeight = upperTexture.Height;

            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;
            float yTextureIncr = (1f / height) * textureHeight;

            // calculate angle between 0 to 2 PI
            float angleX = Unsafe.Add(ref angleCachePtr, x) - viewAngle;
            if (angleX > twoPi)
            {
                angleX -= twoPi;
            }
            else if (angleX < 0f)
            {
                angleX = twoPi + angleX;
            }

            int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) % textureWidth;

            int wallStart = RenderWindowHelper.WallStart[x];
            int ceilingStart = RenderWindowHelper.CeilingStart[x];
            int floorEnd = RenderWindowHelper.FloorEnd[x];
            int fromYClamped = Math.Clamp(wallStart, ceilingStart, floorEnd);
            float vScreen = (float)fromYClamped * yTextureIncr;

            ref uint textureColumnPtr = ref Unsafe.Add(ref upperTextureUintPtr, texX);

            for (;
                    Unsafe.IsAddressGreaterThan(in screenIndexPtrEnd, in screenIndexPtr);
                    vScreen += yTextureIncr, screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width)
                )
            {
                int index = textureWidth * float.ConvertToIntegerNative<int>(vScreen);
                screenIndexPtr = Unsafe.Add(ref textureColumnPtr, index);
            }
        }

        private static void RenderWallLine(
            int width,
            int x,
            int textureHeight,
            int startY,
            int endY,
            uint textureXPos_u,
            uint textureXIncr_u,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
            )
        {
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, startY * width + x);
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, endY * width + x);

            // % is slower than the bitwise &
            // thus we have two paths to render a wall line
            // depending if texture is power of two or not
            if (SharedHelpers.IsPowerOfTwo(textureHeight))
            {
                uint textureMask = (uint)(textureHeight - 1);

                while (!Unsafe.AreSame(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    uint texelIndex = (textureXPos_u >> 16) & textureMask;
                    uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos_u += textureXIncr_u;
                }
            }
            else
            {
                uint textureHeight_u = (uint)textureHeight;

                while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    uint texelIndex = (textureXPos_u >> 16) % textureHeight_u;
                    uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos_u += textureXIncr_u;
                }
            }
        }

        #endregion

        #region Calculation Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EnsureOffsetIsPositive(int textureHeight, int offset)
        {
            offset %= textureHeight;

            if (offset < 0)
            {
                offset = textureHeight + offset;
            }

            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateAndCacheWallColumn(
            scoped Span<uint> buffer,
            ref int bufferIndex,
            scoped ref BGRA wallTexturePtr,
            int textureYPos, byte brightness, bool flipY)
        {
            // reuse the cached column
            if (bufferIndex == textureYPos)
            {
                return;
            }

            const uint Alpha = (uint)byte.MaxValue << 24;

            bufferIndex = textureYPos;

            // avoid calculating if too far away (all black)
            if (brightness == byte.MinValue)
            {
                buffer.Fill(Alpha);
                return;
            }

            ref BGRA columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);
            uint scale = (uint)brightness;

            if (flipY)
            {
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    uint b = columnPtr.B * scale >> 8;
                    uint g = columnPtr.G * scale >> 8 << 8;
                    uint r = columnPtr.R * scale >> 8 << 16;
                    buffer[i] = b | g | r | Alpha;

                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    uint b = columnPtr.B * scale >> 8;
                    uint g = columnPtr.G * scale >> 8 << 8;
                    uint r = columnPtr.R * scale >> 8 << 16;
                    buffer[i] = b | g | r | Alpha;

                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RenderablePlaneInfo CalculateLeftWallYPlaneInfo(RenderableWall wall, int wallFromXOffset)
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

            return new RenderablePlaneInfo(wallStartY, wallEndY, ceilDistIncr, floorDistIncr);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RenderablePlaneInfo CalculateLeftWallYPlaneInfo(RenderableSprite sprite, int wallFromXOffset)
        {
            wallFromXOffset = 0;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (float CameraRay, float CameraRayIncr, float t1, float d2y, float d2x) CalculateCameraRay(RenderableWall wall, int width, int wallFromX)
        {
            float cameraWidthIncr = 2.0f / width;
            float rx1 = wall.R1.X;
            float ry1 = wall.R1.Y;
            float d2x = wall.R2.X - rx1;
            float d2y = wall.R2.Y - ry1;
            float t1 = rx1 * d2y - ry1 * d2x;
            float cameraRay = -1f;
            cameraRay += cameraWidthIncr * wallFromX;

            return (cameraRay, cameraWidthIncr, t1, d2y, d2x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (float CameraRay, float CameraRayIncr, float t1, float d2y, float d2x) CalculateCameraRay(RenderableSprite sprite, int width, int wallFromX)
        {
            float cameraWidthIncr = 2.0f / width;
            float rx1 = sprite.R1.X;
            float ry1 = sprite.R1.Y;
            float d2x = sprite.R2.X - rx1;
            float d2y = sprite.R2.Y - ry1;
            float t1 = rx1 * d2y - ry1 * d2x;
            float cameraRay = -1f;
            cameraRay += cameraWidthIncr * wallFromX;

            return (cameraRay, cameraWidthIncr, t1, d2y, d2x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (float X, float Y) CalculateRayIntersection(float cameraRay, float t1, float d2y, float d2x)
        {
            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;
            float fromToXDist = fromToYDist * cameraRay;

            return (fromToXDist, fromToYDist);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector<float> X, Vector<float> Y) CalculateRayIntersection(Vector<float> cameraRay, Vector<float> t1, Vector<float> d2y, Vector<float> d2x)
        {
            Vector<float> denominator = cameraRay * d2y - d2x;
            Vector<float> fromToYDist = t1 / denominator;
            Vector<float> fromToXDist = fromToYDist * cameraRay;

            return (fromToXDist, fromToYDist);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int TextureLocation, float FromToYDist) CalculateDistance(
            RenderableWall wall,
            float cameraRay,
            float t1, float d2y, float d2x,
            bool flipX)
        {
            bool flipped = flipX ? !wall.Flipped : wall.Flipped;

            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;
            float fromToXDist = fromToYDist * cameraRay;

            float distX = flipped ? (wall.R2.X - fromToXDist) : (fromToXDist - wall.R1.X);
            float distY = flipped ? (wall.R2.Y - fromToYDist) : (fromToYDist - wall.R1.Y);

            float textureXLocation = MathF.Sqrt(distX * distX + distY * distY);

            return (float.ConvertToIntegerNative<int>(textureXLocation), fromToYDist);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (float TextureLocation, float FromToYDist) CalculateDistance(
            RenderableSprite sprite,
            float cameraRay,
            float t1, float d2y, float d2x,
            bool flipX)
        {
            bool flipped = flipX ? !sprite.Flipped : sprite.Flipped;

            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;
            float fromToXDist = fromToYDist * cameraRay;

            float distX = flipped ? (sprite.R2.X - fromToXDist) : (fromToXDist - sprite.R1.X);
            float distY = flipped ? (sprite.R2.Y - fromToYDist) : (fromToYDist - sprite.R1.Y);

            float textureXLocation = MathF.Sqrt(distX * distX + distY * distY);

            return (textureXLocation, fromToYDist);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CalculateDistance2(float cameraRay, float t1, float d2y, float d2x)
        {
            float denominator = cameraRay * d2y - d2x;
            float fromToYDist = t1 / denominator;

            return fromToYDist;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool IsSkybox, bool FlipX, bool FlipY) GetFlags(TextureInfo textureInfo)
        {
            if (textureInfo is null)
            {
                return (false, false, false);
            }

            TextureRenderingOptions options = textureInfo.RenderingOptions;

            bool skyBox = options.HasFlag(TextureRenderingOptions.Skybox);
            bool flipX = options.HasFlag(TextureRenderingOptions.FlipX);
            bool flipY = options.HasFlag(TextureRenderingOptions.FlipY);

            return (skyBox, flipX, flipY);
        }

        #endregion
    }
}