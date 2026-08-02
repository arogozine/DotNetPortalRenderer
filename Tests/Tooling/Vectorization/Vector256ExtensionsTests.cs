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
}
