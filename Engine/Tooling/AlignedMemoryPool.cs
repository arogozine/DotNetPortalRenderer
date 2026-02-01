using System.Numerics;

namespace RenderingEngine.Tooling
{
    internal enum MemoryPoolBucket
    {
        AngleCache,
        CameraHeightToMapYPos,
        XMapPosMultiplierCache,
        RenderColumnStatus,
        CeilingStart,
        WallStart,
        WallEnd,
        FloorEnd,
        Distance,
        TopTextureXLocation,
        BottomTextureXLocation,
        TopTextureYLocation,
        BottomTextureYLocation,
        ClampedFrom,
        ClampedTo,
        TextureXPos
    }

    internal unsafe class AlignedMemoryPool
    {
        internal readonly IntPtr _ptr;
        internal readonly int _byteCount;
        internal readonly int _rem;

        internal readonly (int StartByte, int EndByte)[] Buckets;

        internal Span<byte> Span => new((void*)_ptr, _byteCount);

        private AlignedMemoryPool(int bucketSize, int numberOfBuckets)
        {
            // We use Vector<byte> which is the alignment check in Vector.Alignment (see source code)
            // We then ensure that bucketSize lands on an aligned boundary
            // so that all buckets are aligned at start

            bucketSize *= sizeof(int);
            int alignment = Vector<byte>.Count;
            _rem = bucketSize % alignment;
            bucketSize += _rem;

            _byteCount = bucketSize * numberOfBuckets;
            _ptr = (IntPtr)NativeMemory.AlignedAlloc((nuint)_byteCount, (nuint)alignment);

            Buckets = new (int, int)[numberOfBuckets];
            for (int i = 0; i < Buckets.Length; i++)
            {
                int start = i * bucketSize;
                Buckets[i] = (start, start + bucketSize);
            }
        }

        public Span<T> GetBucket<T>(MemoryPoolBucket bucket)
            where T : unmanaged
        {
            (int byteStart, int byteEnd) = Buckets[(int)bucket];
            return MemoryMarshal.Cast<byte, T>(Span[byteStart..(byteEnd - _rem)]);
        }

        public ref T GetBucketRef<T>(MemoryPoolBucket bucket)
            where T : unmanaged
        {
            (int byteStart, _) = Buckets[(int)bucket];

            byte* ptr = (byte*)_ptr.ToPointer();
            ptr += byteStart;

            return ref Unsafe.AsRef<T>((T*)ptr);
        }

        internal static AlignedMemoryPool GeneratePool(int bucketSize, int numberOfBuckets)
        {
            return new AlignedMemoryPool(bucketSize, numberOfBuckets);
        }

        ~AlignedMemoryPool()
        {
            // need to explicitly free aligned memory
            if (_ptr != IntPtr.Zero)
            {
                NativeMemory.AlignedFree((void*)_ptr);
            }
        }
    }
}
