namespace Tooling;

public sealed class QuickArrayPool<T>
{
    private int _totalAllocated = 32;
    private int _maxBucketLength = 32;
    private int _currentBucket = 0;

    private readonly List<ArrayPoolBucket<T>> _buckets = [];

    public QuickArrayPool()
    {
        _ = AddBucket();
    }

    public ArraySegment<T> Request(int size)
    {
        ArrayPoolBucket<T> bucket = _buckets[_currentBucket];

        if (bucket.TryAllocate(size, out ArraySegment<T> segment))
        {
            return segment;
        }

        // allocate another bucket
        _maxBucketLength = Math.Min(_maxBucketLength << 1, size << 1);
        _totalAllocated += _maxBucketLength;
        _currentBucket++;

        bucket = AddBucket();

        bool allocated = bucket.TryAllocate(size, out segment);
        Debug.Assert(allocated);

        return segment;
    }

    public void ClearAndOptimize()
    {
        if (_currentBucket == 0)
        {
            _buckets[0].Clear();
            return;
        }

        // consolidate buckets
        _buckets.Clear();
        _currentBucket = 0;
        _maxBucketLength = _totalAllocated;
        _ = AddBucket();
    }

    private ArrayPoolBucket<T> AddBucket()
    {
        var bucket = new ArrayPoolBucket<T> { Array = new T[_maxBucketLength] };
        _buckets.Add(bucket);

        return bucket;
    }
}