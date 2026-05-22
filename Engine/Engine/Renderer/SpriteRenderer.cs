using RenderingEngine.Models;
using RenderingEngine.Models.Rendering;
using RenderingEngine.Tooling;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal unsafe interface IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Draw(uint* surface, uint pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DrawLine(uint* surface, Vector256<uint> pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DrawLine(uint* surface, Vector128<uint> pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DrawLine(uint* surface, Vector<uint> pixels);
    }

    internal readonly ref struct DrawSimplePixel : IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void Draw(uint* surface, uint pixel)
        {
            *surface = pixel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector256<uint> pixels)
        {
            pixels.Store(surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector128<uint> pixels)
        {
            pixels.Store(surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector<uint> pixels)
        {
            pixels.Store(surface);
        }
    }

    internal readonly ref struct DrawTransparentPixel : IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void Draw(uint* surface, uint pixel)
        {
            if (pixel != 0U)
            {
                *surface = pixel;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector256<uint> pixels)
        {
            if (Avx2.IsSupported)
            {
                Vector256<uint> gtMask = Vector256.GreaterThan(pixels, Vector256<uint>.Zero);
                Avx2.MaskStore(surface, gtMask, pixels);
                return;
            }

            for (int i = 0; i < Vector256<uint>.Count; i++)
            {
                uint pixel = pixels[i];

                if (pixel != 0U)
                {
                    *surface = pixel;
                }

                surface++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector128<uint> pixels)
        {
            if (Avx2.IsSupported)
            {
                Vector128<uint> gtMask = Vector128.GreaterThan(pixels, Vector128<uint>.Zero);
                Avx2.MaskStore(surface, gtMask, pixels);
                return;
            }

            for (int i = 0; i < Vector128<uint>.Count; i++)
            {
                uint pixel = pixels[i];

                if (pixel != 0U)
                {
                    *surface = pixel;
                }

                surface++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector<uint> pixels)
        {
            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                uint pixel = pixels[i];

                if (pixel != 0U)
                {
                    *surface = pixel;
                }

                surface++;
            }
        }
    }

    internal readonly ref struct DrawAlphaPixel : IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void Draw(uint* surface, uint pixel)
        {
            if (pixel != 0U)
            {
                *surface = BlendBGRA(*surface, pixel);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector256<uint> pixels)
        {
            pixels = BlendBGRA(Vector256.Load(surface), pixels);
            Vector256.Store(pixels, surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector128<uint> pixels)
        {
            pixels = BlendBGRA(Vector128.Load(surface), pixels);
            Vector128.Store(pixels, surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector<uint> pixels)
        {
            pixels = BlendBGRA(Vector.Load(surface), pixels);
            Vector.Store(pixels, surface);
        }

        static uint BlendBGRA(uint bgraDstU, uint bgraSrcU)
        {
            const uint a = 127;
            const uint ByteMask = 0xFF;
            const uint Alpha = (uint)byte.MaxValue << 24;

            uint bDst = bgraDstU & ByteMask;
            uint gDst = (bgraDstU >> 8) & ByteMask;
            uint rDst = (bgraDstU >> 16) & ByteMask;

            uint bSrc = bgraSrcU & ByteMask;
            uint gSrc = (bgraSrcU >> 8) & ByteMask;
            uint rSrc = (bgraSrcU >> 16) & ByteMask;

            uint bOut = (bSrc * a + bDst * a) >> 8;
            uint gOut = (gSrc * a + gDst * a) >> 8;
            uint rOut = (rSrc * a + rDst * a) >> 8;

            return (Alpha | (rOut << 16) | (gOut << 8) | bOut);
        }

        static Vector128<uint> BlendBGRA(Vector128<uint> bgraDst, Vector128<uint> bgraSrc)
        {
            Vector128<uint> gMask = Vector128.GreaterThan(bgraSrc, Vector128<uint>.Zero);
            bgraSrc = Vector128.ConditionalSelect(gMask, bgraSrc, bgraDst);

            Vector128<uint> a = Vector128.Create((uint)127);
            Vector128<uint> byteMask = Vector128.Create((uint)0xFF);
            Vector128<uint> alpha = Vector128.Create((uint)byte.MaxValue << 24);

            Vector128<uint> bDst = bgraDst & byteMask;
            Vector128<uint> bSrc = bgraSrc & byteMask;

            Vector128<uint> gDst = (bgraDst >> 8) & byteMask;
            Vector128<uint> gSrc = (bgraSrc >> 8) & byteMask;

            Vector128<uint> rDst = (bgraDst >> 16) & byteMask;
            Vector128<uint> rSrc = (bgraSrc >> 16) & byteMask;

            Vector128<uint> bOut = ((bSrc * a) + (bDst * a)) >> 8;
            Vector128<uint> gOut = ((gSrc * a) + (gDst * a)) >> 8;
            Vector128<uint> rOut = ((rSrc * a) + (rDst * a)) >> 8;

            return alpha | (rOut << 16) | (gOut << 8) | bOut;
        }

        static Vector256<uint> BlendBGRA(Vector256<uint> bgraDst, Vector256<uint> bgraSrc)
        {
            Vector256<uint> gMask = Vector256.GreaterThan(bgraSrc, Vector256<uint>.Zero);
            bgraSrc = Vector256.ConditionalSelect(gMask, bgraSrc, bgraDst);

            Vector256<uint> a = Vector256.Create((uint)127);
            Vector256<uint> byteMask = Vector256.Create((uint)0xFF);
            Vector256<uint> alpha = Vector256.Create((uint)byte.MaxValue << 24);

            Vector256<uint> bDst = bgraDst & byteMask;
            Vector256<uint> bSrc = bgraSrc & byteMask;

            Vector256<uint> gDst = (bgraDst >> 8) & byteMask;
            Vector256<uint> gSrc = (bgraSrc >> 8) & byteMask;

            Vector256<uint> rDst = (bgraDst >> 16) & byteMask;
            Vector256<uint> rSrc = (bgraSrc >> 16) & byteMask;

            Vector256<uint> bOut = ((bSrc * a) + (bDst * a)) >> 8;
            Vector256<uint> gOut = ((gSrc * a) + (gDst * a)) >> 8;
            Vector256<uint> rOut = ((rSrc * a) + (rDst * a)) >> 8;

            return alpha | (rOut << 16) | (gOut << 8) | bOut;
        }

        static Vector<uint> BlendBGRA(Vector<uint> bgraDst, Vector<uint> bgraSrc)
        {
            Vector<uint> gMask = Vector.GreaterThan(bgraSrc, Vector<uint>.Zero);
            bgraSrc = Vector.ConditionalSelect(gMask, bgraSrc, bgraDst);

            Vector<uint> a = Vector.Create((uint)127);
            Vector<uint> byteMask = Vector.Create((uint)0xFF);
            Vector<uint> alpha = Vector.Create((uint)byte.MaxValue << 24);

            Vector<uint> bDst = bgraDst & byteMask;
            Vector<uint> bSrc = bgraSrc & byteMask;

            Vector<uint> gDst = (bgraDst >> 8) & byteMask;
            Vector<uint> gSrc = (bgraSrc >> 8) & byteMask;

            Vector<uint> rDst = (bgraDst >> 16) & byteMask;
            Vector<uint> rSrc = (bgraSrc >> 16) & byteMask;

            Vector<uint> bOut = ((bSrc * a) + (bDst * a)) >> 8;
            Vector<uint> gOut = ((gSrc * a) + (gDst * a)) >> 8;
            Vector<uint> rOut = ((rSrc * a) + (rDst * a)) >> 8;

            return alpha | (rOut << 16) | (gOut << 8) | bOut;
        }
    }

    internal sealed partial class PortalRenderer
    {
        private void DrawSprite(
            PortalPlayerSnapshot player,
            ReadOnlySpan<Sector> sectors,
            RenderableSprite sprite,
            RenderWindowSpriteSnapshot renderableWall)
        {
            switch (sprite)
            {
                case RenderableWallSprite renderableWallSprite:
                    DrawWallSprite(sectors, renderableWallSprite, renderableWall);
                    break;
                case RenderableFloorSprite renderableFloorSprite:
                    DrawFloorSprite(player, sectors, renderableFloorSprite, renderableWall);
                    break;
                case RenderableBasicSprite renderableSprite:
                    DrawSprite(sectors, renderableSprite, renderableWall);
                    break;
            }
        }

        private unsafe void DrawSprite(ReadOnlySpan<Sector> sectors, RenderableBasicSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            uint* textureXLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncramentPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);
            uint* portalFromClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalFromClamped);
            uint* portalToClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalToClamped);


            int bufferOffset = PixelWidth * renderableWall.Depth;

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<int> wallStartSpan = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEndSpan = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            TextureInfo texture = sprite.Texture;

            int width = PixelWidth;
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            Sector sector = sectors[sprite.SectorId];

            bool isPowerOfTwo = SharedHelpers.IsPowerOfTwo(textureHeight);

            float rx1 = sprite.R1.X;

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteStartY = sprite.YLeftCeil;
            int spriteEndY = sprite.YLeftFloor;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            float fromToYDist = sprite.DistanceMin;

            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * spriteFromX;

            int length = spriteToX - spriteFromX;
            int textureXIncr = (textureWidth << 16) / (spriteEndY - spriteStartY);

            float textureLen = texture.Width / sprite.Length;
            Span<ushort> repeatedCount = TempBuffer<ushort>.GetBuffer(length + 1);

            for (int x = spriteFromX; x <= spriteToX; x++, cameraRay += cameraWidthIncr)
            {
                int wallStart = wallStartSpan[x];
                int wallEnd = wallEndSpan[x];

                if (wallEnd <= wallStart || distance[x] < fromToYDist)
                {
                    repeatedCount[x - spriteFromX] = 0;
                    continue;
                }

                int clamptedFromY = Math.Clamp(spriteStartY, wallStart, wallEnd);
                int clamptedToY = Math.Clamp(spriteEndY, wallStart, wallEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    repeatedCount[x - spriteFromX] = 0;
                    continue;
                }

                int textureXLocation = CalculateTextureXPosition(cameraRay);
                if (textureXLocation >= textureHeight) {
                    textureXLocation = 0;
                }

                int textureYIncr = textureXIncr;
                int textureXPos = float.ConvertToIntegerNative<int>(textureXLocation);
                textureXPos = isPowerOfTwo ? (textureXPos & (textureHeight - 1)) : (textureXPos % textureHeight);
                textureXPos *= textureWidth;
                int textureYPos = (clamptedFromY - spriteStartY) * textureYIncr;

                textureYIncramentPtr[x] = float.ConvertToIntegerNative<uint>(textureYIncr);
                textureYLocationPtr[x] = float.ConvertToIntegerNative<uint>(textureYPos);
                textureXLocationPtr[x] = (uint)textureXPos;
                portalFromClampedPtr[x] = (uint)clamptedFromY;
                portalToClampedPtr[x] = (uint)clamptedToY;
                repeatedCount[x - spriteFromX] = (ushort)length;
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            DrawSpriteShared(sector, sprite, repeatedCount, spriteFromX, spriteToX, texture);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int CalculateTextureXPosition(float cameraRay)
            {
                float distX = MathF.FusedMultiplyAdd(fromToYDist, cameraRay, - rx1);

                return float.ConvertToIntegerNative<int>(MathF.Abs(distX) * textureLen);
            }
        }

        private unsafe void DrawWallSprite(ReadOnlySpan<Sector> sectors, RenderableWallSprite sprite, RenderWindowSpriteSnapshot renderableWall)
        {
            uint* textureXLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncramentPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);
            uint* portalFromClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalFromClamped);
            uint* portalToClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalToClamped);

            int bufferOffset = PixelWidth * renderableWall.Depth;

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            TextureInfo texture = sprite.Texture;

            ref uint screenRef = ref GetScreenPtr<uint>();
            uint* screenPtr = (uint*)buffer;

            int width = PixelWidth;
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;
            bool isPowerOfTwo = SharedHelpers.IsPowerOfTwo(textureHeight);
            Sector sector = sectors[sprite.SectorId];

            bool flipY = sprite.Texture.RenderingOptions.IsFlippedY;
            bool flipX = sprite.Texture.RenderingOptions.IsFlippedX;

            int xLeft = sprite.XLeft;
            int xRight = sprite.XRight;

            int spriteFromX = xLeft;
            int spriteToX = xRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(sprite, 0);
            float spriteStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float spriteEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            Debug.Assert(renderableWall.XLeft <= spriteFromX);

            int xOffset = 0;

            float xScale = texture.Width / sprite.Length;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(sprite, width, spriteFromX);

            int length = spriteToX - spriteFromX;
            Span<ushort> repeatedCount = TempBuffer<ushort>.GetBuffer(length + 1);

            for (int x = spriteFromX; x <= spriteToX; x++, cameraRay += cameraWidthIncr, spriteStartY += ceilDistIncr, spriteEndY += floorDistIncr)
            {
                int ceilingStart = wallStart[x];
                int floorEnd = wallEnd[x];

                if (floorEnd <= ceilingStart)
                {
                    repeatedCount[x - spriteFromX] = 0;
                    continue;
                }

                int spriteStartY_Int = float.ConvertToIntegerNative<int>(spriteStartY);
                int spriteEndY_Int = float.ConvertToIntegerNative<int>(spriteEndY);
                int clamptedFromY = Math.Clamp(spriteStartY_Int, ceilingStart, floorEnd);
                int clamptedToY = Math.Clamp(spriteEndY_Int, ceilingStart, floorEnd);

                if (clamptedFromY >= clamptedToY)
                {
                    repeatedCount[x - spriteFromX] = 0;
                    continue;
                }

                (float textureXLocation, float fromToYdist) = MathFormulas.CalculateDistance(sprite, cameraRay, t1, d2y, d2x);

                if ((int)distance[x] < (int)fromToYdist)
                {
                    repeatedCount[x - spriteFromX] = 0;
                    continue;
                }

                textureXLocation = MathF.FusedMultiplyAdd(textureXLocation, xScale, xOffset);
                int textureYIncr = (textureWidth << 16) / (spriteEndY_Int - spriteStartY_Int);
                int textureXPos = float.ConvertToIntegerNative<int>(textureXLocation);
                textureXPos = isPowerOfTwo ? (textureXPos & (textureHeight - 1)) : (textureXPos % textureHeight);
                textureXPos *= textureWidth;

                int textureYPos = (clamptedFromY - spriteStartY_Int) * textureYIncr;

                portalFromClampedPtr[x] = (uint)clamptedFromY;
                portalToClampedPtr[x] = (uint)clamptedToY;

                textureXLocationPtr[x] = (uint)textureXPos;
                textureYLocationPtr[x] = float.ConvertToIntegerNative<uint>(textureYPos);
                textureYIncramentPtr[x] = float.ConvertToIntegerNative<uint>(textureYIncr);
                repeatedCount[x - spriteFromX] = (ushort)length;
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            DrawSpriteShared(sector, sprite, repeatedCount, spriteFromX, spriteToX, texture);
        }

        private void DrawTransparentWall(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo textureInfo = wall.MiddleTexture!;

            Span<ushort> repeatedCount;
            if (textureInfo.XScale is not null)
            {
                repeatedCount = CalculateTransparentWallBuild(sectors, renderableWall);
            }
            else
            {
                repeatedCount = CalculateTransparentWallDoom(sectors, renderableWall);
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Sector sector = wall.Sector;

            DrawSpriteShared(sector, renderableWall.Wall, repeatedCount, wallFromX, wallToX, textureInfo);
        }

        private unsafe Span<ushort> CalculateTransparentWallDoom(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            int width = PixelWidth;

            int bufferOffset = PixelWidth * renderableWall.Depth;

            Span<float> distanceSpan = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<RenderColumnStatus> columnStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[bufferOffset..];

            if (renderableWall.Depth > 1)
            {
                bufferOffset = PixelWidth * (renderableWall.Depth - 1);
            }

            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            RenderableWall wall = renderableWall.Wall;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);


            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);
            float oneOverSectorHeight = 1f / sectorHeight;

            // Texture Calculations
            TextureInfo? textureInfo = wall.MiddleTexture;
            Debug.Assert(textureInfo != null);
            Texture texture = TextureCache.GetTexture(textureInfo);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;
            bool texHeightDivisible2 = SharedHelpers.IsPowerOfTwo(textureHeight);
            if (texHeightDivisible2)
            {
                textureHeight--;
            }

            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset > sectorHeight ? textureInfo.YOffset - 65536 : textureInfo.YOffset;

            Sector sector = wall.Sector;
            (float xScale, float yScale) = (textureInfo.XScale!.Value, textureInfo.YScale!.Value);
            yScale = (sector.Ceil - sector.Floor) * yScale;
            xScale = xScale / wall.Length * texture.Width;

            int* textureXLocationPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncramentPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);

            int* portalFromClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            int* portalToClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);


            int length = wallToX - wallFromX;
            Span<ushort> buffer = TempBuffer<ushort>.GetBuffer(length + 1);

            bool renderFromTop = textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromTop);


            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus status = columnStatus[x];

                if (status.PortalRenderable)
                {
                    buffer[x - wallFromX] = 0; 
                    continue;
                }

                float dist = distanceSpan[x];
                int floorEnd = wallEnd[x];
                int ceilingStart = wallStart[x];

                (float distance, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                if (fromToYdist > dist)
                {
                    buffer[x - wallFromX] = 0;
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                float pixelsPerUnit = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                float floorPixelOffset = pixelsPerUnit * floorOffset;
                float ceilPixelOffset = pixelsPerUnit * ceilOffset;
                float portalFromY = wallStartY - ceilPixelOffset;
                float portalToY = wallEndY - floorPixelOffset;

                float textureStartY = renderFromTop ? portalFromY : (portalToY - texture.Height * pixelsPerUnit);
                float textureEndY = renderFromTop ? (portalFromY + texture.Height * pixelsPerUnit) : portalToY;

                if (yOffset != 0)
                {
                    float yOffsetF = yOffset * pixelsPerUnit;

                    if (yOffset > 0)
                    {
                        textureStartY = renderFromTop ? textureStartY + yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY + yOffsetF : textureEndY - yOffsetF;
                    }
                    else
                    {
                        textureStartY = renderFromTop ? textureStartY - yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY - yOffsetF : textureEndY - yOffsetF;
                    }
                }

                int textureStartYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureStartY), ceilingStart, floorEnd);
                int textureEndYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureEndY), ceilingStart, floorEnd);

                if (textureStartYClamped >= textureEndYClamped)
                {
                    buffer[x - wallFromX] = 0;
                    continue;
                }

                float offset = textureStartYClamped - textureStartY;

                // Calculate Middle Texture Position
                float textureYIncr = (float)(sectorHeight / (wallEndY - wallStartY));
                int textureXPos = ((float.ConvertToIntegerNative<int>(distance) + xOffset) % textureHeight) * textureWidth;
                float textureYPos = MathF.FusedMultiplyAdd(textureYIncr, offset, textureWidth);


                textureYIncr *= (1 << 16);
                textureYPos *= (1 << 16);

                portalFromClampedPtr[x] = textureStartYClamped;
                portalToClampedPtr[x] = textureEndYClamped;

                textureXLocationPtr[x] = textureXPos;
                textureYLocationPtr[x] = float.ConvertToIntegerNative<uint>(textureYPos);
                textureYIncramentPtr[x] = float.ConvertToIntegerNative<uint>(textureYIncr);
                buffer[x - wallFromX] = (ushort)length;
            }

            return buffer;
        }

        private unsafe Span<ushort> CalculateTransparentWallBuild(
            ReadOnlySpan<Sector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            int width = PixelWidth;

            int offset = PixelWidth * renderableWall.Depth;

            Span<float> spriteDistance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[offset..];
            Span<RenderColumnStatus> columnStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[offset..];

            if (renderableWall.Depth > 1)
            {
                offset = PixelWidth * (renderableWall.Depth - 1);
            }

            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[offset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[offset..];

            RenderableWall wall = renderableWall.Wall;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);


            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);
            float oneOverSectorHeight = 1f / sectorHeight;

            // Texture Calculations
            TextureInfo? textureInfo = wall.MiddleTexture;
            Debug.Assert(textureInfo != null);
            Texture texture = TextureCache.GetTexture(textureInfo);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;
            bool texHeightDivisible2 = SharedHelpers.IsPowerOfTwo(textureHeight);
            if (texHeightDivisible2)
            {
                textureHeight--;
            }
            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;
            Sector sector = wall.Sector;
            (float xScale, float yScale) = (textureInfo.XScale!.Value, textureInfo.YScale!.Value);
            yScale = (sector.Ceil - sector.Floor) * yScale;
            xScale = xScale / wall.Length * texture.Width;

            int* textureXLocationPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncramentPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);

            int* portalFromClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            int* portalToClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);


            int length = wallToX - wallFromX;
            Span<ushort> buffer = TempBuffer<ushort>.GetBuffer(length + 1);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus columnStatusY = columnStatus[x];

                if (columnStatusY.PortalRenderable)
                {
                    buffer[x - wallFromX] = 0;
                    continue;
                }

                float distance = spriteDistance[x];
                int floorEndY = wallEnd[x];
                int ceilingStartY = wallStart[x];

                (float distanceY, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                if (fromToYdist > distance)
                {
                    buffer[x - wallFromX] = 0;
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                float pixelsPerUnit = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                float floorPixelOffset = pixelsPerUnit * floorOffset;
                float ceilPixelOffset = pixelsPerUnit * ceilOffset;
                float textureFromY = wallStartY - ceilPixelOffset;
                float textureToY = wallEndY - floorPixelOffset;

                // Clamp to View Window
                int clampedFromY = Math.Clamp(float.ConvertToIntegerNative<int>(textureFromY), ceilingStartY, floorEndY);
                int clampedToY = Math.Clamp(float.ConvertToIntegerNative<int>(textureToY), ceilingStartY, floorEndY);

                if (clampedFromY >= clampedToY)
                {
                    buffer[x] = 0;
                    continue;
                }

                // Calculate Middle Texture Position
                int textureXPos = float.ConvertToIntegerNative<int>(distanceY * xScale);
                textureXPos += xOffset;
                textureXPos = texHeightDivisible2 ? (textureXPos & textureHeight) : (textureXPos % textureHeight);
                textureXPos *= textureWidth;

                float textureYIncr = (textureWidth * yScale) / (wallEndY - wallStartY);
                float textureYPos = yOffset - textureYIncr * (wallStartY - clampedFromY);

                textureYIncr *= (1 << 16);
                textureYPos *= (1 << 16);

                portalFromClampedPtr[x] = clampedFromY;
                portalToClampedPtr[x] = clampedToY;

                textureXLocationPtr[x] = textureXPos;
                textureYLocationPtr[x] = float.ConvertToIntegerNative<uint>(textureYPos);
                textureYIncramentPtr[x] = float.ConvertToIntegerNative<uint>(textureYIncr);
                buffer[x - wallFromX] = (ushort)length;
            }

            return buffer;
        }

        private unsafe void DrawSpriteShared(
            Sector sector,
            IWallLike sprite,
            Span<ushort> repeatedCount,
            int spriteFromX, int spriteToX,
            TextureInfo texture
            )
        {
            uint* textureXLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncramentPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);
            uint* portalFromClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalFromClamped);
            uint* portalToClampedPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.PortalToClamped);

            int textureWidth = texture.Height;
            int textureHeight = texture.Width;

            uint* screenPtr = (uint*)buffer;
            uint width = (uint)PixelWidth;

            bool isPowerOfTwo = SharedHelpers.IsPowerOfTwo(textureHeight);
            float alpha = Math.Clamp(texture.Alpha, 0f, 1f);

            fixed (uint* texturePtr = &GetTextureRef())
            {
                if (alpha == 1f)
                {
                    Draw(new DrawTransparentPixel(), repeatedCount, texturePtr);
                }
                else
                {
                    Draw(new DrawAlphaPixel(), repeatedCount, texturePtr);
                }
            }

            void Draw<T>(T drawPixel, Span<ushort> repeatedCount, uint* texturePtr)
                where T : IDrawPixel, allows ref struct
            {
                for (int x = spriteFromX; x <= spriteToX;)
                {
                    ushort count = repeatedCount[x - spriteFromX];

                    if (count == 0)
                    {
                        x++;
                        continue;
                    }

                    uint* clampedFromY = portalFromClampedPtr + x;
                    uint* clampedToY = portalToClampedPtr + x;
                    uint* textureXPos = textureXLocationPtr + x;
                    uint* textureYPos = textureYLocationPtr + x;
                    uint* textureYIncr = textureYIncramentPtr + x;

                    Debug.Assert(*clampedFromY < *clampedToY);

                    if (Vector256.IsHardwareAccelerated && count >= Vector256<uint>.Count)
                    {
                        RenderMultipleWallLinesV256(
                            drawPixel,
                            isPowerOfTwo,
                            width,
                            (uint)x,
                            textureWidth,
                            clampedFromY,
                            clampedToY,
                            textureYPos,
                            textureYIncr,
                            screenPtr,
                            textureXPos,
                            texturePtr
                        );

                        x += Vector256<uint>.Count;
                        continue;
                    }

                    if (Vector128.IsHardwareAccelerated && count >= Vector128<uint>.Count)
                    {
                        RenderMultipleWallLinesV128(
                            drawPixel,
                            isPowerOfTwo,
                            width,
                            (uint)x,
                            textureWidth,
                            clampedFromY,
                            clampedToY,
                            textureYPos,
                            textureYIncr,
                            screenPtr,
                            textureXPos,
                            texturePtr
                        );

                        x += Vector128<uint>.Count;
                        continue;
                    }

                    RenderMultipleWallLines(
                        drawPixel,
                        isPowerOfTwo,
                        count,
                        width,
                        (uint)x,
                        textureWidth,
                        clampedFromY,
                        clampedToY,
                        textureYPos,
                        textureYIncr,
                        screenPtr,
                        textureXPos,
                        texturePtr
                    );

                    x += count;
                }

            }

            ref uint GetTextureRef()
            {
                bool flipY = texture.RenderingOptions.IsFlippedY;
                bool flipX = texture.RenderingOptions.IsFlippedX;

                var transform = TextureTransform.Rotated;

                if (flipY)
                {
                    transform |= TextureTransform.FlippedY;
                }

                if (flipX)
                {
                    transform |= TextureTransform.FlippedX;
                }

                if (sprite.Flipped)
                {
                    transform ^= TextureTransform.FlippedX;
                }

                return ref texture.Texture.GetBinaryRef<uint>(sprite.Shade ?? sector.FloorShade,
                    transform);
            }
        }

        #region Render Wall with Blend Mode

        private unsafe static void RenderMultipleWallLinesV256<T>(
            T drawPixel,
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            uint* startY,
            uint* endY,
            uint* textureYPos_u,
            uint* textureYIncr_u,
            uint* screenPtr,
            uint* texturePos,
            uint* textureBuffer
        )
            where T : IDrawPixel, allows ref struct
        {
            var startYV = Vector256.Load(startY);
            var endYV = Vector256.Load(endY);
            var textureXIncr_uV = Vector256.Load(textureYIncr_u);

            (uint min_t, uint max_t) = GetMinMaxValue(startYV);
            (uint min_b, uint max_b) = GetMinMaxValue(endYV);

            if (min_b <= max_t)
            {
                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    uint* textureYPos = textureYPos_u + i;
                    uint incr = textureXIncr_uV[i];
                    uint xi = x + (uint)i;

                    uint top = startYV[i];
                    uint bottom = endYV[i];

                    RenderWallColumn(drawPixel, isPowerOfTwo, width, xi, textureHeight, top, bottom, *textureYPos, incr, screenPtr,
                        textureBuffer + *(texturePos + i));
                }

                return;
            }

            // render tops where there is no shared window
            if (min_t != max_t)
            {
                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    uint top = startYV[i];

                    if (top < max_t)
                    {
                        uint* textureYPos = textureYPos_u + i;
                        uint incr = textureXIncr_uV[i];
                        uint xi = x + (uint)i;

                        *textureYPos = RenderWallColumn2(drawPixel, isPowerOfTwo, width, xi, textureHeight, top, max_t, *textureYPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i)
                        );
                    }
                }
            }
            Vector256<uint> textureYPos_uV = Vector256.Load(textureYPos_u);

            if (min_b > max_t)
            {
                // prepare for the shared vertical window
                Vector256<uint> textureXPosV = Vector256.Load(texturePos);
                uint* screenIndexPtr = screenPtr + (max_t * width + x);
                uint* screenIndexPtrEnd = screenPtr + (min_b * width + x);

                // cache base Ptr for texture buffer and precompute step
                uint widthMinusLanes = width - (uint)Vector256<uint>.Count;

                if (isPowerOfTwo)
                {
                    Vector256<uint> textureMaskV = Vector256.Create((uint)(textureHeight - 1));

                    if (Avx2.IsSupported)
                    {
                        while (screenIndexPtr < screenIndexPtrEnd)
                        {
                            Vector256<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                            texelIndexV += textureXPosV;

                            Vector256<uint> gathered = Avx2.GatherVector256(
                                textureBuffer,
                                texelIndexV.AsInt32(),
                                scale: sizeof(uint)
                            );

                            drawPixel.DrawLine(screenIndexPtr, gathered);

                            textureYPos_uV += textureXIncr_uV;
                            screenIndexPtr += width;
                        }
                    }
                    else
                    {
                        // go down the column set
                        while (screenIndexPtr < screenIndexPtrEnd)
                        {
                            Vector256<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                            texelIndexV += textureXPosV;

                            // horizontally draw the texture
                            for (int i = 0; i < Vector256<uint>.Count; i++)
                            {
                                uint pixel = *(textureBuffer + texelIndexV[i]);

                                drawPixel.Draw(screenIndexPtr, pixel);

                                screenIndexPtr++;
                            }

                            textureYPos_uV += textureXIncr_uV;
                            screenIndexPtr += widthMinusLanes;
                        }
                    }
                }
                else
                {
                    uint textureHeightMask = (uint)textureHeight;

                    // go down the column set
                    while (screenIndexPtr < screenIndexPtrEnd)
                    {
                        Vector256<uint> texelIndexV = textureYPos_uV >> 16;

                        // horizontally draw the texture (keeps per-lane behavior)
                        for (int i = 0; i < Vector256<uint>.Count; i++)
                        {
                            uint pixel = *(textureBuffer + textureXPosV[i] + (texelIndexV[i] % textureHeightMask));

                            drawPixel.Draw(screenIndexPtr, pixel);

                            screenIndexPtr++;
                        }

                        textureYPos_uV += textureXIncr_uV;
                        screenIndexPtr += widthMinusLanes;
                    }
                }
            }

            // render bottoms where there is no shared window
            if (min_b != max_b)
            {
                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    uint bottom = endYV[i];

                    if (bottom > min_b)
                    {
                        uint textureXPos = textureYPos_uV[i];
                        uint incr = textureXIncr_uV[i];

                        uint xi = x + (uint)i;

                        RenderWallColumn(drawPixel, isPowerOfTwo, width, xi, textureHeight, min_b, bottom, textureXPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i)
                        );
                    }
                }
            }
        }

        private static unsafe void RenderMultipleWallLinesV128<T>(
            T drawPixel,
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            uint* startY,
            uint* endY,
            uint* textureYPos_u,
            uint* textureYIncr_u,
            uint* screenPtr,
            uint* texturePos,
            uint* textureBuffer
            )
            where T : IDrawPixel, allows ref struct
        {
            var startYV = Vector128.Load(startY);
            var endYV = Vector128.Load(endY);
            var textureXIncr_uV = Vector128.Load(textureYIncr_u);

            (uint min_t, uint max_t) = GetMinMaxValue(startYV);
            (uint min_b, uint max_b) = GetMinMaxValue(endYV);

            if (min_b <= max_t)
            {
                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    uint* textureYPos = textureYPos_u + i;
                    uint incr = textureXIncr_uV[i];
                    uint xi = x + (uint)i;

                    uint top = startYV[i];
                    uint bottom = endYV[i];

                    RenderWallColumn(drawPixel, isPowerOfTwo, width, xi, textureHeight, top, bottom, *textureYPos, incr, screenPtr,
                        textureBuffer + *(texturePos + i));
                }

                return;
            }

            // render tops where there is no shared window
            if (min_t != max_t)
            {
                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    uint top = startYV[i];

                    if (top < max_t)
                    {
                        uint* textureYPos = textureYPos_u + i;
                        uint incr = textureXIncr_uV[i];
                        uint xi = x + (uint)i;

                        *textureYPos = RenderWallColumn2(drawPixel, isPowerOfTwo, width, xi, textureHeight, top, max_t, *textureYPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i));
                    }
                }
            }

            Vector128<uint> textureYPos_uV = Vector128.Load(textureYPos_u);

            // shared window
            if (min_b > max_t)
            {
                // prepare for the shared vertical window
                Vector128<uint> textureXPosV = Vector128.Load(texturePos);
                uint* screenIndexPtr = screenPtr + (max_t * width + x);
                uint* screenIndexPtrEnd = screenPtr + (min_b * width + x);

                // cache base Ptr for texture buffer and precompute step
                uint widthMinusLanes = width - (uint)Vector128<uint>.Count;

                if (isPowerOfTwo)
                {
                    Vector128<uint> textureMaskV = Vector128.Create((uint)(textureHeight - 1));

                    // go down the column set
                    while (screenIndexPtr < screenIndexPtrEnd)
                    {
                        Vector128<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                        texelIndexV += textureXPosV;

                        if (Avx2.IsSupported)
                        {
                            Vector128<uint> gathered = Avx2.GatherVector128(
                                textureBuffer,
                                texelIndexV.AsInt32(),
                                scale: sizeof(uint)
                            );

                            drawPixel.DrawLine(screenIndexPtr, gathered);
                            screenIndexPtr += Vector128<uint>.Count;
                        }
                        else
                        {
                            // horizontally draw the texture (keeps per-lane behavior but with cached Ptrs)
                            for (int i = 0; i < Vector128<uint>.Count; i++)
                            {
                                uint pixel = *(textureBuffer + texelIndexV[i]);

                                drawPixel.Draw(screenIndexPtr, pixel);

                                screenIndexPtr++;
                            }

                        }

                        textureYPos_uV += textureXIncr_uV;
                        screenIndexPtr += widthMinusLanes;
                    }
                }
                else
                {
                    uint textureMask = (uint)textureHeight;

                    // go down the column set
                    while (screenIndexPtr < screenIndexPtrEnd)
                    {
                        Vector128<uint> texelIndexV = textureYPos_uV >> 16;

                        // horizontally draw the texture (keeps per-lane behavior but with cached Ptrs)
                        for (int i = 0; i < Vector128<uint>.Count; i++)
                        {
                            uint pixel = *(textureBuffer + textureXPosV[i] + (texelIndexV[i] % textureMask));

                            drawPixel.Draw(screenIndexPtr, pixel);

                            screenIndexPtr++;
                        }

                        textureYPos_uV += textureXIncr_uV;
                        screenIndexPtr += widthMinusLanes;
                    }
                }
            }

            // render bottoms where there is no shared window
            if (min_b != max_b)
            {
                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    uint bottom = endYV[i];

                    if (bottom > min_b)
                    {
                        uint textureXPos = textureYPos_uV[i];
                        uint incr = textureXIncr_uV[i];
                        uint xi = x + (uint)i;

                        RenderWallColumn(drawPixel, isPowerOfTwo, width, xi, textureHeight, min_b, bottom, textureXPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i));
                    }
                }
            }
        }

        private static unsafe void RenderMultipleWallLines<T>(
            T drawPixel,
            bool isPowerOfTwo,
            uint count,
            uint width,
            uint x,
            int textureHeight,
            uint* startY,
            uint* endY,
            uint* textureYPos_u,
            uint* textureYIncr_u,
            uint* screenPtr,
            uint* texturePos,
            uint* textureBuffer
            )
            where T : IDrawPixel, allows ref struct
        {
            // each line can start and end at different y positions
            // so we determine the window where all lines can be rendered at once
            uint min_t = int.MaxValue, max_t = 0;
            uint min_b = int.MaxValue, max_b = 0;

            for (int i = 0; i < count; i++)
            {
                uint top = *(startY + i);
                min_t = Math.Min(min_t, top);
                max_t = Math.Max(max_t, top);

                uint bottom = *(endY + i);
                min_b = Math.Min(min_b, bottom);
                max_b = Math.Max(max_b, bottom);
            }

            if (min_b <= max_t)
            {
                for (uint i = 0; i < count; i++)
                {
                    uint textureYPos = *(textureYPos_u + i);
                    uint incr = *(textureYIncr_u + i);
                    uint xi = x + i;
                    uint top = *(startY + i);
                    uint bottom = *(endY + i);

                    RenderWallColumn(drawPixel, isPowerOfTwo, width, xi, textureHeight, top, bottom, textureYPos, incr, screenPtr,
                        textureBuffer + *(texturePos + i));
                }

                return;
            }

            // render tops of each line where there is no shared window
            if (min_t < max_t)
            {
                for (uint i = 0; i < count; i++)
                {
                    uint top = *(startY + i);

                    if (top < max_t)
                    {
                        uint* textureYPos = textureYPos_u + i;
                        uint incr = *(textureYIncr_u + i);

                        uint xi = x + i;

                        *textureYPos = RenderWallColumn2(drawPixel, isPowerOfTwo, width, xi, textureHeight, top, max_t, *textureYPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i));
                    }
                }
            }

            // shared window
            if (min_b > max_t)
            {
                uint* screenIndexPtr = screenPtr + max_t * width + x;
                uint* screenIndexPtrEnd = screenPtr + min_b * width + x;

                if (isPowerOfTwo)
                {
                    uint textureMask = (uint)(textureHeight - 1);

                    // go down the column set
                    while (screenIndexPtr < screenIndexPtrEnd)
                    {
                        // horizontally draw the texture
                        for (int i = 0; i < count; i++)
                        {
                            uint* textureXPos = textureYPos_u + i;
                            uint texelIndex = (*textureXPos >> 16) & textureMask;
                            texelIndex += *(texturePos + i);

                            uint pixel = *(textureBuffer + texelIndex);

                            drawPixel.Draw(screenIndexPtr, pixel);

                            *textureXPos += *(textureYIncr_u + i);
                            screenIndexPtr++;
                        }

                        screenIndexPtr += width - count;
                    }
                }
                else
                {
                    uint textureMask = (uint)textureHeight;

                    // go down the column set
                    while (screenIndexPtr < screenIndexPtrEnd)
                    {
                        // horizontally draw the texture
                        for (int i = 0; i < count; i++)
                        {
                            uint* textureYPos = textureYPos_u + i;
                            uint texelIndex = (*textureYPos >> 16) % textureMask;
                            texelIndex += *(texturePos + i);

                            uint pixel = *(textureBuffer + texelIndex);

                            drawPixel.Draw(screenIndexPtr, pixel);

                            *textureYPos += *(textureYIncr_u + i);
                            screenIndexPtr++;
                        }

                        screenIndexPtr += width - count;
                    }
                }
            }

            // render bottoms of each line where there is no shared window
            if (min_b < max_b)
            {
                for (uint i = 0; i < count; i++)
                {
                    uint bottom = *(endY + i);

                    if (bottom > min_b)
                    {
                        uint textureXPos = *(textureYPos_u + i);
                        uint incr = *(textureYIncr_u + i);
                        uint xi = x + i;

                        RenderWallColumn(drawPixel, isPowerOfTwo, width, xi, textureHeight, min_b, bottom, textureXPos, incr, screenPtr,
                            textureBuffer + *(texturePos + i));
                    }
                }
            }
        }

        private static unsafe uint RenderWallColumn2<T>(
            T drawPixel,
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            uint startY,
            uint endY,
            uint textureYPos_u,
            uint textureYIncr_u,
            uint* screenPtr,
            uint* textureBuffer
            )
            where T : IDrawPixel, allows ref struct
        {
            Debug.Assert(endY >= startY);
            uint* screenIndexPtr = screenPtr + startY * width + x;
            uint* screenIndexPtrEnd = screenPtr + endY * width + x;

            if (isPowerOfTwo)
            {
                uint textureHeightMask = (uint)(textureHeight - 1);

                while (screenIndexPtr != screenIndexPtrEnd)
                {
                    uint texelIndex = (textureYPos_u >> 16) & textureHeightMask;
                    uint pixel = *(textureBuffer + texelIndex);

                    drawPixel.Draw(screenIndexPtr, pixel);

                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }
            else
            {
                uint textureHeightMask = (uint)textureHeight;

                while (screenIndexPtr != screenIndexPtrEnd)
                {
                    uint texelIndex = (textureYPos_u >> 16) % textureHeightMask;
                    uint pixel = *(textureBuffer + texelIndex);

                    drawPixel.Draw(screenIndexPtr, pixel);

                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }

            return textureYPos_u;
        }

        private static unsafe void RenderWallColumn<T>(
            T drawPixel,
            bool isPowerOfTwo,
            uint width,
            uint x,
            int textureHeight,
            uint startY,
            uint endY,
            uint textureYPos_u,
            uint textureYIncr_u,
            uint* screenPtr,
            uint* textureBuffer
            )
            where T : IDrawPixel, allows ref struct
        {
            Debug.Assert(endY >= startY);
            uint* screenIndexPtr = screenPtr + startY * width + x;
            uint* screenIndexPtrEnd = screenPtr + endY * width + x;

            if (isPowerOfTwo)
            {
                uint textureHeightMask = (uint)(textureHeight - 1);

                while (screenIndexPtr < screenIndexPtrEnd)
                {
                    uint texelIndex = (textureYPos_u >> 16) & textureHeightMask;
                    uint pixel = *(textureBuffer + texelIndex);

                    drawPixel.Draw(screenIndexPtr, pixel);

                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }
            else
            {
                uint textureHeightMask = (uint)textureHeight;

                while (screenIndexPtr < screenIndexPtrEnd)
                {
                    uint texelIndex = (textureYPos_u >> 16) % textureHeightMask;
                    uint pixel = *(textureBuffer + texelIndex);

                    drawPixel.Draw(screenIndexPtr, pixel);

                    screenIndexPtr += width;
                    textureYPos_u += textureYIncr_u;
                }
            }
        }

        #endregion
    }
}
