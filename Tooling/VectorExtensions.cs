using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tooling;

public readonly struct AlignedMemory : IMemoryAlignment
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector<T> Load<T>(T* ptr) where T : unmanaged
    {
        return Vector.LoadAligned(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector64<T> Load64<T>(T* ptr) where T : unmanaged
    {
        return Vector64.LoadAligned(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static unsafe Vector128<T> Load128<T>(T* ptr) where T : unmanaged
    {
        return Vector128.LoadAligned(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<T> Load256<T>(T* ptr) where T : unmanaged
    {
        return Vector256.LoadAligned(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Store<T>(T* ptr, Vector<T> vector) where T : unmanaged
    {
        vector.StoreAligned(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Store<T>(T* ptr, Vector64<T> vector) where T : unmanaged
    {
        vector.StoreAligned(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Store<T>(T* ptr, Vector128<T> vector) where T : unmanaged
    {
        vector.StoreAligned(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Store<T>(T* ptr, Vector256<T> vector) where T : unmanaged
    {
        vector.StoreAligned(ptr);
    }
}

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

public static unsafe class VectorExtensions
{
    extension(Vector<uint> vector)
    {
        // Vector512 gather doesn't exist

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> GatherVector(uint* baseAddress, Vector<int> index)
        {
            if (Avx2.IsSupported)
            {
                if (Vector<uint>.Count == Vector256<uint>.Count)
                {
                    return Avx2.GatherVector256(baseAddress, index.AsVector256(), scale: sizeof(uint)).AsVector();
                }

                if (Vector<uint>.Count == Vector128<uint>.Count)
                {
                    return Avx2.GatherVector128(baseAddress, index.AsVector128(), scale: sizeof(uint)).AsVector();
                }
            }

            Span<uint> gather = stackalloc uint[Vector<uint>.Count];

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                uint value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return Vector.Create(gather);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> GatherMaskVector(uint* baseAddress, Vector<int> index, Vector<uint> mask)
        {
            if (Avx2.IsSupported)
            {
                // _mm256_undefined_ps doesn't exist
                if (Vector<uint>.Count == Vector256<uint>.Count)
                {
                    return Avx2.GatherMaskVector256(Vector256<uint>.Zero, baseAddress, index.AsVector256(), mask.AsVector256(), scale: sizeof(uint)).AsVector();
                }

                if (Vector<uint>.Count == Vector128<uint>.Count)
                {
                    return Avx2.GatherMaskVector128(Vector128<uint>.Zero, baseAddress, index.AsVector128(), mask.AsVector128(), scale: sizeof(uint)).AsVector();
                }
            }

            Span<uint> gather = stackalloc uint[Vector<uint>.Count];

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                if (mask[i] == 0)
                    continue;

                uint value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return Vector.Create(gather);
        }
    }
}
