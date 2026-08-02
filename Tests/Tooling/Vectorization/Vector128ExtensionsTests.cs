using System.Runtime.Intrinsics;
using Tooling;

namespace Tests;

// Behavior must be identical across the Avx2, Sse41-fallback, and scalar-fallback code paths.
// Run with: normal, DOTNET_EnableAVX2=0 (forces Sse41 fallback), and DOTNET_EnableHWIntrinsic=0 (forces scalar fallback).
public unsafe class Vector128ExtensionsTests
{
    [Fact]
    public void Gather_Int32Family_ReturnsValuesAtEachIndex()
    {
        int count = Vector128<uint>.Count;
        uint[] source = new uint[count * 2];
        for (int i = 0; i < source.Length; i++) source[i] = (uint)(i * 10 + 1);

        Vector128<int> index = Vector128.Create(6, 4, 2, 0);

        fixed (uint* basePtr = source)
        {
            Vector128<uint> gathered = Vector128.Gather(basePtr, index);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(source[index[i]], gathered[i]);
            }
        }
    }

    [Fact]
    public void Gather_Float_ReturnsValuesAtEachIndex()
    {
        int count = Vector128<float>.Count;
        float[] source = new float[count * 2];
        for (int i = 0; i < source.Length; i++) source[i] = i * 1.5f;

        Vector128<int> index = Vector128.Create(7, 5, 3, 1);

        fixed (float* basePtr = source)
        {
            Vector128<float> gathered = Vector128.Gather(basePtr, index);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(source[index[i]], gathered[i]);
            }
        }
    }

    [Fact]
    public void Gather_Int64Family_ReturnsValuesAtEachIndex()
    {
        int count = Vector128<long>.Count;
        long[] source = new long[count * 2];
        for (int i = 0; i < source.Length; i++) source[i] = i * 100 + 1;

        // Only the first `count` lanes of the index vector are consulted for 64-bit element gathers.
        Vector128<int> index = Vector128.Create(3, 0, 0, 0);

        fixed (long* basePtr = source)
        {
            Vector128<long> gathered = Vector128.Gather(basePtr, index);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(source[index[i]], gathered[i]);
            }
        }
    }

    [Fact]
    public void Gather_Double_ReturnsValuesAtEachIndex()
    {
        int count = Vector128<double>.Count;
        double[] source = new double[count * 2];
        for (int i = 0; i < source.Length; i++) source[i] = i * 2.5;

        Vector128<int> index = Vector128.Create(3, 1, 0, 0);

        fixed (double* basePtr = source)
        {
            Vector128<double> gathered = Vector128.Gather(basePtr, index);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(source[index[i]], gathered[i]);
            }
        }
    }

    [Fact]
    public void Gather_UnsupportedType_ThrowsNotSupportedException()
    {
        byte[] source = new byte[16];

        fixed (byte* basePtr = source)
        {
            byte* p = basePtr;
            Assert.Throws<NotSupportedException>(() => Vector128.Gather(p, Vector128<int>.Zero));
        }
    }

    [Fact]
    public void GatherMask_Int32Family_ReturnsValuesForSetLanesAndZeroForMaskedLanes()
    {
        int count = Vector128<uint>.Count;
        uint[] source = new uint[count];
        for (int i = 0; i < count; i++) source[i] = (uint)(i + 1);

        Vector128<int> index = Vector128.Create(0, 1, 2, 3);
        Vector128<int> mask = Vector128.Create(-1, 0, -1, 0);

        fixed (uint* basePtr = source)
        {
            Vector128<uint> gathered = Vector128.GatherMask(basePtr, index, mask);

            for (int i = 0; i < count; i++)
            {
                uint expected = mask[i] != 0 ? source[index[i]] : 0u;
                Assert.Equal(expected, gathered[i]);
            }
        }
    }

    [Fact]
    public void GatherMask_Float_ReturnsValuesForSetLanesAndZeroForMaskedLanes()
    {
        int count = Vector128<float>.Count;
        float[] source = new float[count];
        for (int i = 0; i < count; i++) source[i] = i + 0.5f;

        Vector128<int> index = Vector128.Create(0, 1, 2, 3);
        Vector128<int> mask = Vector128.Create(0, -1, 0, -1);

        fixed (float* basePtr = source)
        {
            Vector128<float> gathered = Vector128.GatherMask(basePtr, index, mask);

            for (int i = 0; i < count; i++)
            {
                float expected = mask[i] != 0 ? source[index[i]] : 0f;
                Assert.Equal(expected, gathered[i]);
            }
        }
    }

    [Fact]
    public void GatherMask_WithSource_Int32Family_KeepsSourceForMaskedLanes()
    {
        int count = Vector128<uint>.Count;
        uint[] source = new uint[count];
        for (int i = 0; i < count; i++) source[i] = (uint)(i + 1);

        Vector128<int> index = Vector128.Create(3, 2, 1, 0);
        Vector128<int> mask = Vector128.Create(-1, 0, -1, 0);
        Vector128<uint> sourceVector = Vector128.Create(100u, 200u, 300u, 400u);

        fixed (uint* basePtr = source)
        {
            Vector128<uint> gathered = Vector128.GatherMask(sourceVector, basePtr, index, mask);

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
        int count = Vector128<long>.Count;
        long[] source = new long[count];
        for (int i = 0; i < count; i++) source[i] = (i + 1) * 10;

        Vector128<int> index = Vector128.Create(1, 0, 0, 0);
        Vector128<long> mask = Vector128.Create(-1L, 0L);

        fixed (long* basePtr = source)
        {
            Vector128<long> gathered = Vector128.GatherMask(basePtr, index, mask);

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
        int count = Vector128<long>.Count;
        long[] source = new long[count];
        for (int i = 0; i < count; i++) source[i] = (i + 1) * 10;

        Vector128<int> index = Vector128.Create(1, 0, 0, 0);
        Vector128<long> mask = Vector128.Create(0L, -1L);
        Vector128<long> sourceVector = Vector128.Create(500L, 600L);

        fixed (long* basePtr = source)
        {
            Vector128<long> gathered = Vector128.GatherMask(sourceVector, basePtr, index, mask);

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
        int count = Vector128<uint>.Count;
        uint[] source = new uint[count];
        for (int i = 0; i < count; i++) source[i] = (uint)(i + 1);

        Vector128<int> index = Vector128.Create(0, 1, 2, 3);

        fixed (uint* basePtr = source)
        {
            Vector128<uint> gathered = Vector128.GatherMask(basePtr, index, Vector128<int>.Zero);

            Assert.Equal(Vector128<uint>.Zero, gathered);
        }
    }
}
