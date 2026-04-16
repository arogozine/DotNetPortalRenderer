namespace RenderingEngine.Tooling;

internal static unsafe class AlignedMemoryPoolExtensions
{
    extension (DynamicAlignedMemoryPool pool)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> GetBucket<T>(SpriteCachePoolBucket bucket)
            where T : unmanaged
        {
            return pool.GetBucket<T>((int)bucket);
        }
    }

    extension (AlignedMemoryPool pool)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> GetBucket<T>(MemoryPoolBucket bucket)
            where T : unmanaged
        {
            return pool.GetBucket<T>((int)bucket);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearBuckets(params ReadOnlySpan<MemoryPoolBucket> buckets)
        {
            ReadOnlySpan<int> bucketsInt = MemoryMarshal.Cast<MemoryPoolBucket, int>(buckets);
            pool.ClearBuckets(bucketsInt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetBucketRef<T>(MemoryPoolBucket bucket)
            where T : unmanaged
        {
            return ref pool.GetBucketRef<T>((int)bucket);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetBucketPtr(MemoryPoolBucket bucket)
        {
            return pool.GetBucketPtr((int)bucket);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* GetBucketPtr<T>(MemoryPoolBucket bucket)
            where T : unmanaged
        {
            return (T*)pool.GetBucketPtr((int)bucket);
        }
    }
}
