using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tooling;

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
