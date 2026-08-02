using System.Numerics;
using System.Runtime.Intrinsics;

namespace Tooling;

public unsafe interface IMemoryAlignment
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static abstract void Store<T>(T* ptr, Vector<T> vector)
        where T : unmanaged;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static abstract void Store<T>(T* ptr, Vector64<T> vector)
        where T : unmanaged;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static abstract void Store<T>(T* ptr, Vector128<T> vector)
        where T : unmanaged;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static abstract void Store<T>(T* ptr, Vector256<T> vector)
        where T : unmanaged;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static abstract Vector<T> Load<T>(T* ptr)
        where T : unmanaged;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static abstract Vector64<T> Load64<T>(T* ptr)
        where T : unmanaged;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static abstract Vector128<T> Load128<T>(T* ptr)
        where T : unmanaged;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static abstract Vector256<T> Load256<T>(T* ptr)
        where T : unmanaged;
}
