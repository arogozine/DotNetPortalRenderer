using Tooling;

namespace Tests;

public class ArrayPoolBucketTests
{
    [Fact]
    public void TryAllocate_SequentialAllocationsAdvanceOffsetAndReturnCorrectSegments()
    {
        var bucket = new ArrayPoolBucket<int> { Array = new int[10] };

        bool first = bucket.TryAllocate(3, out Tooling.ArraySegment<int> segment1);
        Assert.True(first);
        Assert.Equal(0, segment1.Offset);
        Assert.Equal(3, segment1.Count);
        Assert.Equal(3, bucket.Offset);

        bool second = bucket.TryAllocate(4, out Tooling.ArraySegment<int> segment2);
        Assert.True(second);
        Assert.Equal(3, segment2.Offset);
        Assert.Equal(4, segment2.Count);
        Assert.Equal(7, bucket.Offset);
    }

    [Fact]
    public void TryAllocate_ReturnedSegmentSharesBackingArrayWithBucket()
    {
        var array = new int[10];
        var bucket = new ArrayPoolBucket<int> { Array = array };

        bucket.TryAllocate(5, out Tooling.ArraySegment<int> segment);

        Assert.Same(array, segment.Array);
    }

    [Fact]
    public void TryAllocate_ExactlyFillingRemainingCapacitySucceeds()
    {
        var bucket = new ArrayPoolBucket<int> { Array = new int[8] };

        bool result = bucket.TryAllocate(8, out Tooling.ArraySegment<int> segment);

        Assert.True(result);
        Assert.Equal(8, segment.Count);
        Assert.Equal(8, bucket.Offset);
    }

    [Fact]
    public void TryAllocate_ExceedingRemainingCapacityByOneFailsAndLeavesOffsetUnchanged()
    {
        var bucket = new ArrayPoolBucket<int> { Array = new int[8] };
        bucket.TryAllocate(5, out _);

        bool result = bucket.TryAllocate(4, out Tooling.ArraySegment<int> segment);

        Assert.False(result);
        Assert.Null(segment.Array);
        Assert.Equal(5, bucket.Offset);
    }

    [Fact]
    public void TryAllocate_AfterCapacityExhaustedFurtherRequestsAlsoFail()
    {
        var bucket = new ArrayPoolBucket<int> { Array = new int[4] };
        bucket.TryAllocate(4, out _);

        bool result = bucket.TryAllocate(1, out Tooling.ArraySegment<int> segment);

        Assert.False(result);
        Assert.Null(segment.Array);
    }

    [Fact]
    public void TryAllocate_ZeroSizeRequestSucceedsAndDoesNotAdvanceOffset()
    {
        var bucket = new ArrayPoolBucket<int> { Array = new int[4] };

        bool result = bucket.TryAllocate(0, out Tooling.ArraySegment<int> segment);

        Assert.True(result);
        Assert.Empty(segment);
        Assert.Equal(0, bucket.Offset);
    }

    [Fact]
    public void Clear_ZeroesWrittenDataWithinUsedRangeAndResetsOffsetToZero()
    {
        var bucket = new ArrayPoolBucket<int> { Array = new int[4] };
        bucket.TryAllocate(3, out Tooling.ArraySegment<int> segment);

        segment[0] = 1;
        segment[1] = 2;
        segment[2] = 3;

        bucket.Clear();

        Assert.Equal(0, bucket.Offset);
        Assert.Equal(0, bucket.Array[0]);
        Assert.Equal(0, bucket.Array[1]);
        Assert.Equal(0, bucket.Array[2]);
    }

    [Fact]
    public void Clear_LeavesDataBeyondPreviousOffsetUntouched()
    {
        var bucket = new ArrayPoolBucket<int> { Array = new int[4] };
        bucket.TryAllocate(2, out _);

        bucket.Array[3] = 999;

        bucket.Clear();

        Assert.Equal(999, bucket.Array[3]);
    }

    [Fact]
    public void TryAllocate_AfterClearStartsAgainFromOffsetZero()
    {
        var bucket = new ArrayPoolBucket<int> { Array = new int[4] };
        bucket.TryAllocate(3, out _);
        bucket.Clear();

        bool result = bucket.TryAllocate(2, out Tooling.ArraySegment<int> segment);

        Assert.True(result);
        Assert.Equal(0, segment.Offset);
        Assert.Equal(2, bucket.Offset);
    }
}
