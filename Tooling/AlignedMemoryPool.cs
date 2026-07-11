using System.Numerics;

namespace RenderingEngine.Tooling;

public unsafe class AlignedMemoryPool
{
    public int BucketSize => _bucketSize;
    public int NumberOfBuckets => Buckets.Length;

    internal int _bucketSize;
    internal IntPtr _ptr;
    internal int _byteCount;
    internal int _rem;

    internal (int StartByte, int EndByte)[] Buckets;

    internal Span<byte> Span => new((void*)_ptr, _byteCount);

    private AlignedMemoryPool(int bucketSize, int numberOfBuckets)
    {
        // We use Vector<byte> which is the alignment check in Vector.Alignment (see source code)
        // We then ensure that bucketSize lands on an aligned boundary
        // so that all buckets are aligned at start

        this._bucketSize = bucketSize;

        bucketSize *= sizeof(int);
        int alignment = Vector<byte>.Count;

        _rem = (alignment - 1) & bucketSize;
        _rem = (alignment - 1) & (alignment - _rem);

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

    public Span<T> GetBucket<T>(int bucket)
        where T : unmanaged
    {
        (int byteStart, int byteEnd) = Buckets[bucket];
        return MemoryMarshal.Cast<byte, T>(Span[byteStart..(byteEnd - _rem)]);
    }

    public ref T GetBucketRef<T>(int bucket)
        where T : unmanaged
    {
        (int byteStart, _) = Buckets[bucket];

        byte* ptr = (byte*)_ptr.ToPointer();
        ptr += byteStart;

        return ref Unsafe.AsRef<T>((T*)ptr);
    }

    public void* GetBucketPtr(int bucket)
    {
        (int byteStart, _) = Buckets[bucket];

        byte* ptr = (byte*)_ptr.ToPointer();
        ptr += byteStart;
        return ptr;
    }

    public void ClearBuckets(params ReadOnlySpan<int> buckets)
    {
        for (int i = 0; i < buckets.Length; i++)
        {
            GetBucket<byte>(buckets[i]).Clear();
        }
    }

    public static AlignedMemoryPool GeneratePool(int bucketSize, int numberOfBuckets)
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
