using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal unsafe partial class PortalRenderer
    {
        private void RenderSkyboxVector(
            PortalPlayerSnapshot player,
            RenderableSector sector,
            int sectorFromX, int sectorToX,
            bool temp2)
        {
            GameTextureInfo textureInfo = sector.CeilTexture;
            GameTexture texture = textureInfo.Texture;

            ref uint ceilingTexturePtr = ref texture.GetBinaryRef<uint>(textureInfo.Palette, sector.CeilingShade, TextureTransform.Normal);

            uint* screenPtr = (uint*)Buffer;
            
            int* wallStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);
            int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            fixed (uint* texturePtr = &ceilingTexturePtr)
            {
                RenderSkyboxShared(player, RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderCeiling,
                    screenPtr, texturePtr, sectorFromX, sectorToX,
                    ceilingStartPtr, wallStartPtr,
                    ceilingStartPtr, floorEndPtr,
                    PixelWidth,
                    texture.Width, texture.Height, temp2 ? MemoryPoolBucket.Temp2 : MemoryPoolBucket.Temp4);
            }
        }

        // AI Assisted: repeatedCountBucket lets floor rendering use Temp4 instead of Temp2 so it
        // can run concurrently with ceiling rendering without racing on the same buffer
        protected abstract void RenderSkyboxShared(PortalPlayerSnapshot player,
            RenderColumnStatus renderColumnStatus,
            uint* screenPtr,
            uint* texturePtr,
            int sectorFromX, int sectorToX,
            int* fromYPtr, int* toYPtr,
            int* ceilingStartPtr, int* floorEndPtr,
            int width,
            int textureWidth,
            int textureHeight,
            MemoryPoolBucket repeatedCountBucket);

        private void RenderSkyboxFloorVector(
            PortalPlayerSnapshot player,
            RenderableSector sector,
            int sectorFromX, int sectorToX,
            bool temp2)
        {
            GameTextureInfo textureInfo = sector.FloorTexture;
            GameTexture texture = textureInfo.Texture;

            ref uint ceilingTexturePtr = ref texture.GetBinaryRef<uint>(textureInfo.Palette, sector.CeilingShade, TextureTransform.Normal);

            uint* screenPtr = (uint*)Buffer;
            
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
                    texture.Width, texture.Height, temp2 ? MemoryPoolBucket.Temp2 : MemoryPoolBucket.Temp4);
            }
        }

        private bool DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall,
            bool usePrimaryTempBuckets)
        {
            int* wallStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);
            int* wallEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallEndClamped);

            RenderableWall wall = renderableWall.Wall;
            Debug.Assert(wall.MiddleTexture != null);
            DrawBasicSkyboxWall(player, renderableWall, wallStartPtr, wallEndPtr, wall.MiddleTexture, usePrimaryTempBuckets);

            // Set render status to finished
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            status[wallFromX..wallToX].Fill(RenderColumnStatus.FinishedRendering);

            return true;
        }

        private void DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall,
            int* wallStartPtr, int* wallEndPtr,
            GameTextureInfo wallTexture,
            bool usePrimaryTempBuckets)
        {
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint wallTextureUintPtr = ref wallTexture.Texture.GetBinaryRef<uint>(wallTexture.Palette, renderableWall.Wall.Shade ?? byte.MaxValue, TextureTransform.Normal);

            uint* screenPtr = (uint*)Buffer;

            int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            fixed (uint* texturePtr = &wallTextureUintPtr)
            {
                RenderSkyboxShared(player, RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall,
                    screenPtr, texturePtr, wallFromX, wallToX,
                    wallStartPtr, wallEndPtr,
                    ceilingStartPtr, floorEndPtr,
                    PixelWidth,
                    wallTexture.Width, wallTexture.Height, usePrimaryTempBuckets ? MemoryPoolBucket.Temp2 : MemoryPoolBucket.Temp4);
            }
        }
    }
}
