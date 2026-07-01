using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal unsafe partial class PortalRenderer
    {
        private void RenderSkyboxVector(PortalPlayerSnapshot player, RenderableSector sector)
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
                RenderSkyboxShared(player, RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderCeiling,
                    screenPtr, texturePtr, sectorFromX, sectorToX,
                    ceilingStartPtr, wallStartPtr,
                    ceilingStartPtr, floorEndPtr,
                    PixelWidth,
                    texture.Width, texture.Height);
            }
        }

        protected abstract void RenderSkyboxShared(PortalPlayerSnapshot player,
            RenderColumnStatus renderColumnStatus,
            uint* screenPtr,
            uint* texturePtr,
            int sectorFromX, int sectorToX,
            int* fromYPtr, int* toYPtr,
            int* ceilingStartPtr, int* floorEndPtr,
            int width,
            int textureWidth,
            int textureHeight);

        private void RenderSkyboxFloorVector(
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

        private bool DrawBasicSkyboxWall(
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

        private void DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall,
            int* wallStartPtr, int* wallEndPtr,
            GameTextureInfo wallTexture)
        {
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint wallTextureUintPtr = ref wallTexture.Texture.GetBinaryRef<uint>(wallTexture.Palette, renderableWall.Wall.Shade ?? byte.MaxValue, TextureTransform.Normal);

            uint* screenPtr = (uint*)buffer;

            int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            fixed (uint* texturePtr = &wallTextureUintPtr)
            {
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
