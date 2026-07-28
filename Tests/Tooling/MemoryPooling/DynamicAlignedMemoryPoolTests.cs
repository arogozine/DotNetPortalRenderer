using System.Numerics;
using RenderingEngine.Tooling;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Tests;

public unsafe class DynamicAlignedMemoryPoolTests
{
    [Fact]
    public void GeneratePool_EachBucketStartIsAlignedToVectorByteCount()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(133, 5);

        int alignment = Vector<byte>.Count;

        for (int i = 0; i < pool.NumberOfBuckets; i++)
        {
            long addr = pool.Buckets[i].ToInt64();
            Assert.Equal(0, addr % alignment);
        }
    }

    [Fact]
    public void GetBucket_ResultCanBeVectorLoadedAligned()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(133, 5);

        for (int i = 0; i < pool.NumberOfBuckets; i++)
        {
            Span<int> bucket = pool.GetBucket<int>(i);
            _ = Vector.LoadAligned((int*)Unsafe.AsPointer(ref bucket[0]));
        }
    }

    [Fact]
    public void GetBucketRef_AddressMatchesGetBucket()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(16, 3);

        var span = pool.GetBucket<int>(1);
        ref int spanRef = ref MemoryMarshal.GetReference(span);

        ref int refFromMethod = ref pool.GetBucketRef<int>(1);

        void* pSpan = Unsafe.AsPointer(ref spanRef);
        void* pRef = Unsafe.AsPointer(ref refFromMethod);

        Assert.Equal((long)pSpan, (long)pRef);
    }

    [Fact]
    public void GetBucketPtr_MatchesGetBucketRefAddress()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(16, 4);

        for (int i = 0; i < pool.NumberOfBuckets; i++)
        {
            void* ptr = pool.GetBucketPtr(i);
            void* refPtr = Unsafe.AsPointer(ref pool.GetBucketRef<int>(i));

            Assert.Equal((long)ptr, (long)refPtr);
        }
    }

    [Fact]
    public void GetBucketRef_WriteIsVisibleThroughGetBucketSpanAndViceVersa()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(8, 2);

        var span = pool.GetBucket<int>(0);
        ref int startRef = ref pool.GetBucketRef<int>(0);

        span[0] = 12345;
        Assert.Equal(12345, startRef);

        startRef = 54321;
        Assert.Equal(54321, span[0]);
    }

    [Fact]
    public void GetBucket_BucketsAreIndependentlyAddressable_WritingOneBucketDoesNotAffectAnother()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(8, 2);

        var bucket0 = pool.GetBucket<int>(0);
        var bucket1 = pool.GetBucket<int>(1);

        for (int i = 0; i < bucket1.Length; i++)
        {
            bucket1[i] = 555;
        }

        for (int i = 0; i < bucket0.Length; i++)
        {
            bucket0[i] = 999;
        }

        foreach (int value in bucket1)
        {
            Assert.Equal(555, value);
        }
    }

    [Fact]
    public void ClearBuckets_ZeroesOnlyTargetedBucketsLeavesOthersIntact()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(8, 2);

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

    [Fact]
    public void GetBucketSize_ReturnsElementCountForInt()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(64, 1);

        Assert.Equal(pool.BucketSizeInBytes / sizeof(int), pool.GetBucketSize<int>());
    }

    [Fact]
    public void GetBucketSize_ReturnsElementCountForByte()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(64, 1);

        Assert.Equal(pool.BucketSizeInBytes, pool.GetBucketSize<byte>());
    }

    [Fact]
    public void ReAlloc_Growing_PreservesExistingData()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(4, 2);

        var bucket0 = pool.GetBucket<int>(0);
        bucket0[0] = 4242;

        int oldSizeInBytes = pool.BucketSizeInBytes;

        pool.ReAlloc(64);

        Assert.True(pool.BucketSizeInBytes > oldSizeInBytes);

        var grownBucket0 = pool.GetBucket<int>(0);
        Assert.Equal(4242, grownBucket0[0]);
    }

    [Fact]
    public void ReAlloc_Shrinking_DoesNotThrowAndPreservesDataWithinNewSize()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(64, 2);

        var bucket0 = pool.GetBucket<int>(0);
        bucket0[0] = 7777;

        pool.ReAlloc(4);

        var shrunkBucket0 = pool.GetBucket<int>(0);
        Assert.Equal(7777, shrunkBucket0[0]);
    }

    [Fact]
    public void ReAlloc_RealignsEachBucketStartToVectorByteCount()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(4, 3);

        pool.ReAlloc(133);

        int alignment = Vector<byte>.Count;

        for (int i = 0; i < pool.NumberOfBuckets; i++)
        {
            long addr = pool.Buckets[i].ToInt64();
            Assert.Equal(0, addr % alignment);
        }
    }

    [Fact]
    public void GeneratePool_NumberOfBucketsReflectsConstructorArgument()
    {
        var pool = DynamicAlignedMemoryPool.GeneratePool(16, 7);

        Assert.Equal(7, pool.NumberOfBuckets);
    }

    [Fact]
    public void GeneratePool_BucketSizeInBytesIsPositiveAndSizeofIntScaled()
    {
        const int bucketSize = 10;
        var pool = DynamicAlignedMemoryPool.GeneratePool(bucketSize, 1);

        Assert.True(pool.BucketSizeInBytes > 0);
        Assert.True(pool.BucketSizeInBytes >= bucketSize * sizeof(int));
    }
}
