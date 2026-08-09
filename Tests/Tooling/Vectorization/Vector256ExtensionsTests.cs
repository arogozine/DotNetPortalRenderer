using System.Runtime.Intrinsics;
using Tooling;

namespace Tests;

// Behavior must be identical across the Avx2, Sse41-fallback, and scalar-fallback code paths.
// Run with: normal, DOTNET_EnableAVX2=0 (forces Sse41 fallback), and DOTNET_EnableHWIntrinsic=0 (forces scalar fallback).
public unsafe class Vector256ExtensionsTests
{
    [Fact]
    public void Gather_Int32Family_ReturnsValuesAtEachIndex()
    {
        int count = Vector256<uint>.Count;
        uint[] source = new uint[count * 2];
        for (int i = 0; i < source.Length; i++) source[i] = (uint)(i * 10 + 1);

        Vector256<int> index = Vector256.Create(14, 12, 10, 8, 6, 4, 2, 0);

        fixed (uint* basePtr = source)
        {
            Vector256<uint> gathered = Vector256.Gather(basePtr, index);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(source[index[i]], gathered[i]);
            }
        }
    }

    [Fact]
    public void Gather_Float_ReturnsValuesAtEachIndex()
    {
        int count = Vector256<float>.Count;
        float[] source = new float[count * 2];
        for (int i = 0; i < source.Length; i++) source[i] = i * 1.5f;

        Vector256<int> index = Vector256.Create(15, 13, 11, 9, 7, 5, 3, 1);

        fixed (float* basePtr = source)
        {
            Vector256<float> gathered = Vector256.Gather(basePtr, index);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(source[index[i]], gathered[i]);
            }
        }
    }

    [Fact]
    public void Gather_Int64Family_ReturnsValuesAtEachIndex()
    {
        int count = Vector256<long>.Count;
        long[] source = new long[count * 2];
        for (int i = 0; i < source.Length; i++) source[i] = i * 100 + 1;

        Vector128<int> index = Vector128.Create(6, 4, 2, 0);

        fixed (long* basePtr = source)
        {
            Vector256<long> gathered = Vector256.Gather(basePtr, index);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(source[index[i]], gathered[i]);
            }
        }
    }

    [Fact]
    public void Gather_Double_ReturnsValuesAtEachIndex()
    {
        int count = Vector256<double>.Count;
        double[] source = new double[count * 2];
        for (int i = 0; i < source.Length; i++) source[i] = i * 2.5;

        Vector128<int> index = Vector128.Create(7, 5, 3, 1);

        fixed (double* basePtr = source)
        {
            Vector256<double> gathered = Vector256.Gather(basePtr, index);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(source[index[i]], gathered[i]);
            }
        }
    }

    [Fact]
    public void Gather_UnsupportedType_ThrowsNotSupportedException()
    {
        byte[] source = new byte[32];

        fixed (byte* basePtr = source)
        {
            byte* p = basePtr;
            Assert.Throws<NotSupportedException>(() => Vector256.Gather(p, Vector256<int>.Zero));
        }
    }

    [Fact]
    public void GatherMask_Int32Family_ReturnsValuesForSetLanesAndZeroForMaskedLanes()
    {
        int count = Vector256<uint>.Count;
        uint[] source = new uint[count];
        for (int i = 0; i < count; i++) source[i] = (uint)(i + 1);

        Vector256<int> index = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7);
        Vector256<int> mask = Vector256.Create(-1, 0, -1, 0, -1, 0, -1, 0);

        fixed (uint* basePtr = source)
        {
            Vector256<uint> gathered = Vector256.GatherMask(basePtr, index, mask);

            for (int i = 0; i < count; i++)
            {
                uint expected = mask[i] != 0 ? source[index[i]] : 0u;
                Assert.Equal(expected, gathered[i]);
            }
        }
    }

    [Fact]
    public void GatherMask_WithSource_Int32Family_KeepsSourceForMaskedLanes()
    {
        int count = Vector256<uint>.Count;
        uint[] source = new uint[count];
        for (int i = 0; i < count; i++) source[i] = (uint)(i + 1);

        Vector256<int> index = Vector256.Create(7, 6, 5, 4, 3, 2, 1, 0);
        Vector256<int> mask = Vector256.Create(-1, 0, -1, 0, -1, 0, -1, 0);
        Vector256<uint> sourceVector = Vector256.Create(100u, 200u, 300u, 400u, 500u, 600u, 700u, 800u);

        fixed (uint* basePtr = source)
        {
            Vector256<uint> gathered = Vector256.GatherMask(sourceVector, basePtr, index, mask);

            for (int i = 0; i < count; i++)
            {
                uint expected = mask[i] != 0 ? source[index[i]] : sourceVector[i];
                Assert.Equal(expected, gathered[i]);
            }
        }
    }

    [Fact]
    public void GatherMask_Int64Family_ReturnsValuesForSetLanesAndZeroForMaskedLanes()
    {
        int count = Vector256<long>.Count;
        long[] source = new long[count];
        for (int i = 0; i < count; i++) source[i] = (i + 1) * 10;

        Vector128<int> index = Vector128.Create(3, 2, 1, 0);
        Vector256<long> mask = Vector256.Create(-1L, 0L, -1L, 0L);

        fixed (long* basePtr = source)
        {
            Vector256<long> gathered = Vector256.GatherMask(basePtr, index, mask);

            for (int i = 0; i < count; i++)
            {
                long expected = mask[i] != 0 ? source[index[i]] : 0L;
                Assert.Equal(expected, gathered[i]);
            }
        }
    }

    [Fact]
    public void GatherMask_WithSource_Int64Family_KeepsSourceForMaskedLanes()
    {
        int count = Vector256<long>.Count;
        long[] source = new long[count];
        for (int i = 0; i < count; i++) source[i] = (i + 1) * 10;

        Vector128<int> index = Vector128.Create(3, 2, 1, 0);
        Vector256<long> mask = Vector256.Create(0L, -1L, 0L, -1L);
        Vector256<long> sourceVector = Vector256.Create(500L, 600L, 700L, 800L);

        fixed (long* basePtr = source)
        {
            Vector256<long> gathered = Vector256.GatherMask(sourceVector, basePtr, index, mask);

            for (int i = 0; i < count; i++)
            {
                long expected = mask[i] != 0 ? source[index[i]] : sourceVector[i];
                Assert.Equal(expected, gathered[i]);
            }
        }
    }

    [Fact]
    public void GatherMask_AllLanesMasked_ReturnsZeroVector()
    {
        int count = Vector256<uint>.Count;
        uint[] source = new uint[count];
        for (int i = 0; i < count; i++) source[i] = (uint)(i + 1);

        Vector256<int> index = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7);

        fixed (uint* basePtr = source)
        {
            Vector256<uint> gathered = Vector256.GatherMask(basePtr, index, Vector256<int>.Zero);

            Assert.Equal(Vector256<uint>.Zero, gathered);
        }
    }

    [Fact]
    public void OperatorModulo_Int_ReturnsPerLaneRemainder()
    {
        int count = Vector256<int>.Count;
        int[] values = new int[count];
        for (int i = 0; i < count; i++) values[i] = i * 7 + 3;
        Vector256<int> left = Vector256.Create(values);
        const int right = 5;

        Vector256<int> result = left % right;

        for (int i = 0; i < count; i++)
        {
            Assert.Equal(values[i] % right, result[i]);
        }
    }

    [Fact]
    public void OperatorModulo_UInt_ReturnsPerLaneRemainder()
    {
        int count = Vector256<uint>.Count;
        uint[] values = new uint[count];
        for (int i = 0; i < count; i++) values[i] = (uint)(i * 7 + 3);
        Vector256<uint> left = Vector256.Create(values);
        const uint right = 5u;

        Vector256<uint> result = left % right;

        for (int i = 0; i < count; i++)
        {
            Assert.Equal(values[i] % right, result[i]);
        }
    }

    [Fact]
    public void OperatorModulo_Int_NegativeDividendsTruncateTowardZero()
    {
        int count = Vector256<int>.Count;
        int[] values = new int[count];
        for (int i = 0; i < count; i++) values[i] = -(i * 7 + 3);
        Vector256<int> left = Vector256.Create(values);
        const int right = 5;

        Vector256<int> result = left % right;

        for (int i = 0; i < count; i++)
        {
            Assert.Equal(values[i] % right, result[i]);
        }
    }

    [Fact]
    public void OperatorModulo_Int_NegativeDivisor_MatchesScalarBehavior()
    {
        int count = Vector256<int>.Count;
        int[] values = new int[count];
        for (int i = 0; i < count; i++) values[i] = i % 2 == 0 ? i * 11 + 4 : -(i * 11 + 4);
        Vector256<int> left = Vector256.Create(values);
        const int right = -6;

        Vector256<int> result = left % right;

        for (int i = 0; i < count; i++)
        {
            Assert.Equal(values[i] % right, result[i]);
        }
    }

    [Fact]
    public void OperatorModulo_Int_DivisorOfOne_ReturnsZero()
    {
        Vector256<int> left = Vector256.Create(int.MinValue, int.MaxValue, -1, 0, 1, 12345, -54321, 7);

        Vector256<int> result = left % 1;

        Assert.Equal(Vector256<int>.Zero, result);
    }

    [Fact]
    public void OperatorModulo_Int_MinAndMaxValueBoundaries_MatchesScalarBehavior()
    {
        Vector256<int> left = Vector256.Create(int.MinValue, int.MinValue + 1, int.MaxValue, int.MaxValue - 1, -1, 1, 0, int.MinValue);
        const int right = 7;

        Vector256<int> result = left % right;

        for (int i = 0; i < Vector256<int>.Count; i++)
        {
            Assert.Equal(left[i] % right, result[i]);
        }
    }

    [Fact]
    public void OperatorModulo_Int_DivideByNegativeOne_ThrowsOverflowExceptionForMinValue()
    {
        // Matches scalar `int.MinValue % -1`, which throws OverflowException (CLR div/rem quirk)
        // regardless of checked/unchecked context.
        Vector256<int> left = Vector256.Create(int.MinValue, -1, 1, 0, 100, -100, int.MaxValue, 42);

        Assert.Throws<OverflowException>(() => left % -1);
    }

    [Fact]
    public void OperatorModulo_Int_DivideByNegativeOne_WithoutMinValue_ReturnsZero()
    {
        Vector256<int> left = Vector256.Create(-1, 1, 0, 100, -100, int.MaxValue, 42, int.MinValue + 1);

        Vector256<int> result = left % -1;

        Assert.Equal(Vector256<int>.Zero, result);
    }

    [Fact]
    public void OperatorModulo_Int_DivideByZero_Throws()
    {
        Vector256<int> left = Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8);

        Assert.Throws<DivideByZeroException>(() => left % 0);
    }

    [Fact]
    public void OperatorModulo_Int_RandomValues_MatchesScalarBehavior()
    {
        var rand = new Random(1234);
        int count = Vector256<int>.Count;

        for (int trial = 0; trial < 200; trial++)
        {
            int[] values = new int[count];
            for (int i = 0; i < count; i++) values[i] = rand.Next(int.MinValue, int.MaxValue);
            Vector256<int> left = Vector256.Create(values);

            int right = rand.Next(1, int.MaxValue) * (rand.Next(2) == 0 ? 1 : -1);

            Vector256<int> result = left % right;

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(values[i] % right, result[i]);
            }
        }
    }

    [Fact]
    public void OperatorModulo_UInt_ValuesAboveIntMaxValue_ReturnsPerLaneRemainder()
    {
        Vector256<uint> left = Vector256.Create(
            uint.MaxValue,
            uint.MaxValue - 1,
            2147483648u,
            3000000000u,
            (uint)int.MaxValue + 1,
            4000000000u,
            2147483649u,
            uint.MaxValue - 7
        );
        const uint right = 13u;

        Vector256<uint> result = left % right;

        for (int i = 0; i < Vector256<uint>.Count; i++)
        {
            Assert.Equal(left[i] % right, result[i]);
        }
    }

    [Fact]
    public void OperatorModulo_UInt_DivisorAboveIntMaxValue_ReturnsPerLaneRemainder()
    {
        Vector256<uint> left = Vector256.Create(
            uint.MaxValue,
            2147483648u,
            3000000001u,
            0u,
            1u,
            4294967290u,
            2147483647u,
            2500000000u
        );
        const uint right = 3000000001u;

        Vector256<uint> result = left % right;

        for (int i = 0; i < Vector256<uint>.Count; i++)
        {
            Assert.Equal(left[i] % right, result[i]);
        }
    }

    [Fact]
    public void OperatorModulo_UInt_DivisorOfOne_ReturnsZero()
    {
        Vector256<uint> left = Vector256.Create(0u, 1u, uint.MaxValue, 2147483648u, 123456u, uint.MaxValue - 1, 7u, 4000000000u);

        Vector256<uint> result = left % 1u;

        Assert.Equal(Vector256<uint>.Zero, result);
    }

    [Fact]
    public void OperatorModulo_UInt_DivideByZero_Throws()
    {
        Vector256<uint> left = Vector256.Create(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u);

        Assert.Throws<DivideByZeroException>(() => left % 0u);
    }

    [Fact]
    public void OperatorModulo_UInt_RandomValuesAcrossFullRange_MatchesScalarBehavior()
    {
        var rand = new Random(5678);
        int count = Vector256<uint>.Count;

        for (int trial = 0; trial < 200; trial++)
        {
            uint[] values = new uint[count];
            for (int i = 0; i < count; i++) values[i] = (uint)rand.NextInt64(0, (long)uint.MaxValue + 1);
            Vector256<uint> left = Vector256.Create(values);

            uint right = (uint)rand.NextInt64(1, (long)uint.MaxValue + 1);

            Vector256<uint> result = left % right;

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(values[i] % right, result[i]);
            }
        }
    }
}
