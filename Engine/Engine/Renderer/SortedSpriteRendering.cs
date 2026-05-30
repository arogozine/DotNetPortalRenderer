using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private readonly DynamicAlignedMemoryPool spriteCacheMemoryPool;

        private void FillDepthZero()
        {
            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[..PixelWidth];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[..PixelWidth];
            Span<RenderColumnStatus> renderStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[..PixelWidth];

            wallStart.Clear();
            wallEnd.Fill(PixelHeight - 1);
            renderStatus.Fill(RenderColumnStatus.NewRender);
        }

        private void CacheDepthAndStartEndBoundsForSpriteRendering(int depth)
        {
            int offset = depth * PixelWidth;

            ResizeCacheIfNeeded();

            Span<float> distance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[offset..];
            memoryPool.GetBucket<float>(MemoryPoolBucket.Distance).CopyTo(distance);

            Span<RenderColumnStatus> renderStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[offset..];
            memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus).CopyTo(renderStatus);

            offset += PixelWidth;

            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[offset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[offset..];

            memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped).CopyTo(wallStart);
            memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped).CopyTo(wallEnd);

            Clamp(wallStart, wallEnd);

            return;

            void ResizeCacheIfNeeded()
            {
                int minSize = offset + PixelWidth << 1;

                if (spriteCacheMemoryPool.GetBucketSize<float>() <= minSize)
                {
                    spriteCacheMemoryPool.ReAlloc(minSize);
                }
            }

            unsafe void Clamp(Span<int> wallStart, Span<int> wallEnd)
            {
                int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
                int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

                int start = 0;
                int length = PixelWidth;

                if (Vector.IsHardwareAccelerated && length > 128)
                {
                    int rem = length % Vector<int>.Count;
                    length -= rem;

                    Vector<int> zero = Vector<int>.Zero;
                    Vector<int> max = Vector.Create(PixelHeight - 1);
                    Vector<int> ceilingStartV, floorEndV;
                    Vector<int> wallStartV, wallEndV;

                    for (int i = 0; i < length; i += Vector<int>.Count)
                    {
                        ceilingStartV = Vector.LoadAligned(&ceilingStartPtr[i]);
                        floorEndV = Vector.LoadAligned(&floorEndPtr[i]);

                        ceilingStartV = Vector.ClampNative(ceilingStartV, zero, max);
                        floorEndV = Vector.ClampNative(floorEndV, zero, max);

                        wallStartV = Vector.LoadUnsafe(ref wallStart[i]);
                        wallStartV = Vector.ClampNative(wallStartV, ceilingStartV, floorEndV);
                        Vector.StoreUnsafe(wallStartV, ref wallStart[i]);

                        wallEndV = Vector.LoadUnsafe(ref wallEnd[i]);
                        wallEndV = Vector.ClampNative(wallEndV, ceilingStartV, floorEndV);
                        Vector.StoreUnsafe(wallEndV, ref wallEnd[i]);
                    }

                    start = length;
                    length += rem;
                }

                for (int i = start; i < length; i++)
                {
                    int ceilingStart = ceilingStartPtr[i];
                    int floorEnd = floorEndPtr[i];

                    ceilingStart = Math.Clamp(ceilingStart, 0, PixelHeight - 1);
                    floorEnd = Math.Clamp(floorEnd, 0, PixelHeight - 1);

                    wallStart[i] = Math.Clamp(wallStart[i], ceilingStart, floorEnd);
                    wallEnd[i] = Math.Clamp(wallEnd[i], ceilingStart, floorEnd);
                }
            }
        }
    }
}
