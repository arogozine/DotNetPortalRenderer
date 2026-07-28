using System.Collections;
using System.Collections.Generic;
using Tooling;

namespace Tests;

public class ArraySegmentTests
{
    [Fact]
    public void Constructor_ArrayOnly_WrapsEntireArrayStartingAtOffsetZero()
    {
        int[] array = [1, 2, 3];
        var segment = new Tooling.ArraySegment<int>(array);

        Assert.Same(array, segment.Array);
        Assert.Equal(0, segment.Offset);
        Assert.Equal(3, segment.Count);
    }

    [Fact]
    public void Constructor_ArrayOnly_NullArrayThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Tooling.ArraySegment<int>(null!));
    }

    [Fact]
    public void Constructor_ArrayOffsetCount_ValidRangeSetsPropertiesCorrectly()
    {
        int[] array = [1, 2, 3, 4, 5];
        var segment = new Tooling.ArraySegment<int>(array, 1, 3);

        Assert.Same(array, segment.Array);
        Assert.Equal(1, segment.Offset);
        Assert.Equal(3, segment.Count);
    }

    [Fact]
    public void Constructor_ArrayOffsetCount_NullArrayThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Tooling.ArraySegment<int>(null!, 0, 0));
    }

    [Fact]
    public void Constructor_ArrayOffsetCount_NegativeOffsetOrCountThrowsArgumentOutOfRangeException()
    {
        int[] array = [1, 2, 3];

        Assert.Throws<ArgumentOutOfRangeException>(() => new Tooling.ArraySegment<int>(array, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tooling.ArraySegment<int>(array, 0, -1));
    }

    [Fact]
    public void Constructor_ArrayOffsetCount_OutOfBoundsRangeThrowsArgumentException()
    {
        int[] array = [1, 2, 3];

        Assert.Throws<ArgumentException>(() => new Tooling.ArraySegment<int>(array, 4, 0));
        Assert.Throws<ArgumentException>(() => new Tooling.ArraySegment<int>(array, 1, 3));
    }

    [Fact]
    public void Constructor_ArrayOffsetCount_BoundaryOffsetEqualsLengthWithZeroCountSucceeds()
    {
        int[] array = [1, 2, 3];
        var segment = new Tooling.ArraySegment<int>(array, 3, 0);

        Assert.Equal(3, segment.Offset);
        Assert.Empty(segment);
    }

    [Fact]
    public void Empty_IsZeroCountWithNonNullBackingArray()
    {
        Assert.Empty(Tooling.ArraySegment<int>.Empty);
        Assert.NotNull(Tooling.ArraySegment<int>.Empty.Array);
    }

    [Fact]
    public void Indexer_GetSet_ReadsAndWritesThroughToUnderlyingArrayAtOffset()
    {
        int[] array = [1, 2, 3, 4, 5];
        var segment = new Tooling.ArraySegment<int>(array, 1, 3);

        Assert.Equal(2, segment[0]);

        segment[0] = 99;
        Assert.Equal(99, array[1]);
    }

    [Fact]
    public void Indexer_OutOfRangeIndexThrowsArgumentOutOfRangeException()
    {
        int[] array = [1, 2, 3];
        var segment = new Tooling.ArraySegment<int>(array, 0, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => segment[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => segment[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => segment[-1] = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => segment[2] = 0);
    }

    [Fact]
    public void GetEnumerator_OnDefaultUninitializedSegmentThrowsInvalidOperationException()
    {
        Tooling.ArraySegment<int> segment = default;

        Assert.Throws<InvalidOperationException>(() => segment.GetEnumerator());
    }

    [Fact]
    public void Foreach_EnumeratesAllElementsInOffsetOrder()
    {
        int[] array = [1, 2, 3, 4, 5];
        var segment = new Tooling.ArraySegment<int>(array, 1, 3);

        var values = new List<int>();
        foreach (int value in segment)
        {
            values.Add(value);
        }

        Assert.Equal([2, 3, 4], values);
    }

    [Fact]
    public void Enumerator_CurrentBeforeMoveNextThrowsInvalidOperationException()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3]);
        var enumerator = segment.GetEnumerator();

        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void Enumerator_CurrentAfterEnumerationExhaustedThrowsInvalidOperationException()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2]);
        var enumerator = segment.GetEnumerator();

        while (enumerator.MoveNext()) { }

        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void Enumerator_MoveNextOnZeroCountSegmentReturnsFalseWithoutThrowing()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3], 1, 0);
        var enumerator = segment.GetEnumerator();

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Enumerator_ResetViaIEnumeratorAllowsReenumerationFromStart()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3]);
        IEnumerator<int> enumerator = ((IEnumerable<int>)segment).GetEnumerator();

        enumerator.MoveNext();
        Assert.Equal(1, enumerator.Current);

        enumerator.Reset();

        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
    }

    [Fact]
    public void GetHashCode_EqualSegmentsProduceEqualHashCodes()
    {
        int[] array = [1, 2, 3];
        var segment1 = new Tooling.ArraySegment<int>(array, 0, 2);
        var segment2 = new Tooling.ArraySegment<int>(array, 0, 2);

        Assert.Equal(segment1.GetHashCode(), segment2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DefaultSegmentReturnsZero()
    {
        Tooling.ArraySegment<int> segment = default;
        Assert.Equal(0, segment.GetHashCode());
    }

    [Fact]
    public void Equals_SameArraySameOffsetSameCountAreEqual()
    {
        int[] array = [1, 2, 3];
        var segment1 = new Tooling.ArraySegment<int>(array, 0, 2);
        var segment2 = new Tooling.ArraySegment<int>(array, 0, 2);

        Assert.True(segment1.Equals(segment2));
        Assert.True(segment1.Equals((object)segment2));
    }

    [Fact]
    public void Equals_DifferingOffsetOrCountOrArrayReferenceAreNotEqual()
    {
        int[] array = [1, 2, 3, 4];
        var baseline = new Tooling.ArraySegment<int>(array, 0, 2);

        Assert.False(baseline.Equals(new Tooling.ArraySegment<int>(array, 1, 2)));
        Assert.False(baseline.Equals(new Tooling.ArraySegment<int>(array, 0, 3)));

        int[] otherArrayWithSameContent = [1, 2, 3, 4];
        Assert.False(baseline.Equals(new Tooling.ArraySegment<int>(otherArrayWithSameContent, 0, 2)));
    }

    [Fact]
    public void EqualityOperators_MatchEqualsSemantics()
    {
        int[] array = [1, 2, 3];
        var segment1 = new Tooling.ArraySegment<int>(array, 0, 2);
        var segment2 = new Tooling.ArraySegment<int>(array, 0, 2);
        var segment3 = new Tooling.ArraySegment<int>(array, 1, 2);

        Assert.True(segment1 == segment2);
        Assert.False(segment1 != segment2);
        Assert.True(segment1 != segment3);
        Assert.False(segment1 == segment3);
    }

    [Fact]
    public void CopyTo_ArrayOverload_DefaultsToDestinationIndexZero()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3]);
        int[] destination = new int[3];

        segment.CopyTo(destination);

        Assert.Equal([1, 2, 3], destination);
    }

    [Fact]
    public void CopyTo_ArrayOverloadWithDestinationIndex_CopiesToCorrectPosition()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3]);
        int[] destination = new int[5];

        segment.CopyTo(destination, 2);

        Assert.Equal([0, 0, 1, 2, 3], destination);
    }

    [Fact]
    public void CopyTo_ArrayOverload_DestinationTooSmallThrowsArgumentException()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3]);
        int[] destination = new int[2];

        Assert.ThrowsAny<ArgumentException>(() => segment.CopyTo(destination));
    }

    [Fact]
    public void CopyTo_ArraySegmentOverload_CopiesIntoDestinationRange()
    {
        var source = new Tooling.ArraySegment<int>([1, 2, 3]);
        int[] destinationArray = new int[5];
        var destination = new Tooling.ArraySegment<int>(destinationArray, 1, 3);

        source.CopyTo(destination);

        Assert.Equal([0, 1, 2, 3, 0], destinationArray);
    }

    [Fact]
    public void CopyTo_ArraySegmentOverload_DestinationTooShortThrowsArgumentException()
    {
        var source = new Tooling.ArraySegment<int>([1, 2, 3]);
        var destination = new Tooling.ArraySegment<int>(new int[2]);

        Assert.Throws<ArgumentException>(() => source.CopyTo(destination));
    }

    [Fact]
    public void Slice_SingleIndexOverload_ReturnsCorrectTailAndSupportsFullAndEmptyBoundaries()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3, 4, 5], 1, 3);

        var tail = segment.Slice(1);
        Assert.Equal(2, tail.Count);
        Assert.Equal(3, tail[0]);

        var full = segment.Slice(0);
        Assert.Equal(segment.Count, full.Count);

        var empty = segment.Slice(3);
        Assert.Empty(empty);
    }

    [Fact]
    public void Slice_SingleIndexOverload_OutOfRangeThrowsArgumentOutOfRangeException()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3]);

        Assert.Throws<ArgumentOutOfRangeException>(() => segment.Slice(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => segment.Slice(4));
    }

    [Fact]
    public void Slice_IndexAndCountOverload_ReturnsCorrectSubRange()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3, 4, 5], 1, 3);

        var sub = segment.Slice(1, 1);

        Assert.Single(sub);
        Assert.Equal(3, sub[0]);
    }

    [Fact]
    public void Slice_IndexAndCountOverload_OutOfRangeThrowsArgumentOutOfRangeException()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3]);

        Assert.Throws<ArgumentOutOfRangeException>(() => segment.Slice(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => segment.Slice(0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => segment.Slice(2, 2));
    }

    [Fact]
    public void ToArray_NonEmptySegmentReturnsIndependentCopyNotSameArrayReference()
    {
        int[] array = [1, 2, 3];
        var segment = new Tooling.ArraySegment<int>(array);

        int[] copy = segment.ToArray();

        Assert.Equal(array, copy);
        Assert.NotSame(array, copy);
    }

    [Fact]
    public void ToArray_ZeroCountSegmentReturnsSameSingletonReferenceAsEmptyArray()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3], 1, 0);

        int[] result = segment.ToArray();

        Assert.Same(Tooling.ArraySegment<int>.Empty.Array, result);
    }

    [Fact]
    public void ToArray_OnDefaultSegmentThrowsInvalidOperationException()
    {
        Tooling.ArraySegment<int> segment = default;

        Assert.Throws<InvalidOperationException>(() => segment.ToArray());
    }

    [Fact]
    public void AsSpan_Overloads_ReflectOffsetAndCountAndAreBoundsChecked()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3, 4, 5], 1, 3);

        Span<int> full = segment.AsSpan();
        Assert.Equal(3, full.Length);
        Assert.Equal(2, full[0]);

        Span<int> fromStart = segment.AsSpan(1);
        Assert.Equal(2, fromStart.Length);
        Assert.Equal(3, fromStart[0]);

        Span<int> ranged = segment.AsSpan(1, 1);
        Assert.Equal(1, ranged.Length);
        Assert.Equal(3, ranged[0]);

        Assert.Throws<ArgumentOutOfRangeException>(() => segment.AsSpan(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => segment.AsSpan(0, 10));
    }

    [Fact]
    public void AsMemory_ReflectsOffsetAndCount()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3, 4, 5], 1, 3);

        Memory<int> memory = segment.AsMemory();

        Assert.Equal(3, memory.Length);
        Assert.Equal(2, memory.Span[0]);
    }

    [Fact]
    public void DefaultSegment_AllAccessorMethods_ThrowInvalidOperationException()
    {
        Tooling.ArraySegment<int> segment = default;

        Assert.Throws<InvalidOperationException>(() => segment.AsSpan());
        Assert.Throws<InvalidOperationException>(() => segment.AsSpan(0));
        Assert.Throws<InvalidOperationException>(() => segment.AsSpan(0, 0));
        Assert.Throws<InvalidOperationException>(() => segment.ToArray());
        Assert.Throws<InvalidOperationException>(() => segment.Slice(0));
        Assert.Throws<InvalidOperationException>(() => segment.CopyTo(new int[1]));
    }

    [Fact]
    public void ImplicitConversion_FromArray_WrapsWholeArrayAndFromNullYieldsDefault()
    {
        int[] array = [1, 2, 3];
        Tooling.ArraySegment<int> segment = array;

        Assert.Same(array, segment.Array);
        Assert.Equal(3, segment.Count);

        int[]? nullArray = null;
        Tooling.ArraySegment<int> fromNull = nullArray!;

        Assert.Null(fromNull.Array);
    }

    [Fact]
    public void ImplicitConversion_ToSpanReadOnlySpanMemoryReadOnlyMemory_AllReflectOffsetAndCount()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3, 4, 5], 1, 3);

        Span<int> span = segment;
        ReadOnlySpan<int> readOnlySpan = segment;
        Memory<int> memory = segment;
        ReadOnlyMemory<int> readOnlyMemory = segment;

        Assert.Equal(3, span.Length);
        Assert.Equal(3, readOnlySpan.Length);
        Assert.Equal(3, memory.Length);
        Assert.Equal(3, readOnlyMemory.Length);
        Assert.Equal(2, span[0]);
        Assert.Equal(2, readOnlySpan[0]);
    }

    [Fact]
    public void IList_ExplicitIndexerGetSet_WorksThroughInterfaceCast()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3], 0, 2);
        IList<int> list = segment;

        Assert.Equal(2, list[1]);

        list[1] = 42;
        Assert.Equal(42, segment[1]);
    }

    [Fact]
    public void IList_IndexOf_ReturnsRelativeIndexOrNegativeOneWhenAbsent()
    {
        var segment = new Tooling.ArraySegment<int>([10, 1, 2, 3, 10], 1, 3);
        IList<int> list = segment;

        Assert.Equal(1, list.IndexOf(2));
        Assert.Equal(-1, list.IndexOf(10));
    }

    [Fact]
    public void IList_InsertAndRemoveAt_ThrowNotSupportedException()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3]);
        IList<int> list = segment;

        Assert.Throws<NotSupportedException>(() => list.Insert(0, 5));
        Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
    }

    [Fact]
    public void IReadOnlyList_ExplicitIndexer_ReturnsCorrectElement()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3], 1, 2);
        IReadOnlyList<int> list = segment;

        Assert.Equal(2, list[0]);
        Assert.Equal(3, list[1]);
    }

    [Fact]
    public void ICollection_IsReadOnlyIsTrueAndAddClearRemoveThrowNotSupportedException()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3]);
        ICollection<int> collection = segment;

        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Add(1));
        Assert.Throws<NotSupportedException>(() => collection.Clear());
        Assert.Throws<NotSupportedException>(() => collection.Remove(1));
    }

    [Fact]
    public void ICollection_Contains_ReturnsExpectedTrueFalse()
    {
        var segment = new Tooling.ArraySegment<int>([10, 1, 2, 3, 10], 1, 3);
        ICollection<int> collection = segment;

        Assert.True(collection.Contains(2));
        Assert.False(collection.Contains(10));
    }

    [Fact]
    public void IEnumerable_NonGenericGetEnumerator_YieldsSameSequenceAsGenericEnumerator()
    {
        var segment = new Tooling.ArraySegment<int>([1, 2, 3]);
        IEnumerable nonGeneric = segment;

        var values = new List<int>();
        IEnumerator enumerator = nonGeneric.GetEnumerator();
        while (enumerator.MoveNext())
        {
            values.Add((int)enumerator.Current!);
        }

        Assert.Equal([1, 2, 3], values);
    }
}
