using System.Numerics;
using RenderingEngine.Tooling;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Tests;

public unsafe class AlignedMemoryPoolTests
{
    [Fact]
    public void GeneratePool_BucketsAreAlignedToVectorSize()
    {
        const int bucketSize = 16; // in ints (AlignedMemoryPool multiplies by sizeof(int))
        const int numBuckets = 3;
        var pool = AlignedMemoryPool.GeneratePool(bucketSize, numBuckets);

        IntPtr basePtr = pool._ptr;
        var buckets = pool.Buckets;

        int alignment = Vector<byte>.Count;
        long baseAddr = basePtr.ToInt64();

        for (int i = 0; i < buckets.Length; i++)
        {
            int start = buckets[i].StartByte;
            long addr = baseAddr + start;
            Assert.Equal(0, addr % alignment);
        }
    }

    [Fact]
    public void GetBucket_ResultCanBeVectorLoadedAligned()
    {
        const int bucketSize = 133;
        const int numBuckets = 71;
        var pool = AlignedMemoryPool.GeneratePool(bucketSize, numBuckets);

        for (int i = 0; i < numBuckets; i++)
        {
            Span<int> bucket = pool.GetBucket<int>((MemoryPoolBucket)i);

            _ = Vector.LoadAligned((int*)Unsafe.AsPointer(ref bucket[0]));
        }
    }

    [Fact]
    public void GeneratePool_BucketsAreDistinctAndCoverTotalRange()
    {
        const int bucketSize = 8; // in ints
        const int numBuckets = 4;
        var pool = AlignedMemoryPool.GeneratePool(bucketSize, numBuckets);

        int byteCount = pool._byteCount;
        var buckets = pool.Buckets;

        // Verify ranges are contiguous, non-overlapping and sum to total byte count
        int sum = 0;
        for (int i = 0; i < buckets.Length; i++)
        {
            int start = buckets[i].StartByte;
            int end = buckets[i].EndByte;
            Assert.True(end > start, "Bucket must have positive length");

            // Ensure no overlap with previous
            if (i > 0)
            {
                int prevEnd = buckets[i - 1].EndByte;
                Assert.Equal(prevEnd, start);
            }

            sum += end - start;
        }

        Assert.Equal(byteCount, sum);
    }

    [Fact]
    public void GetBucketRef_AddressMatchesGetBucket()
    {
        var pool = AlignedMemoryPool.GeneratePool(16, 3);

        var span = pool.GetBucket<int>(MemoryPoolBucket.AngleCache);
        ref int spanRef = ref MemoryMarshal.GetReference(span);

        ref int refFromMethod = ref pool.GetBucketRef<int>(MemoryPoolBucket.AngleCache);

        void* pSpan = Unsafe.AsPointer(ref spanRef);
        void* pRef = Unsafe.AsPointer(ref refFromMethod);

        Assert.Equal((long)pSpan, (long)pRef);
    }

    [Fact]
    public void GetBucketRef_WriteIsVisibleThroughGetBucketSpanAndViceVersa()
    {
        var pool = AlignedMemoryPool.GeneratePool(8, 2);

        var span = pool.GetBucket<int>(MemoryPoolBucket.AngleCache);
        ref int startRef = ref pool.GetBucketRef<int>(MemoryPoolBucket.AngleCache);

        span[0] = 12345;
        Assert.Equal(12345, startRef);

        startRef = 54321;
        Assert.Equal(54321, span[0]);
    }

    [Fact]
    public void GeneratePool_BucketSizeAndNumberOfBucketsReflectArguments()
    {
        const int bucketSize = 37;
        const int numBuckets = 5;
        var pool = AlignedMemoryPool.GeneratePool(bucketSize, numBuckets);

        Assert.Equal(numBuckets, pool.NumberOfBuckets);
        Assert.Equal(bucketSize, pool.BucketSize);
    }

    [Fact]
    public void GetBucketPtr_MatchesGetBucketRefAddress()
    {
        const int numBuckets = 4;
        var pool = AlignedMemoryPool.GeneratePool(16, numBuckets);

        for (int i = 0; i < numBuckets; i++)
        {
            void* ptr = pool.GetBucketPtr(i);
            void* refPtr = Unsafe.AsPointer(ref pool.GetBucketRef<int>(i));

            Assert.Equal((long)ptr, (long)refPtr);
        }
    }

    [Fact]
    public void ClearBuckets_ZeroesOnlyTargetedBucketsLeavesOthersIntact()
    {
        var pool = AlignedMemoryPool.GeneratePool(8, 2);

        var bucket0 = pool.GetBucket<int>(0);
        var bucket1 = pool.GetBucket<int>(1);

        for (int i = 0; i < bucket0.Length; i++)
        {
            bucket0[i] = 111;
            bucket1[i] = 222;
        }

        pool.ClearBuckets(0);

        foreach (int value in bucket0)
        {
            Assert.Equal(0, value);
        }

        foreach (int value in bucket1)
        {
            Assert.Equal(222, value);
        }
    }
}
