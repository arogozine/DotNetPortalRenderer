using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tooling;

[SkipLocalsInit]
public static unsafe class Vector128Extensions
{
    extension(Vector128<int>)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<int> operator % (Vector128<int> left, int right)
        {
            if (Avx.IsSupported)
            {
                if (right == 0)
                {
                    throw new DivideByZeroException();
                }

                // CLR div/rem traps on int.MinValue / -1 regardless of checked/unchecked context; the
                // float64 path below has no such trap (it would silently return the mathematically
                // correct 0), so this is special-cased to keep behavior identical to the scalar fallback.
                if (right == -1 && Vector128.EqualsAny(left, Vector128.Create(int.MinValue)))
                {
                    throw new OverflowException();
                }

                Vector256<double> rightD = Vector256.Create((double)right);

                return VectorExtensionsShared.ModuloLaneSigned(Avx.ConvertToVector256Double(left), rightD);
            }

            if (Sse41.IsSupported)
            {
                Vector128<int> result = Vector128<int>.Zero;

                result = Sse41.Insert(result, left[0] % right, 0);
                result = Sse41.Insert(result, left[1] % right, 1);
                result = Sse41.Insert(result, left[2] % right, 2);
                result = Sse41.Insert(result, left[3] % right, 3);

                return result;
            }

            return Vector128.Create(
                left[0] % right,
                left[1] % right,
                left[2] % right,
                left[3] % right
            );
        }
    }

    extension(Vector128<uint>)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> operator %(Vector128<uint> left, uint right)
        {
            if (Avx.IsSupported)
            {
                if (right == 0)
                {
                    throw new DivideByZeroException();
                }

                Vector256<double> rightD = Vector256.Create((double)right);
                return VectorExtensionsShared.ModuloLaneUnsigned(left, rightD);
            }

            if (Sse41.IsSupported)
            {
                Vector128<uint> result = Vector128<uint>.Zero;

                result = Sse41.Insert(result, left[0] % right, 0);
                result = Sse41.Insert(result, left[1] % right, 1);
                result = Sse41.Insert(result, left[2] % right, 2);
                result = Sse41.Insert(result, left[3] % right, 3);

                return result;
            }

            return Vector128.Create(
                left[0] % right,
                left[1] % right,
                left[2] % right,
                left[3] % right
            );
        }
    }

    extension<T>(Vector128)
        where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MaskStore(T* address, Vector128<int> mask, Vector128<T> source)
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

            for (int i = 0; i < Vector128<T>.Count; i++)
            {
                if (mask[i] == 0)
                {
                    continue;
                }

                address[i] = source[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Gather(T* baseAddress, Vector128<int> index)
        {
            if (typeof(T) != typeof(int) && typeof(T) != typeof(uint) && typeof(T) != typeof(float) &&
                typeof(T) != typeof(long) && typeof(T) != typeof(ulong) && typeof(T) != typeof(double))
            {
                throw new NotSupportedException();
            }

            if (sizeof(T) == sizeof(long))
            {
                if (Avx2.IsSupported)
                {
                    return typeof(T) == typeof(double)
                        ? Avx2.GatherVector128((double*)baseAddress, index, scale: sizeof(double)).As<double, T>()
                        : Avx2.GatherVector128((long*)baseAddress, index, scale: sizeof(long)).As<long, T>();
                }
            }
            else
            {
                if (Avx2.IsSupported)
                {
                    return typeof(T) == typeof(float)
                        ? Avx2.GatherVector128((float*)baseAddress, index, scale: sizeof(float)).As<float, T>()
                        : Avx2.GatherVector128((int*)baseAddress, index, scale: sizeof(int)).As<int, T>();
                }

                if (Sse41.IsSupported)
                {
                    Vector128<int> result = Vector128<int>.Zero;
                    int* baseAddressI = (int*)baseAddress;

                    result = Sse41.Insert(result, *(baseAddressI + index[0]), 0);
                    result = Sse41.Insert(result, *(baseAddressI + index[1]), 1);
                    result = Sse41.Insert(result, *(baseAddressI + index[2]), 2);
                    result = Sse41.Insert(result, *(baseAddressI + index[3]), 3);

                    return result.As<int, T>();
                }
            }

            Vector128<T> gatherV = default;
            T* gather = (T*)Unsafe.AsPointer(ref gatherV);

            for (int i = 0; i < Vector128<T>.Count; i++)
            {
                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return gatherV;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> GatherMask(T* baseAddress, Vector128<int> index, Vector128<int> mask)
        {
            if (typeof(T) != typeof(int) && typeof(T) != typeof(uint) && typeof(T) != typeof(float))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                return typeof(T) == typeof(float)
                    ? Avx2.GatherMaskVector128(Vector128<float>.Zero, (float*)baseAddress, index, mask.As<int, float>(), scale: sizeof(float)).As<float, T>()
                    : Avx2.GatherMaskVector128(Vector128<int>.Zero, (int*)baseAddress, index, mask, scale: sizeof(int)).As<int, T>();
            }

            if (Sse41.IsSupported)
            {
                Vector128<int> result = Vector128<int>.Zero;
                int* baseAddressI = (int*)baseAddress;

                if (mask[0] != 0) result = Sse41.Insert(result, *(baseAddressI + index[0]), 0);
                if (mask[1] != 0) result = Sse41.Insert(result, *(baseAddressI + index[1]), 1);
                if (mask[2] != 0) result = Sse41.Insert(result, *(baseAddressI + index[2]), 2);
                if (mask[3] != 0) result = Sse41.Insert(result, *(baseAddressI + index[3]), 3);

                return result.As<int, T>();
            }

            Vector128<T> gatherV = default;

            T* gather = (T*)Unsafe.AsPointer(in gatherV);

            for (int i = 0; i < Vector128<T>.Count; i++)
            {
                if (mask[i] == 0)
                    continue;

                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return gatherV;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> GatherMask(in Vector128<T> source, T* baseAddress, Vector128<int> index, Vector128<int> mask)
        {
            if (typeof(T) != typeof(int) && typeof(T) != typeof(uint) && typeof(T) != typeof(float))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                return typeof(T) == typeof(float)
                    ? Avx2.GatherMaskVector128(source.As<T, float>(), (float*)baseAddress, index, mask.As<int, float>(), scale: sizeof(float)).As<float, T>()
                    : Avx2.GatherMaskVector128(source.As<T, int>(), (int*)baseAddress, index, mask, scale: sizeof(int)).As<int, T>();
            }

            if (Sse41.IsSupported)
            {
                Vector128<int> result = source.As<T, int>();
                int* baseAddressI = (int*)baseAddress;

                if (mask[0] != 0) result = Sse41.Insert(result, *(baseAddressI + index[0]), 0);
                if (mask[1] != 0) result = Sse41.Insert(result, *(baseAddressI + index[1]), 1);
                if (mask[2] != 0) result = Sse41.Insert(result, *(baseAddressI + index[2]), 2);
                if (mask[3] != 0) result = Sse41.Insert(result, *(baseAddressI + index[3]), 3);

                return result.As<int, T>();
            }

            // TODO: Sve Gather on ARM

            T* gather = (T*)Unsafe.AsPointer(in source);

            for (int i = 0; i < Vector128<T>.Count; i++)
            {
                if (mask[i] == 0)
                    continue;

                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return source;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> GatherMask(T* baseAddress, Vector128<int> index, Vector128<long> mask)
        {
            if (typeof(T) != typeof(long) && typeof(T) != typeof(ulong) && typeof(T) != typeof(double))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                return typeof(T) == typeof(double)
                    ? Avx2.GatherMaskVector128(Vector128<double>.Zero, (double*)baseAddress, index, mask.As<long, double>(), scale: sizeof(double)).As<double, T>()
                    : Avx2.GatherMaskVector128(Vector128<long>.Zero, (long*)baseAddress, index, mask, scale: sizeof(long)).As<long, T>();
            }

            Vector128<T> gatherV = default;

            T* gather = (T*)Unsafe.AsPointer(ref gatherV);

            for (int i = 0; i < Vector128<T>.Count; i++)
            {
                if (mask[i] == 0)
                    continue;

                T value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return gatherV;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> GatherMask(in Vector128<T> source, T* baseAddress, Vector128<int> index, Vector128<long> mask)
        {
            if (typeof(T) != typeof(long) && typeof(T) != typeof(ulong) && typeof(T) != typeof(double))
            {
                throw new NotSupportedException();
            }

            if (Avx2.IsSupported)
            {
                return typeof(T) == typeof(double)
                    ? Avx2.GatherMaskVector128(source.As<T, double>(), (double*)baseAddress, index, mask.As<long, double>(), scale: sizeof(double)).As<double, T>()
                    : Avx2.GatherMaskVector128(source.As<T, long>(), (long*)baseAddress, index, mask, scale: sizeof(long)).As<long, T>();
            }

            T* gather = (T*)Unsafe.AsPointer(in source);

            for (int i = 0; i < Vector128<T>.Count; i++)
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
