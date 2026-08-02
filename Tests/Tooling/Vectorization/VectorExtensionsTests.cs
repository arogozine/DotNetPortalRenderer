using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Tooling;

namespace Tests;

public unsafe class VectorExtensionsTests
{
    private const nuint BufferSize = 64;
    private const nuint BufferAlignment = 64;

    private static void WithAlignedBuffer(Action<IntPtr> action)
    {
        IntPtr ptr = (IntPtr)NativeMemory.AlignedAlloc(BufferSize, BufferAlignment);
        try
        {
            action(ptr);
        }
        finally
        {
            NativeMemory.AlignedFree((void*)ptr);
        }
    }

    private static void RoundTripVectorInt<TAlign>() where TAlign : IMemoryAlignment
    {
        WithAlignedBuffer(ptr =>
        {
            int count = Vector<int>.Count;
            int[] values = new int[count];
            for (int i = 0; i < count; i++) values[i] = i + 1;

            var vector = new Vector<int>(values);
            int* p = (int*)ptr;

            TAlign.Store(p, vector);
            Vector<int> loaded = TAlign.Load(p);

            Assert.Equal(vector, loaded);
        });
    }

    private static void RoundTripVectorFloat<TAlign>() where TAlign : IMemoryAlignment
    {
        WithAlignedBuffer(ptr =>
        {
            int count = Vector<float>.Count;
            float[] values = new float[count];
            for (int i = 0; i < count; i++) values[i] = i + 0.5f;

            var vector = new Vector<float>(values);
            float* p = (float*)ptr;

            TAlign.Store(p, vector);
            Vector<float> loaded = TAlign.Load(p);

            Assert.Equal(vector, loaded);
        });
    }

    private static void RoundTripVector64Int<TAlign>() where TAlign : IMemoryAlignment
    {
        WithAlignedBuffer(ptr =>
        {
            Vector64<int> vector = Vector64.Create(1, 2);
            int* p = (int*)ptr;

            TAlign.Store(p, vector);
            Vector64<int> loaded = TAlign.Load64(p);

            Assert.Equal(vector, loaded);
        });
    }

    private static void RoundTripVector64Float<TAlign>() where TAlign : IMemoryAlignment
    {
        WithAlignedBuffer(ptr =>
        {
            Vector64<float> vector = Vector64.Create(1.5f, 2.5f);
            float* p = (float*)ptr;

            TAlign.Store(p, vector);
            Vector64<float> loaded = TAlign.Load64(p);

            Assert.Equal(vector, loaded);
        });
    }

    private static void RoundTripVector128Int<TAlign>() where TAlign : IMemoryAlignment
    {
        WithAlignedBuffer(ptr =>
        {
            Vector128<int> vector = Vector128.Create(1, 2, 3, 4);
            int* p = (int*)ptr;

            TAlign.Store(p, vector);
            Vector128<int> loaded = TAlign.Load128(p);

            Assert.Equal(vector, loaded);
        });
    }

    private static void RoundTripVector128Float<TAlign>() where TAlign : IMemoryAlignment
    {
        WithAlignedBuffer(ptr =>
        {
            Vector128<float> vector = Vector128.Create(1.5f, 2.5f, 3.5f, 4.5f);
            float* p = (float*)ptr;

            TAlign.Store(p, vector);
            Vector128<float> loaded = TAlign.Load128(p);

            Assert.Equal(vector, loaded);
        });
    }

    private static void RoundTripVector256Int<TAlign>() where TAlign : IMemoryAlignment
    {
        WithAlignedBuffer(ptr =>
        {
            Vector256<int> vector = Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8);
            int* p = (int*)ptr;

            TAlign.Store(p, vector);
            Vector256<int> loaded = TAlign.Load256(p);

            Assert.Equal(vector, loaded);
        });
    }

    private static void RoundTripVector256Float<TAlign>() where TAlign : IMemoryAlignment
    {
        WithAlignedBuffer(ptr =>
        {
            Vector256<float> vector = Vector256.Create(1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f);
            float* p = (float*)ptr;

            TAlign.Store(p, vector);
            Vector256<float> loaded = TAlign.Load256(p);

            Assert.Equal(vector, loaded);
        });
    }

    [Fact]
    public void StoreThenLoad_Vector_AlignedMemory_RoundTripsForIntAndFloat()
    {
        RoundTripVectorInt<AlignedMemory>();
        RoundTripVectorFloat<AlignedMemory>();
    }

    [Fact]
    public void StoreThenLoad_Vector64_AlignedMemory_RoundTripsForIntAndFloat()
    {
        RoundTripVector64Int<AlignedMemory>();
        RoundTripVector64Float<AlignedMemory>();
    }

    [Fact]
    public void StoreThenLoad_Vector128_AlignedMemory_RoundTripsForIntAndFloat()
    {
        RoundTripVector128Int<AlignedMemory>();
        RoundTripVector128Float<AlignedMemory>();
    }

    [Fact]
    public void StoreThenLoad_Vector256_AlignedMemory_RoundTripsForIntAndFloat()
    {
        RoundTripVector256Int<AlignedMemory>();
        RoundTripVector256Float<AlignedMemory>();
    }

    [Fact]
    public void StoreThenLoad_Vector_UnalignedMemory_RoundTripsForIntAndFloat()
    {
        RoundTripVectorInt<UnalignedMemory>();
        RoundTripVectorFloat<UnalignedMemory>();
    }

    [Fact]
    public void StoreThenLoad_Vector64_UnalignedMemory_RoundTripsForIntAndFloat()
    {
        RoundTripVector64Int<UnalignedMemory>();
        RoundTripVector64Float<UnalignedMemory>();
    }

    [Fact]
    public void StoreThenLoad_Vector128_UnalignedMemory_RoundTripsForIntAndFloat()
    {
        RoundTripVector128Int<UnalignedMemory>();
        RoundTripVector128Float<UnalignedMemory>();
    }

    [Fact]
    public void StoreThenLoad_Vector256_UnalignedMemory_RoundTripsForIntAndFloat()
    {
        RoundTripVector256Int<UnalignedMemory>();
        RoundTripVector256Float<UnalignedMemory>();
    }

    [Fact]
    public void StoreThenLoad_Vector128AndVector_UnalignedMemory_ToleratesMisalignedPointer()
    {
        // Over-allocate and use an offset of 1 byte to guarantee misalignment relative to any vector width.
        byte[] backing = new byte[128];

        fixed (byte* basePtr = backing)
        {
            byte* misaligned = basePtr + 1;

            int* intPtr = (int*)misaligned;
            Vector128<int> vector = Vector128.Create(10, 20, 30, 40);
            UnalignedMemory.Store(intPtr, vector);
            Vector128<int> loaded128 = UnalignedMemory.Load128(intPtr);
            Assert.Equal(vector, loaded128);

            int count = Vector<int>.Count;
            int[] values = new int[count];
            for (int i = 0; i < count; i++) values[i] = i + 100;
            var fullVector = new Vector<int>(values);

            UnalignedMemory.Store(intPtr, fullVector);
            Vector<int> loadedFull = UnalignedMemory.Load(intPtr);
            Assert.Equal(fullVector, loadedFull);
        }
    }

    [Fact]
    public void StoreThenLoad_Vector128_AlignedAndUnalignedImplementations_ProduceIdenticalDataForSameAlignedBuffer()
    {
        WithAlignedBuffer(ptr =>
        {
            int* p = (int*)ptr;
            Vector128<int> vector = Vector128.Create(1, 2, 3, 4);

            AlignedMemory.Store(p, vector);
            Vector128<int> loadedUnaligned = UnalignedMemory.Load128(p);

            Assert.Equal(vector, loadedUnaligned);

            UnalignedMemory.Store(p, vector);
            Vector128<int> loadedAligned = AlignedMemory.Load128(p);

            Assert.Equal(vector, loadedAligned);
        });
    }

    [Fact]
    public void Gather_ReturnsValuesAtEachIndex()
    {
        int count = Vector<uint>.Count;
        uint[] source = new uint[count * 2];
        for (int i = 0; i < source.Length; i++) source[i] = (uint)(i * 10 + 1);

        int[] indexValues = new int[count];
        for (int i = 0; i < count; i++) indexValues[i] = (count - 1 - i) * 2;
        var index = new Vector<int>(indexValues);

        fixed (uint* basePtr = source)
        {
            Vector<uint> gathered = Vector<uint>.Gather(basePtr, index);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(source[indexValues[i]], gathered[i]);
            }
        }
    }

    [Fact]
    public void GatherMask_ReturnsValuesForSetLanesAndZeroForMaskedLanes()
    {
        int count = Vector<uint>.Count;
        uint[] source = new uint[count];
        for (int i = 0; i < count; i++) source[i] = (uint)(i + 1);

        int[] indexValues = new int[count];
        for (int i = 0; i < count; i++) indexValues[i] = i;
        var index = new Vector<int>(indexValues);

        uint[] maskValues = new uint[count];
        for (int i = 0; i < count; i++) maskValues[i] = i % 2 == 0 ? uint.MaxValue : 0u;
        var mask = new Vector<uint>(maskValues);

        fixed (uint* basePtr = source)
        {
            Vector<uint> gathered = Vector<uint>.GatherMask(basePtr, index, mask);

            for (int i = 0; i < count; i++)
            {
                uint expected = maskValues[i] != 0 ? source[indexValues[i]] : 0u;
                Assert.Equal(expected, gathered[i]);
            }
        }
    }

    [Fact]
    public void GatherMask_AllLanesMasked_ReturnsZeroVector()
    {
        int count = Vector<uint>.Count;
        uint[] source = new uint[count];
        for (int i = 0; i < count; i++) source[i] = (uint)(i + 1);

        int[] indexValues = new int[count];
        for (int i = 0; i < count; i++) indexValues[i] = i;
        var index = new Vector<int>(indexValues);

        fixed (uint* basePtr = source)
        {
            Vector<uint> gathered = Vector<uint>.GatherMask(basePtr, index, Vector<uint>.Zero);

            Assert.Equal(Vector<uint>.Zero, gathered);
        }
    }
}
