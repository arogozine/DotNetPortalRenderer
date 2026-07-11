namespace RenderingEngine.Tooling;

public sealed class SimpleObjectPool<T>
    where T : class, new()
{
    private readonly T?[] _objects;
    private readonly Action<T> _reset;

    public SimpleObjectPool(int count, Action<T> reset)
    {
        _objects = new T[count];
        _reset = reset;
    }

    public T GetOrCreate(int index)
    {
        T? obj = _objects[index];

        if (obj == null)
        {
            obj = new();
            _objects[index] = obj;
        }

        return obj;
    }

    public void Reset()
    {
        for (int i = 0; i < _objects.Length; i++)
        {
            if (_objects[i] is { } obj)
            {
                _reset(obj);
            }
        }
    }
}
