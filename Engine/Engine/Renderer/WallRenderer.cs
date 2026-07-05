using RenderingEngine.Tooling;
using SoftwareRendererModels;

namespace RenderingEngine.Engine
{
    internal unsafe partial class PortalRenderer
    {
        private void DrawWallShared(
            RenderablePortalWall renderableWall,
            GameTextureInfo textureInfo,
            scoped Span<ushort> repeatedCount,
            short? shade,
            uint* fromYClamped,
            uint* toYClamped)
        {
            RenderableWall wall = renderableWall.Wall;

            bool flipY = textureInfo.RenderingOptions.IsFlippedY;
            bool flipX = textureInfo.RenderingOptions.IsFlippedX;

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            uint* screenPtr = (uint*)buffer;

            var transform = TextureTransform.Rotated;

            if (flipY)
            {
                transform |= TextureTransform.FlippedY;
            }

            if (flipX)
            {
                transform |= TextureTransform.FlippedX;
            }

            uint* textureYPosPtr = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureXLocation = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureXLocation);
            uint* textureYIncrementPtr = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);

            ref uint wallTextureRef = ref textureInfo.Texture.GetBinaryRef<uint>(textureInfo.Palette, shade ?? default, transform);
            int textureWidth = textureInfo.Height;

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            bool isPowerOfTwo = SharedHelpers.IsPowerOfTwo(textureInfo.Height);

            fixed (uint* wallTexturePtr = &wallTextureRef)
            fixed (ushort* repeatedCountPtr = &repeatedCount[0])
            {
                if (isPowerOfTwo)
                {
                    CoreRendererForPowTextures<DrawSimplePixel>.RenderWall(wallFromX, wallToX, (uint)width, textureWidth, repeatedCountPtr,
                        wallTexturePtr, screenPtr,
                        fromYClamped, toYClamped, textureXLocation, textureYPosPtr, textureYIncrementPtr);
                }
                else
                {
                    CoreRendererForOddTextures<DrawSimplePixel>.RenderWall(wallFromX, wallToX, (uint)width, textureWidth, repeatedCountPtr,
                        wallTexturePtr, screenPtr,
                        fromYClamped, toYClamped, textureXLocation, textureYPosPtr, textureYIncrementPtr);
                }
            }
        }

        private bool DrawBasicWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall)
        {
            // separate path for skybox rendering
            RenderableWall wall = renderableWall.Wall;
            GameTextureInfo textureInfo = wall.MiddleTexture!;

            Debug.Assert(textureInfo != null);

            if (textureInfo.RenderingOptions.IsSkybox)
            {
                CalculateDistance(renderableWall);
                return DrawBasicSkyboxWall(player, renderableWall);
            }

            // Precalculate render window and texture positions
            PrecalculateBasicWallDistance(renderableWall);

            ref RenderColumnStatus statusRef = ref memoryPool.GetBucketRef<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            ushort length = (ushort)(wallToX - wallFromX + 1);
            Span<ushort> repeatedCount = memoryPool.GetBucket<ushort>(MemoryPoolBucket.Temp)[..length];
            repeatedCount.Fill(length);

            // Set the X position as "FinishedRendering" for this wall
            // and determines where there repeat count is 0 (nothing to draw)
            statusRef = ref Unsafe.Add(ref statusRef, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++)
            {
                if (!statusRef.WallRenderable)
                {
                    repeatedCount[x - wallFromX] = 0;
                }

                statusRef = RenderColumnStatus.FinishedRendering;
                statusRef = ref Unsafe.Add(ref statusRef, 1);
            }

            uint* wallStartClamped = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.WallStartClamped);
            uint* wallEndClamped = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.WallEndClamped);

            DrawWallShared(renderableWall, wall.MiddleTexture!, repeatedCount, wall.Shade, wallStartClamped, wallEndClamped);

            return true;
        }

        private void DrawTransparentWall(
            ReadOnlySpan<RenderableSector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            GameTextureInfo textureInfo = wall.MiddleTexture!;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ushort* repeatedCount = CalculateTransparentWall(sectors, renderableWall);

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount, wallToX - wallFromX + 1);

            RenderableSector sector = wall.Sector;

            DrawSpriteShared(sector, renderableWall.Wall, repeatedCount, wallFromX, wallToX, false, textureInfo);
        }

        private bool DrawPortalWall(
            PortalPlayerSnapshot player,
            ReadOnlySpan<RenderableSector> sectors,
            RenderablePortalWall renderableWall)
        {
            (bool renderLower, bool renderUpper, bool basicWall) = CalculateCanRenderPortalWall(sectors, renderableWall.Wall);

            CalculateWallClamp(renderableWall);
            CalculatePortalClamp(renderableWall);

            // ceiling and floor of the sector are the same
            // so no wall is drawn
            if (!renderLower && !renderUpper)
            {
                CalculateDistance(renderableWall);
                return true;
            }

            RenderableWall wall = renderableWall.Wall;

            if (renderLower)
            {
                GameTextureInfo lowerTexture = wall.LowerTexture!;

                if (lowerTexture.RenderingOptions.IsSkybox)
                {
                    CalculateDistance(renderableWall);
                    DrawLowerSkyboxPortalWall(player, renderableWall);
                }
                else
                {
                    CalculateLowerTextureYIncrement(renderableWall, lowerTexture);
                    CalculateTextureDistanceAndXPosition(renderableWall, lowerTexture);
                    DrawLowerPortalWall(renderableWall);
                }
            }

            if (renderUpper)
            {
                GameTextureInfo upperTexture = wall.UpperTexture!;

                if (upperTexture.RenderingOptions.IsSkybox)
                {
                    if (!renderLower) { CalculateDistance(renderableWall); }
                    DrawUpperSkyboxPortalWall(player, renderableWall);
                }
                else
                {
                    CalculateUpperTextureYIncrement(renderableWall, upperTexture);
                    CalculateTextureDistanceAndXPosition(renderableWall, upperTexture);
                    DrawUpperPortalWall(renderableWall);
                }
            }


            // if sector height matches top or bottom offset only top or bottom texture was drawn
            // no middle texture is possible, thus we can treat this as basic wall
            return basicWall;
        }

        private void DrawUpperSkyboxPortalWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall)
        {
            int* wallStartClampedPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);
            int* wallEndClampedPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            RenderableWall wall = renderableWall.Wall;

            Debug.Assert(wall.UpperTexture is not null);
            DrawBasicSkyboxWall(player, renderableWall, wallStartClampedPtr, wallEndClampedPtr, wall.UpperTexture);
        }

        private void DrawLowerSkyboxPortalWall(
                PortalPlayerSnapshot player,
                RenderablePortalWall renderableWall)
        {
            int* wallStartClampedPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);
            int* wallEndClampedPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallEndClamped);
            RenderableWall wall = renderableWall.Wall;

            Debug.Assert(wall.LowerTexture is not null);
            DrawBasicSkyboxWall(player, renderableWall, wallStartClampedPtr, wallEndClampedPtr, wall.LowerTexture);
        }

        private void DrawUpperPortalWall(
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            GameTextureInfo upperTexture = wall.UpperTexture!;

            Debug.Assert(upperTexture != null);

            uint* wallStartClamped = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.WallStartClamped);
            uint* wallEndClamped = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalFromClamped);

            Span<ushort> repeatedCount = DetermineMaxHorizontalRenderingDistance(renderableWall, wallStartClamped, wallEndClamped);

            DrawWallShared(renderableWall, upperTexture, repeatedCount, wall.Shade, wallStartClamped, wallEndClamped);
        }

        private void DrawLowerPortalWall(
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            GameTextureInfo lowerTexture = wall.LowerTexture!;

            Debug.Assert(lowerTexture != null);

            uint* wallStartClamped = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalToClamped);
            uint* wallEndClamped = memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.WallEndClamped);

            Span<ushort> repeatedCount = DetermineMaxHorizontalRenderingDistance(renderableWall, wallStartClamped, wallEndClamped);

            DrawWallShared(renderableWall, lowerTexture, repeatedCount, wall.LowerShade, wallStartClamped, wallEndClamped);
        }
    }
}