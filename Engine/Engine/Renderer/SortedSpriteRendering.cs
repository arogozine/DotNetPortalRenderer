using RenderingEngine.Tooling;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private readonly AlignedMemoryPool spriteCacheMemoryPool;

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
                if (spriteCacheMemoryPool.BucketSize <= (offset + PixelWidth))
                {
                    spriteCacheMemoryPool.ReAlloc(offset + PixelWidth << 1, (int)SpriteCachePoolBucket.RenderStatus + 1);
                }
            }

            void Clamp(Span<int> wallStart, Span<int> wallEnd)
            {
                Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
                Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

                int start = 0;
                int length = ceilingStart.Length;

                if (Vector.IsHardwareAccelerated && length > 128)
                {
                    int rem = length % Vector<int>.Count;
                    length -= rem;

                    Vector<int> ceilingStartV, floorEndV;
                    Vector<int> wallStartV, wallEndV;

                    for (int i = 0; i < length; i += Vector<int>.Count)
                    {
                        ceilingStartV = Vector.LoadUnsafe(ref ceilingStart[i]);
                        floorEndV = Vector.LoadUnsafe(ref floorEnd[i]);

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
                    wallStart[i] = Math.Clamp(wallStart[i], ceilingStart[i], floorEnd[i]);
                    wallEnd[i] = Math.Clamp(wallEnd[i], ceilingStart[i], floorEnd[i]);
                }
            }
        }
    }
}
