using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tooling;

[SkipLocalsInit]
public static unsafe class VectorExtensions
{
    extension(Vector<uint>)
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> Gather(uint* baseAddress, Vector<int> index)
        {
            if (Avx2.IsSupported)
            {
                if (Vector<uint>.Count == Vector512<uint>.Count)
                {
                    Vector512<int> indexV = index.AsVector512();

                    Vector256<uint> low = Avx2.GatherVector256(baseAddress, indexV.GetLower(), scale: sizeof(uint));
                    Vector256<uint> high = Avx2.GatherVector256(baseAddress, indexV.GetUpper(), scale: sizeof(uint));

                    return Vector512.Create(low, high).AsVector();
                }

                if (Vector<uint>.Count == Vector256<uint>.Count)
                {
                    return Avx2.GatherVector256(baseAddress, index.AsVector256(), scale: sizeof(uint)).AsVector();
                }

                if (Vector<uint>.Count == Vector128<uint>.Count)
                {
                    return Avx2.GatherVector128(baseAddress, index.AsVector128(), scale: sizeof(uint)).AsVector();
                }
            }

            scoped Span<uint> gather = stackalloc uint[Vector<uint>.Count];

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                uint value = *(baseAddress + index[i]);
                gather[i] = value;
            }

            return Vector.Create(gather);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> GatherMask(uint* baseAddress, Vector<int> index, Vector<uint> mask)
        {
            if (Avx2.IsSupported)
            {
                if (Vector<uint>.Count == Vector512<uint>.Count)
                {
                    Vector512<int> indexV = index.AsVector512();
                    Vector512<uint> maskV = mask.AsVector512();

                    Vector256<uint> low = Avx2.GatherMaskVector256(Vector256<uint>.Zero, baseAddress, indexV.GetLower(), maskV.GetLower(), scale: sizeof(uint));
                    Vector256<uint> high = Avx2.GatherMaskVector256(Vector256<uint>.Zero, baseAddress, indexV.GetUpper(), maskV.GetUpper(), scale: sizeof(uint));

                    return Vector512.Create(low, high).AsVector();
                }

                if (Vector<uint>.Count == Vector256<uint>.Count)
                {
                    return Avx2.GatherMaskVector256(Vector256<uint>.Zero, baseAddress, index.AsVector256(), mask.AsVector256(), scale: sizeof(uint)).AsVector();
                }

                if (Vector<uint>.Count == Vector128<uint>.Count)
                {
                    return Avx2.GatherMaskVector128(Vector128<uint>.Zero, baseAddress, index.AsVector128(), mask.AsVector128(), scale: sizeof(uint)).AsVector();
                }
            }

            scoped Span<uint> gather = stackalloc uint[Vector<uint>.Count];
            gather.Clear();

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
