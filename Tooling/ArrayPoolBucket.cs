
using System.Diagnostics.CodeAnalysis;

namespace Tooling;

[SkipLocalsInit]
internal sealed class ArrayPoolBucket<T>
{
    public required T[] Array { get; init; }
    public int Offset { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAllocate(int size, [NotNullWhen(true)] out ArraySegment<T> segment)
    {
        if (Offset + size <= Array.Length)
        {
            segment = new ArraySegment<T>(Array, Offset, size);
            Offset += size;
            return true;
        }

        segment = default;
        return false;
    }

    public void Clear()
    {
        Array.AsSpan()[..Offset].Clear();
        Offset = 0;
    }
}
