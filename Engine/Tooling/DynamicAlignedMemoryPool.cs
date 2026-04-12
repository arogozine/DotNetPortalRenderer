using System.Numerics;

namespace RenderingEngine.Tooling;

internal unsafe class DynamicAlignedMemoryPool
{
    public int BucketSizeInBytes => _bucketSizeInBytes;

    public int NumberOfBuckets => Buckets.Length;

    internal int _bucketSizeInBytes;
    internal int _byteCount;
    internal int _rem;

    internal IntPtr[] Buckets;

    private DynamicAlignedMemoryPool(int bucketSize, int numberOfBuckets)
    {
        // We use Vector<byte> which is the alignment check in Vector.Alignment (see source code)
        // We then ensure that bucketSize lands on an aligned boundary
        // so that all buckets are aligned at start

        bucketSize *= sizeof(int);
        int alignment = Vector<byte>.Count;

        _rem = (alignment - 1) & bucketSize;
        _rem = (alignment - 1) & (alignment - _rem);

        bucketSize += _rem;
        _byteCount = bucketSize;

        Buckets = new IntPtr[numberOfBuckets];
        for (int i = 0; i < Buckets.Length; i++)
        {
            Buckets[i] = (IntPtr)NativeMemory.AlignedAlloc((nuint)_byteCount, (nuint)alignment);
        }

        this._bucketSizeInBytes = bucketSize;
    }

    public void ReAlloc(int bucketSize)
    {
        // We use Vector<byte> which is the alignment check in Vector.Alignment (see source code)
        // We then ensure that bucketSize lands on an aligned boundary
        // so that all buckets are aligned at start

        // We use Vector<byte> which is the alignment check in Vector.Alignment (see source code)
        // We then ensure that bucketSize lands on an aligned boundary
        // so that all buckets are aligned at start

        bucketSize *= sizeof(int);
        int alignment = Vector<byte>.Count;

        _rem = (alignment - 1) & bucketSize;
        _rem = (alignment - 1) & (alignment - _rem);

        bucketSize += _rem;
        _byteCount = bucketSize;

        for (int i = 0; i < Buckets.Length; i++)
        {
            Buckets[i] = (IntPtr)NativeMemory.AlignedRealloc((void*)Buckets[i], (nuint)_byteCount, (nuint)alignment);
        }

        this._bucketSizeInBytes = bucketSize;
    }

    public int GetBucketSize<T>()
        where T : unmanaged
    {
        return _bucketSizeInBytes / sizeof(T);
    }

    public Span<T> GetBucket<T>(int bucket)
        where T : unmanaged
    {
        byte* ptr = (byte*)Buckets[bucket];
        return MemoryMarshal.Cast<byte, T>(new Span<byte>(ptr, _bucketSizeInBytes));
    }

    public ref T GetBucketRef<T>(int bucket)
        where T : unmanaged
    {
        byte* ptr = (byte*)Buckets[bucket];

        return ref Unsafe.AsRef<T>((T*)ptr);
    }

    public void* GetBucketPtr(int bucket)
    {
        return (byte*)Buckets[bucket];
    }

    public void ClearBuckets(params ReadOnlySpan<int> buckets)
    {
        for (int i = 0; i < buckets.Length; i++)
        {
            GetBucket<byte>(buckets[i]).Clear();
        }
    }

    internal static DynamicAlignedMemoryPool GeneratePool(int bucketSize, int numberOfBuckets)
    {
        return new DynamicAlignedMemoryPool(bucketSize, numberOfBuckets);
    }

    ~DynamicAlignedMemoryPool()
    {
        for (int i = 0; i < Buckets.Length; i++)
        {
            IntPtr ptr = Buckets[i];

            if (ptr != IntPtr.Zero)
            {
                NativeMemory.AlignedFree((void*)ptr);
            }
        }
    }
}
