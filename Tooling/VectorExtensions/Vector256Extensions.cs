using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tooling;

[SkipLocalsInit]
public static unsafe class Vector256Extensions
{
    extension<T>(Vector256)
        where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MaskStore(T* address, Vector256<int> mask, Vector256<T> source)
        {
            if (sizeof(T) != sizeof(int))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                Avx2.MaskStore((int*)address, mask, source.As<T, int>());
                return;
            }

            for (int i = 0; i < Vector256<T>.Count; i++)
            {
                if (mask[i] == 0)
                {
                    continue;
                }

                T textureIndex = source[i];
                address[i] = source[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Gather(T* baseAddress, Vector256<int> index)
        {
            if (typeof(T) != typeof(int) && typeof(T) != typeof(uint) && typeof(T) != typeof(float))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                return typeof(T) == typeof(float)
                    ? Avx2.GatherVector256((float*)baseAddress, index, scale: sizeof(float)).As<float, T>()
                    : Avx2.GatherVector256((int*)baseAddress, index, scale: sizeof(int)).As<int, T>();
            }

            if (Sse41.IsSupported)
            {
                Vector128<int> lower = Vector128<int>.Zero;
                Vector128<int> upper = Vector128<int>.Zero;

                int* baseAddressI = (int*)baseAddress;

                lower = Sse41.Insert(lower, *(baseAddressI + index[0]), 0);
                lower = Sse41.Insert(lower, *(baseAddressI + index[1]), 1);
                lower = Sse41.Insert(lower, *(baseAddressI + index[2]), 2);
                lower = Sse41.Insert(lower, *(baseAddressI + index[3]), 3);

                upper = Sse41.Insert(upper, *(baseAddressI + index[4]), 0);
                upper = Sse41.Insert(upper, *(baseAddressI + index[5]), 1);
                upper = Sse41.Insert(upper, *(baseAddressI + index[6]), 2);
                upper = Sse41.Insert(upper, *(baseAddressI + index[7]), 3);

                return Vector256.Create(lower, upper).As<int, T>();
            }

            Vector256<T> gatherV = default;
            T* gather = (T*)Unsafe.AsPointer(ref gatherV);

            for (int i = 0; i < Vector256<int>.Count; i++)
            {
                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return gatherV;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Gather(T* baseAddress, Vector128<int> index)
        {
            if (typeof(T) != typeof(long) && typeof(T) != typeof(ulong) && typeof(T) != typeof(double))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                return typeof(T) == typeof(double)
                    ? Avx2.GatherVector256((double*)baseAddress, index, scale: sizeof(double)).As<double, T>()
                    : Avx2.GatherVector256((long*)baseAddress, index, scale: sizeof(long)).As<long, T>();
            }

            Vector256<T> gatherV = default;
            T* gather = (T*)Unsafe.AsPointer(ref gatherV);

            for (int i = 0; i < Vector256<T>.Count; i++)
            {
                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return gatherV;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> GatherMask(T* baseAddress, Vector256<int> index, Vector256<int> mask)
        {
            if (typeof(T) != typeof(int) && typeof(T) != typeof(uint) && typeof(T) != typeof(float))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                return typeof(T) == typeof(float)
                    ? Avx2.GatherMaskVector256(Vector256<float>.Zero, (float*)baseAddress, index, mask.As<int, float>(), scale: sizeof(float)).As<float, T>()
                    : Avx2.GatherMaskVector256(Vector256<int>.Zero, (int*)baseAddress, index, mask, scale: sizeof(int)).As<int, T>();
            }

            Vector256<T> gatherV = default;

            T* gather = (T*)Unsafe.AsPointer(in gatherV);

            for (int i = 0; i < Vector256<T>.Count; i++)
            {
                if (mask[i] == 0)
                    continue;

                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return gatherV;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> GatherMask(in Vector256<T> source, T* baseAddress, Vector256<int> index, Vector256<int> mask)
        {
            if (typeof(T) != typeof(int) && typeof(T) != typeof(uint) && typeof(T) != typeof(float))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                return typeof(T) == typeof(float)
                    ? Avx2.GatherMaskVector256(source.As<T, float>(), (float*)baseAddress, index, mask.As<int, float>(), scale: sizeof(float)).As<float, T>()
                    : Avx2.GatherMaskVector256(source.As<T, int>(), (int*)baseAddress, index, mask, scale: sizeof(int)).As<int, T>();
            }

            T* gather = (T*)Unsafe.AsPointer(in source);

            for (int i = 0; i < Vector256<T>.Count; i++)
            {
                if (mask[i] == 0)
                    continue;

                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return source;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> GatherMask(T* baseAddress, Vector128<int> index, Vector256<long> mask)
        {
            if (typeof(T) != typeof(long) && typeof(T) != typeof(ulong) && typeof(T) != typeof(double))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                return typeof(T) == typeof(double)
                    ? Avx2.GatherMaskVector256(Vector256<double>.Zero, (double*)baseAddress, index, mask.As<long, double>(), scale: sizeof(double)).As<double, T>()
                    : Avx2.GatherMaskVector256(Vector256<long>.Zero, (long*)baseAddress, index, mask, scale: sizeof(long)).As<long, T>();
            }

            Vector256<T> gatherV = default;

            T* gather = (T*)Unsafe.AsPointer(ref gatherV);

            for (int i = 0; i < Vector256<T>.Count; i++)
            {
                if (mask[i] == 0)
                    continue;

                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return gatherV;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> GatherMask(in Vector256<T> source, T* baseAddress, Vector128<int> index, Vector256<long> mask)
        {
            if (typeof(T) != typeof(long) && typeof(T) != typeof(ulong) && typeof(T) != typeof(double))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                return typeof(T) == typeof(double)
                    ? Avx2.GatherMaskVector256(source.As<T, double>(), (double*)baseAddress, index, mask.As<long, double>(), scale: sizeof(double)).As<double, T>()
                    : Avx2.GatherMaskVector256(source.As<T, long>(), (long*)baseAddress, index, mask, scale: sizeof(long)).As<long, T>();
            }

            T* gather = (T*)Unsafe.AsPointer(in source);

            for (int i = 0; i < Vector256<T>.Count; i++)
            {
                if (mask[i] == 0)
                    continue;

                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return source;
        }
    }
}
