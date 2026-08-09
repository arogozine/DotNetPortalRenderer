using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tooling;

internal static class VectorExtensionsShared
{
    // Hardware has no integer divide/modulo by a non-constant vector. Int32/UInt32 values are exactly
    // representable as double (53-bit mantissa), so the remainder is computed as left - trunc(left / right) * right
    // in float64, which is exact here because trunc(left/right)*right stays within double's exact-integer range.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<int> ModuloLaneSigned(Vector256<double> leftD, Vector256<double> rightD)
    {
        Vector256<double> quotient = Avx.RoundToZero(Avx.Divide(leftD, rightD));
        Vector256<double> remainder = Avx.Subtract(leftD, Avx.Multiply(quotient, rightD));

        return Avx.ConvertToVector128Int32(remainder);
    }

    // uint32 -> double has no direct AVX instruction. The bits are converted as signed int32 (which yields
    // value - 2^32 whenever the top bit is set), then 2^32 is added back for those lanes to recover the
    // true unsigned magnitude. The remainder (< right <= uint.MaxValue) is converted back the same way in
    // reverse: bias by -2^31 into the exact int32 range, convert, then flip the sign bit (equivalent to
    // +2^31 mod 2^32, since the addend has no other bits set) to land on the final uint32 bit pattern.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<uint> ModuloLaneUnsigned(Vector128<uint> leftPart, Vector256<double> rightD)
    {
        if (Avx512F.VL.IsSupported)
        {
            Vector256<double> correctedLeftD = Avx512F.VL.ConvertToVector256Double(leftPart);

            Vector256<double> quotient = Avx.RoundToZero(Avx.Divide(correctedLeftD, rightD));
            Vector256<double> remainder = Avx.Subtract(correctedLeftD, Avx.Multiply(quotient, rightD));

            return Avx512F.VL.ConvertToVector128UInt32(remainder);
        }
        else
        {
            Vector128<int> SignBit32 = Vector128.Create(unchecked((int)0x80000000));
            Vector256<double> TwoPow31 = Vector256.Create(2147483648.0);
            Vector256<double> TwoPow32 = Vector256.Create(4294967296.0);

            Vector256<double> leftD = Avx.ConvertToVector256Double(leftPart.AsInt32());
            Vector256<double> negativeMask = Avx.CompareLessThan(leftD, Vector256<double>.Zero);
            Vector256<double> correctedLeftD = Avx.Add(leftD, Avx.And(negativeMask, TwoPow32));

            Vector256<double> quotient = Avx.RoundToZero(Avx.Divide(correctedLeftD, rightD));
            Vector256<double> remainder = Avx.Subtract(correctedLeftD, Avx.Multiply(quotient, rightD));

            Vector128<int> shifted = Avx.ConvertToVector128Int32(Avx.Subtract(remainder, TwoPow31));

            return Sse2.Xor(shifted, SignBit32).AsUInt32();
        }

    }

}
