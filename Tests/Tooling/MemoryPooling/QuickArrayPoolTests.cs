using Tooling;

namespace Tests;

public class QuickArrayPoolTests
{
    [Fact]
    public void Request_SequentialSmallAllocationsStayWithinInitialBucketAndReturnCorrectSegments()
    {
        var pool = new QuickArrayPool<int>();

        var segment1 = pool.Request(5);
        var segment2 = pool.Request(5);

        Assert.Equal(5, segment1.Count);
        Assert.Equal(5, segment2.Count);
        Assert.Same(segment1.Array, segment2.Array);
        Assert.Equal(0, segment1.Offset);
        Assert.Equal(5, segment2.Offset);
    }

    [Fact]
    public void Request_ExceedingInitialBucketCapacityGrowsToNewLargerBucket()
    {
        var pool = new QuickArrayPool<int>();

        var initial = pool.Request(30);
        var overflow = pool.Request(10);

        Assert.NotSame(initial.Array, overflow.Array);
        Assert.Equal(10, overflow.Count);
        Assert.True(overflow.Array!.Length >= 10);
    }

    [Fact]
    public void Request_ReturnedSegmentsHaveCorrectCountMatchingRequestedSize()
    {
        var pool = new QuickArrayPool<int>();

        foreach (int size in new[] { 1, 4, 16, 64 })
        {
            var segment = pool.Request(size);
            Assert.Equal(size, segment.Count);
        }
    }

    [Fact]
    public void Request_RepeatedGrowthAcrossMultipleBucketGenerationsSucceedsWithoutException()
    {
        var pool = new QuickArrayPool<int>();
        var arrays = new HashSet<int[]>();

        for (int i = 0; i < 20; i++)
        {
            var segment = pool.Request(50);
            arrays.Add(segment.Array!);
        }

        Assert.True(arrays.Count > 1);
    }

    [Fact]
    public void Request_SegmentsFromDifferentBucketGenerationsUseDistinctBackingArrays()
    {
        var pool = new QuickArrayPool<int>();

        var first = pool.Request(30);
        var second = pool.Request(30);

        Assert.NotSame(first.Array, second.Array);
    }

    [Fact]
    public void ClearAndOptimize_WithOnlyInitialBucket_ClearsInPlaceAndSubsequentRequestStartsFromOffsetZero()
    {
        var pool = new QuickArrayPool<int>();

        var segment = pool.Request(10);
        segment[0] = 42;

        pool.ClearAndOptimize();

        var afterClear = pool.Request(5);
        Assert.Equal(0, afterClear.Offset);
        Assert.Equal(0, afterClear[0]);
    }

    [Fact]
    public void ClearAndOptimize_AfterGrowth_ConsolidatesToSingleBucketAndSubsequentRequestWithinOldTotalCapacitySucceeds()
    {
        var pool = new QuickArrayPool<int>();

        pool.Request(30);
        pool.Request(20);

        pool.ClearAndOptimize();

        var first = pool.Request(10);
        var second = pool.Request(10);

        Assert.Same(first.Array, second.Array);
    }

    [Fact]
    public void ClearAndOptimize_ConsolidatedBucketZeroesPreviouslyWrittenData()
    {
        var pool = new QuickArrayPool<int>();

        var segment1 = pool.Request(30);
        segment1[0] = 111;
        var segment2 = pool.Request(20);
        segment2[0] = 222;

        pool.ClearAndOptimize();

        var afterOptimize = pool.Request(50);
        foreach (int value in afterOptimize.AsSpan())
        {
            Assert.Equal(0, value);
        }
    }

    [Fact]
    public void Request_ConcurrentCallsFromMultipleThreads_CompleteWithoutExceptionAndProduceNonOverlappingSegments()
    {
        var pool = new QuickArrayPool<int>();
        const int threadCount = 8;
        const int requestsPerThread = 50;
        const int requestSize = 4;

        var allSegments = new System.Collections.Concurrent.ConcurrentBag<Tooling.ArraySegment<int>>();

        Parallel.For(0, threadCount, _ =>
        {
            for (int i = 0; i < requestsPerThread; i++)
            {
                allSegments.Add(pool.Request(requestSize));
            }
        });

        Assert.Equal(threadCount * requestsPerThread, allSegments.Count);

        var byArray = allSegments.GroupBy(s => s.Array);
        foreach (var group in byArray)
        {
            var ranges = group.Select(s => (s.Offset, s.Count)).OrderBy(r => r.Offset).ToList();
            for (int i = 1; i < ranges.Count; i++)
            {
                Assert.True(ranges[i].Offset >= ranges[i - 1].Offset + ranges[i - 1].Count);
            }
        }
    }
}
