#pragma warning disable SYSLIB5003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace Tooling;

[SkipLocalsInit]
public static unsafe class VectorExtensions
{
    extension(Vector<int>)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> operator %(Vector<int> left, int right)
        {
            Vector<int> resultV = default;
            int* result = (int*)Unsafe.AsPointer(ref resultV);

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                result[i] = left[i] % right;
            }

            return resultV;
        }
    }

    extension(Vector<uint>)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> operator %(Vector<uint> left, uint right)
        {
            Vector<uint> resultV = default;
            uint* result = (uint*)Unsafe.AsPointer(ref resultV);

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                result[i] = left[i] % right;
            }

            return resultV;
        }
    }

    extension<T>(Vector)
        where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MaskStore(T* address, Vector<int> mask, Vector<T> source)
        {
            // TODO: ARM equivalent ?

            if (Vector<T>.Count == Vector512<T>.Count)
            {
                Vector512<T> indexV = source.AsVector512();
                Vector512<int> maskV = mask.AsVector512();

                Vector256.MaskStore(address, maskV.GetLower(), indexV.GetLower());
                Vector256.MaskStore(address + Vector256<T>.Count, maskV.GetUpper(), indexV.GetUpper());

                return;
            }

            if (Vector<T>.Count == Vector256<T>.Count)
            {
                Vector256.MaskStore(address, mask.AsVector256(), source.AsVector256());
                return;
            }

            if (Vector<T>.Count == Vector128<T>.Count)
            {
                Vector128.MaskStore(address, mask.AsVector128(), source.AsVector128());
                return;
            }

            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (mask[i] == 0)
                {
                    continue;
                }

                address[i] = source[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Gather(T* baseAddress, Vector<int> index)
        {
            if (Sve.IsSupported && sizeof(T) == sizeof(int))
            {
                return Sve.GatherVector(Vector<int>.AllBitsSet, (int*)baseAddress, index)
                    .As<int, T>();
            }

            if (Vector<T>.Count == Vector512<T>.Count)
            {
                Vector512<int> indexV = index.AsVector512();

                Vector256<T> low = Vector256.Gather(baseAddress, indexV.GetLower());
                Vector256<T> high = Vector256.Gather(baseAddress, indexV.GetUpper());

                return Vector512.Create(low, high).AsVector();
            }

            if (Vector<T>.Count == Vector256<T>.Count)
            {
                return Vector256.Gather(baseAddress, index.AsVector256()).AsVector();
            }

            if (Vector<T>.Count == Vector128<T>.Count)
            {
                return Vector128.Gather(baseAddress, index.AsVector128()).AsVector();
            }

            Vector<T> gatherV = default;
            T* gather = (T*)Unsafe.AsPointer(ref gatherV);

            for (int i = 0; i < Vector<T>.Count; i++)
            {
                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return gatherV;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> GatherMask(T* baseAddress, Vector<int> index, Vector<uint> mask)
        {
            if (Sve.IsSupported && sizeof(T) == sizeof(int))
            {
                return Sve.GatherVector(mask.As<uint, int>(), (int*)baseAddress, index)
                    .As<int, T>();
            }

            if (Vector<T>.Count == Vector512<T>.Count)
            {
                Vector512<int> indexV = index.AsVector512();
                Vector512<uint> maskV = mask.AsVector512();

                Vector256<T> low = Vector256.GatherMask(baseAddress, indexV.GetLower(), maskV.GetLower().As<uint, int>());
                Vector256<T> high = Vector256.GatherMask(baseAddress, indexV.GetUpper(), maskV.GetUpper().As<uint, int>());

                return Vector512.Create(low, high).AsVector();
            }

            if (Vector<T>.Count == Vector256<T>.Count)
            {
                return Vector256.GatherMask(baseAddress, index.AsVector256(), mask.AsVector256().As<uint, int>()).AsVector();
            }

            if (Vector<T>.Count == Vector128<T>.Count)
            {
                return Vector128.GatherMask(baseAddress, index.AsVector128(), mask.AsVector128().As<uint, int>()).AsVector();
            }

            Vector<T> gatherV = default;
            T* gather = (T*)Unsafe.AsPointer(ref gatherV);

            for (int i = 0; i < Vector<T>.Count; i++)
            {
                T value = *(baseAddress + index[i]);

                if (mask[i] == 0)
                    continue;

                gather[i] = value;
            }

            return gatherV;
        }
    }
}
