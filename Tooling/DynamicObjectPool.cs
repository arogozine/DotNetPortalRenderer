namespace RenderingEngine.Tooling;

public sealed class DynamicObjectPool<T>
        where T : class, new()
{
    private int _requestedObjectCount = 0;
    private T?[] _objects;
    private readonly Action<T> _reset;
    private readonly Lock _lock = new();

    public DynamicObjectPool(Action<T> reset)
    {
        _objects = new T[32];
        _reset = reset;
    }

    public void Clear() => _objects.AsSpan().Clear();

    public T GetOrCreate()
    {
        lock (_lock)
        {
            int index = ++_requestedObjectCount;

            if (index >= _objects.Length)
            {
                Array.Resize(ref _objects, _objects.Length * 2);
            }

            T? obj = _objects[index];

            if (obj == null)
            {
                obj = new();
                _objects[index] = obj;
            }

            return obj;
        }
    }

    public void Reset()
    {
        _requestedObjectCount = 0;

        for (int i = 0; i < _objects.Length; i++)
        {
            if (_objects[i] is { } obj)
            {
                _reset(obj);
            }
        }
    }
}
