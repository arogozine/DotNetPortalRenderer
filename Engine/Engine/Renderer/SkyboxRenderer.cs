using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private unsafe void RenderSkyboxVector(PortalPlayerSnapshot player, RenderableSector sector)
        {
            GameTextureInfo textureInfo = sector.CeilTexture;
            GameTexture texture = textureInfo.Texture;

            ref uint ceilingTexturePtr = ref texture.GetBinaryRef<uint>(textureInfo.Palette, sector.CeilingShade, TextureTransform.Normal);

            uint* screenPtr = (uint*)buffer;

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int* wallStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);
            int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            fixed (uint* texturePtr = &ceilingTexturePtr)
            {
                Sse.Prefetch2(texturePtr);

                RenderSkyboxShared(player, RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderCeiling,
                    screenPtr, texturePtr, sectorFromX, sectorToX,
                    ceilingStartPtr, wallStartPtr,
                    ceilingStartPtr, floorEndPtr,
                    PixelWidth,
                    texture.Width, texture.Height);
            }
        }

        private unsafe void RenderSkyboxShared(PortalPlayerSnapshot player,
            RenderColumnStatus renderColumnStatus,
            uint* screenPtr,
            uint* texturePtr,
            int sectorFromX, int sectorToX,
            int* fromYPtr, int* toYPtr,
            int* ceilingStartPtr, int* floorEndPtr,
            int width,
            int textureWidth,
            int textureHeight)
        {
            float yTextureIncr = ((float)textureHeight) / PixelHeight;

            float* angleCachePtr = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.AngleCache);

            Span<ushort> repeatedCount = CaclulateRepeatedCount();
            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            bool isPowerOfTwo = SharedHelpers.IsPowerOfTwo(textureHeight);

            fixed (ushort* repeatedCountPtr = &repeatedCount[0])
            {

                if (isPowerOfTwo)
                {
                    CoreRendererForPowTextures<DrawSimplePixel>.RenderSkybox(player, 1, repeatedCountPtr, angleCachePtr, screenPtr, texturePtr, sectorFromX, sectorToX, fromYPtr, toYPtr, ceilingStartPtr, floorEndPtr, width, textureWidth, textureHeight, yTextureIncr);
                }
                else
                {
                    CoreRendererForOddTextures<DrawSimplePixel>.RenderSkybox(player, 1, repeatedCountPtr, angleCachePtr, screenPtr, texturePtr, sectorFromX, sectorToX, fromYPtr, toYPtr, ceilingStartPtr, floorEndPtr, width, textureWidth, textureHeight, yTextureIncr);
                }
            }

            return;

            Span<ushort> CaclulateRepeatedCount()
            {
                RenderColumnStatus* status = memoryPool.GetBucketPtr<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

                int length = sectorToX - sectorFromX;

                Span<ushort> repeatedCount = memoryPool.GetBucket<ushort>(MemoryPoolBucket.Temp2)[..(length + 1)];

                for (int x = sectorFromX; x <= sectorToX; x++)
                {
                    RenderColumnStatus columnStatus = status[x];

                    if (!columnStatus.HasFlag(renderColumnStatus))
                    {
                        repeatedCount[x - sectorFromX] = 0;
                        continue;
                    }

                    repeatedCount[x - sectorFromX] = (ushort)length;
                }

                return repeatedCount;
            }
        }

        private unsafe void RenderSkyboxFloorVector(
            PortalPlayerSnapshot player,
            RenderableSector sector)
        {
            GameTextureInfo textureInfo = sector.FloorTexture;
            GameTexture texture = textureInfo.Texture;

            ref uint ceilingTexturePtr = ref texture.GetBinaryRef<uint>(textureInfo.Palette, sector.CeilingShade, TextureTransform.Normal);

            uint* screenPtr = (uint*)buffer;

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int* wallEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallEndClamped);
            int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            fixed (uint* texturePtr = &ceilingTexturePtr)
            {
                Sse.Prefetch2(texturePtr);

                RenderSkyboxShared(player, RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderFloor,
                    screenPtr, texturePtr, sectorFromX, sectorToX,
                    wallEndPtr, floorEndPtr,
                    ceilingStartPtr, floorEndPtr,
                    PixelWidth,
                    texture.Width, texture.Height);
            }
        }

        private unsafe bool DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall)
        {
            int* wallStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);
            int* wallEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallEndClamped);

            RenderableWall wall = renderableWall.Wall;
            Debug.Assert(wall.MiddleTexture != null);
            DrawBasicSkyboxWall(player, renderableWall, wallStartPtr, wallEndPtr, wall.MiddleTexture);

            // Set render status to finished
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            status[wallFromX..wallToX].Fill(RenderColumnStatus.FinishedRendering);

            return true;
        }

        private unsafe void DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall,
            int* wallStartPtr, int* wallEndPtr,
            GameTextureInfo wallTexture)
        {
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            GameTextureInfo textureInfo = wallTexture;
            ref uint wallTextureUintPtr = ref wallTexture.Texture.GetBinaryRef<uint>(wallTexture.Palette, renderableWall.Wall.Shade ?? byte.MaxValue, TextureTransform.Normal);

            uint* screenPtr = (uint*)buffer;

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            fixed (uint* texturePtr = &wallTextureUintPtr)
            {
                Sse.Prefetch2(texturePtr);

                RenderSkyboxShared(player, RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall,
                    screenPtr, texturePtr, wallFromX, wallToX,
                    wallStartPtr, wallEndPtr,
                    ceilingStartPtr, floorEndPtr,
                    PixelWidth,
                    wallTexture.Width, wallTexture.Height);
            }
        }
    }
}
