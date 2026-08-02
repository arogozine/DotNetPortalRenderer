using System.Numerics;
using System.Runtime.Intrinsics;

namespace Tooling;

public readonly struct UnalignedMemory : IMemoryAlignment
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector<T> Load<T>(T* ptr) where T : unmanaged
    {
        return Vector.Load(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector64<T> Load64<T>(T* ptr) where T : unmanaged
    {
        return Vector64.Load(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static unsafe Vector128<T> Load128<T>(T* ptr) where T : unmanaged
    {
        return Vector128.Load(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<T> Load256<T>(T* ptr) where T : unmanaged
    {
        return Vector256.Load(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Store<T>(T* ptr, Vector<T> vector) where T : unmanaged
    {
        vector.Store(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Store<T>(T* ptr, Vector64<T> vector) where T : unmanaged
    {
        vector.Store(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Store<T>(T* ptr, Vector128<T> vector) where T : unmanaged
    {
        vector.Store(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Store<T>(T* ptr, Vector256<T> vector) where T : unmanaged
    {
        vector.Store(ptr);
    }
}
