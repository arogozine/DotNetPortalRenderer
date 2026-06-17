// ArraySortHelper<T> modified from .NET Reference Source
// Original Design (c) Microsoft

// Modified because the original allocates a delegate for some reason

using System.Numerics;

namespace RenderingEngine.Tooling;

internal static class ArraySortHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sort<T, C>(Span<T> keys, C comparer)
        where C : IComparer<T>
    {
        ArraySortHelper<T, C>.Sort(keys, comparer);
    }
}

internal static class ArraySortHelper<T, C>
    where C : IComparer<T>
{
    private static int IntrosortSizeThreshold = 16;

    #region IArraySortHelper<T> Members

    public static void Sort(Span<T> keys, IComparer<T> comparer)
    {
        if (keys.Length > 1)
        {
            IntroSort(keys, 2 * (BitOperations.Log2((uint)keys.Length) + 1), comparer);
        }
    }

    #endregion

    private static void SwapIfGreater(Span<T> keys, IComparer<T> comparer, int i, int j)
    {
        Debug.Assert(i != j);

        if (comparer.Compare(keys[i], keys[j]) > 0)
        {
            (keys[j], keys[i]) = (keys[i], keys[j]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap(Span<T> a, int i, int j)
    {
        Debug.Assert(i != j);

        (a[j], a[i]) = (a[i], a[j]);
    }

    // IntroSort is recursive; block it from being inlined into itself as
    // this is currenly not profitable.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void IntroSort(Span<T> keys, int depthLimit, IComparer<T> comparer)
    {
        Debug.Assert(!keys.IsEmpty);
        Debug.Assert(depthLimit >= 0);
        Debug.Assert(comparer != null);

        int partitionSize = keys.Length;
        while (partitionSize > 1)
        {
            if (partitionSize <= IntrosortSizeThreshold)
            {

                if (partitionSize == 2)
                {
                    SwapIfGreater(keys, comparer, 0, 1);
                    return;
                }

                if (partitionSize == 3)
                {
                    SwapIfGreater(keys, comparer, 0, 1);
                    SwapIfGreater(keys, comparer, 0, 2);
                    SwapIfGreater(keys, comparer, 1, 2);
                    return;
                }

                InsertionSort(keys.Slice(0, partitionSize), comparer);
                return;
            }

            if (depthLimit == 0)
            {
                HeapSort(keys[..partitionSize], comparer);
                return;
            }
            depthLimit--;

            int p = PickPivotAndPartition(keys[..partitionSize], comparer);

            // Note we've already partitioned around the pivot and do not have to move the pivot again.
            IntroSort(keys[(p + 1)..partitionSize], depthLimit, comparer);
            partitionSize = p;
        }
    }

    private static int PickPivotAndPartition(Span<T> keys, IComparer<T> comparer)
    {
        Debug.Assert(keys.Length >= IntrosortSizeThreshold);
        Debug.Assert(comparer != null);

        int hi = keys.Length - 1;

        // Compute median-of-three.  But also partition them, since we've done the comparison.
        int middle = hi >> 1;

        // Sort lo, mid and hi appropriately, then pick mid as the pivot.
        SwapIfGreater(keys, comparer, 0, middle);  // swap the low with the mid point
        SwapIfGreater(keys, comparer, 0, hi);   // swap the low with the high
        SwapIfGreater(keys, comparer, middle, hi); // swap the middle with the high

        T pivot = keys[middle];
        Swap(keys, middle, hi - 1);
        int left = 0, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

        while (left < right)
        {
            while (comparer.Compare(keys[++left], pivot) < 0) ;
            while (comparer.Compare(pivot, keys[--right]) < 0) ;

            if (left >= right)
                break;

            Swap(keys, left, right);
        }

        // Put pivot in the right location.
        if (left != hi - 1)
        {
            Swap(keys, left, hi - 1);
        }
        return left;
    }

    private static void HeapSort(Span<T> keys, IComparer<T> comparer)
    {
        Debug.Assert(comparer != null);
        Debug.Assert(!keys.IsEmpty);

        int n = keys.Length;
        for (int i = n >> 1; i >= 1; i--)
        {
            DownHeap(keys, i, n, comparer);
        }

        for (int i = n; i > 1; i--)
        {
            Swap(keys, 0, i - 1);
            DownHeap(keys, 1, i - 1, comparer);
        }
    }

    private static void DownHeap(Span<T> keys, int i, int n, IComparer<T> comparer)
    {
        Debug.Assert(comparer != null);

        T d = keys[i - 1];
        while (i <= n >> 1)
        {
            int child = 2 * i;
            if (child < n && comparer.Compare(keys[child - 1], keys[child]) < 0)
            {
                child++;
            }

            if (!(comparer.Compare(d, keys[child - 1]) < 0))
                break;

            keys[i - 1] = keys[child - 1];
            i = child;
        }

        keys[i - 1] = d;
    }

    private static void InsertionSort(Span<T> keys, IComparer<T> comparer)
    {
        for (int i = 0; i < keys.Length - 1; i++)
        {
            T t = keys[i + 1];

            int j = i;
            while (j >= 0 && comparer.Compare(t, keys[j]) < 0)
            {
                keys[j + 1] = keys[j];
                j--;
            }

            keys[j + 1] = t;
        }
    }
}