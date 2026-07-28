using RenderingEngine.Tooling;

namespace Tests;

public class ArraySortHelperTests
{
    private sealed class IntComparer : IComparer<int>
    {
        public int Compare(int x, int y) => x.CompareTo(y);
    }

    private sealed class ReverseIntComparer : IComparer<int>
    {
        public int Compare(int x, int y) => y.CompareTo(x);
    }

    private sealed record Widget(int Priority, string Name);

    private sealed class WidgetPriorityComparer : IComparer<Widget>
    {
        public int Compare(Widget? x, Widget? y) => x!.Priority.CompareTo(y!.Priority);
    }

    private static void AssertSortsLikeOracle(int[] input)
    {
        int[] expected = (int[])input.Clone();
        Array.Sort(expected);

        int[] actual = (int[])input.Clone();
        ArraySortHelper.Sort(actual.AsSpan(), new IntComparer());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Sort_EmptyArray_DoesNotThrow()
    {
        int[] array = [];
        ArraySortHelper.Sort(array.AsSpan(), new IntComparer());
        Assert.Empty(array);
    }

    [Fact]
    public void Sort_SingleElementArray_RemainsUnchanged()
    {
        int[] array = [42];
        ArraySortHelper.Sort(array.AsSpan(), new IntComparer());
        Assert.Equal([42], array);
    }

    [Fact]
    public void Sort_TwoElementArray_SortsCorrectlyBothOrders()
    {
        AssertSortsLikeOracle([2, 1]);
        AssertSortsLikeOracle([1, 2]);
    }

    [Fact]
    public void Sort_ThreeElementArray_SortsCorrectlyAllPermutations()
    {
        int[] baseValues = [1, 2, 3];
        foreach (var permutation in Permute(baseValues))
        {
            AssertSortsLikeOracle(permutation);
        }
    }

    private static IEnumerable<int[]> Permute(int[] values)
    {
        if (values.Length <= 1)
        {
            yield return values;
            yield break;
        }

        for (int i = 0; i < values.Length; i++)
        {
            int[] rest = values.Where((_, idx) => idx != i).ToArray();
            foreach (var permutation in Permute(rest))
            {
                yield return [values[i], .. permutation];
            }
        }
    }

    [Fact]
    public void Sort_InsertionSortRange_4To16Elements_MatchesArraySortOracleForRandomInput()
    {
        var random = new Random(1);

        for (int size = 4; size <= 16; size++)
        {
            int[] array = Enumerable.Range(0, size).Select(_ => random.Next(-1000, 1000)).ToArray();
            AssertSortsLikeOracle(array);
        }
    }

    [Fact]
    public void Sort_IntrosortRange_17To100Elements_MatchesArraySortOracleForRandomInput()
    {
        var random = new Random(2);

        for (int size = 17; size <= 100; size++)
        {
            int[] array = Enumerable.Range(0, size).Select(_ => random.Next(-10000, 10000)).ToArray();
            AssertSortsLikeOracle(array);
        }
    }

    [Fact]
    public void Sort_LargeArray_500To2000Elements_MatchesArraySortOracleForRandomInput()
    {
        var random = new Random(3);

        foreach (int size in new[] { 500, 999, 1500, 2000 })
        {
            int[] array = Enumerable.Range(0, size).Select(_ => random.Next()).ToArray();
            AssertSortsLikeOracle(array);
        }
    }

    [Fact]
    public void Sort_AlreadySortedInput_RemainsUnchangedAndMatchesOracle()
    {
        int[] array = Enumerable.Range(0, 200).ToArray();
        AssertSortsLikeOracle(array);
    }

    [Fact]
    public void Sort_ReverseSortedInput_MatchesOracleAfterSort()
    {
        int[] array = Enumerable.Range(0, 200).Reverse().ToArray();
        AssertSortsLikeOracle(array);
    }

    [Fact]
    public void Sort_AllDuplicateValues_MatchesOracle()
    {
        int[] array = Enumerable.Repeat(7, 200).ToArray();
        AssertSortsLikeOracle(array);
    }

    [Fact]
    public void Sort_AdversarialOrganPipePattern_LargeArray_MatchesOracle()
    {
        const int size = 2000;
        int[] array = new int[size];
        for (int i = 0; i < size / 2; i++)
        {
            array[i] = i;
            array[size - 1 - i] = i;
        }

        AssertSortsLikeOracle(array);
    }

    [Fact]
    public void Sort_ManyRepeatedValuesLargeArray_MatchesOracle()
    {
        var random = new Random(4);
        int[] array = Enumerable.Range(0, 2000).Select(_ => random.Next(0, 5)).ToArray();

        AssertSortsLikeOracle(array);
    }

    [Fact]
    public void Sort_WithCustomReferenceTypeAndComparer_SortsByComparerCriteria()
    {
        Widget[] widgets =
        [
            new Widget(3, "c"),
            new Widget(1, "a"),
            new Widget(2, "b"),
        ];

        ArraySortHelper.Sort(widgets.AsSpan(), new WidgetPriorityComparer());

        Assert.Equal(["a", "b", "c"], widgets.Select(w => w.Name));
    }

    [Fact]
    public void Sort_WithReverseComparer_ProducesDescendingOrder()
    {
        int[] array = [5, 3, 1, 4, 2];
        ArraySortHelper.Sort(array.AsSpan(), new ReverseIntComparer());

        Assert.Equal([5, 4, 3, 2, 1], array);
    }
}
