using RenderingEngine.Engine;

namespace Tests.RenderingEngine.Renderer.Formulas
{
    public class SharedHelpersTests
    {
        [Fact]
        public void RefineRepeatedValues_AllZeros()
        {
            int[] valuesA = [0, 0, 0, 0];
            int[] valuesB = [0, 0, 0, 0];

            bool repeated = SharedHelpers.RefineRepeatedValues(valuesA, valuesB);

            Assert.False(repeated);
            Assert.Equal(valuesA, valuesB);
        }

        [Fact]
        public void RefineRepeatedValues_NoRepeats()
        {
            int[] valuesA = [1, 1, 1, 1];
            int[] valuesB = [1, 1, 1, 1];

            bool repeated = SharedHelpers.RefineRepeatedValues(valuesA, valuesB);

            Assert.False(repeated);
            Assert.Equal(valuesA, valuesB);
        }

        [Fact]
        public void RefineRepeatedValues_Works()
        {
            int[] valuesA = [3, 2, 1, 1];
            int[] valuesB = [2, 1, 1, 1];

            bool repeated = SharedHelpers.RefineRepeatedValues(valuesA, valuesB);

            Assert.True(repeated);
            Assert.Equal(valuesA, valuesB);
        }

        [Fact]
        public void RefineRepeatedValues_SameValues()
        {
            int[] valuesA = [3, 2, 1, 1];
            int[] valuesB = [3, 2, 1, 1];

            bool repeated = SharedHelpers.RefineRepeatedValues(valuesA, valuesB);

            Assert.True(repeated);
            Assert.Equal(valuesA, valuesB);
        }

        [Fact]
        public void PopulateRepeatedValuesInPlace_Works()
        {
            int[] values = [3, 3, 3, 4];
            bool repeated = SharedHelpers.PopulateRepeatedValuesInPlace(values);

            Assert.True(repeated);
            Assert.Equal(values, new int[] { 3, 2, 1, 1 });
        }


        [Fact]
        public void PopulateRepeatedValuesInPlace_NoRepeats()
        {
            int[] values = [1, 2, 3, 4];
            bool repeated = SharedHelpers.PopulateRepeatedValuesInPlace(values);

            Assert.False(repeated);
            Assert.Equal(values, new int[] { 1, 1, 1, 1 });
        }

        [Fact]
        public void PopulateRepeatedValues_Works()
        {
            ushort[] repeatedValues = new ushort[4];
            int[] values = [3, 3, 3, 4];
            bool repeated = SharedHelpers.PopulateRepeatedValues(repeatedValues, values);

            Assert.True(repeated);

            Assert.Equal(repeatedValues, new ushort[] { 3, 2, 1, 1});
        }

        [Fact]
        public void PopulateRepeatedValues_NoRepeats()
        {
            ushort[] repeatedValues = new ushort[4];
            int[] values = [1, 2, 3, 4];
            bool repeated = SharedHelpers.PopulateRepeatedValues(repeatedValues, values);

            Assert.False(repeated);

            Assert.Equal(repeatedValues, new ushort[] { 1, 1, 1, 1 });
        }

        [Fact]
        public void PopulateRepeatedValues_SingleValue()
        {
            ushort[] repeatedValues = new ushort[1];
            int[] values = [7];
            bool repeated = SharedHelpers.PopulateRepeatedValues(repeatedValues, values);

            Assert.False(repeated);

            Assert.Equal(repeatedValues, new ushort[] { 1 });
        }

        [Fact]
        public void PopulateRepeatedValues_NoValue()
        {
            ushort[] repeatedValues = [];
            int[] values = [];
            bool repeated = SharedHelpers.PopulateRepeatedValues(repeatedValues, values);

            Assert.False(repeated);
        }

        [Fact]
        public void Within_Works()
        {
            Assert.True(SharedHelpers.Within<decimal>(2, 1, 3));
            Assert.True(SharedHelpers.Within<double>(2, 1, 3));
            Assert.True(SharedHelpers.Within<float>(2, 1, 3));
            Assert.True(SharedHelpers.Within<int>(2, 1, 3));
            Assert.True(SharedHelpers.Within<uint>(2, 1, 3));
            Assert.True(SharedHelpers.Within<short>(2, 1, 3));
            Assert.True(SharedHelpers.Within<ushort>(2, 1, 3));
            Assert.True(SharedHelpers.Within<long>(2, 1, 3));
            Assert.True(SharedHelpers.Within<ulong>(2, 1, 3));
            Assert.True(SharedHelpers.Within<byte>(2, 1, 3));
            Assert.True(SharedHelpers.Within<sbyte>(2, 1, 3));


            Assert.False(SharedHelpers.Within<decimal>(4, 1, 3));
            Assert.False(SharedHelpers.Within<double>(4, 1, 3));
            Assert.False(SharedHelpers.Within<float>(4, 1, 3));
            Assert.False(SharedHelpers.Within<int>(4, 1, 3));
            Assert.False(SharedHelpers.Within<uint>(4, 1, 3));
            Assert.False(SharedHelpers.Within<short>(4, 1, 3));
            Assert.False(SharedHelpers.Within<ushort>(4, 1, 3));
            Assert.False(SharedHelpers.Within<long>(4, 1, 3));
            Assert.False(SharedHelpers.Within<ulong>(4, 1, 3));
            Assert.False(SharedHelpers.Within<byte>(4, 1, 3));
            Assert.False(SharedHelpers.Within<sbyte>(4, 1, 3));

            Assert.False(SharedHelpers.Within<decimal>(3, 1, 3));
            Assert.False(SharedHelpers.Within<double>(3, 1, 3));
            Assert.False(SharedHelpers.Within<float>(3, 1, 3));
            Assert.False(SharedHelpers.Within<int>(3, 1, 3));
            Assert.False(SharedHelpers.Within<uint>(3, 1, 3));
            Assert.False(SharedHelpers.Within<short>(3, 1, 3));
            Assert.False(SharedHelpers.Within<ushort>(3, 1, 3));
            Assert.False(SharedHelpers.Within<long>(3, 1, 3));
            Assert.False(SharedHelpers.Within<ulong>(3, 1, 3));
            Assert.False(SharedHelpers.Within<byte>(3, 1, 3));
            Assert.False(SharedHelpers.Within<sbyte>(3, 1, 3));
        }

        [Fact]
        public void WithinInclusive_Works()
        {
            Assert.True(SharedHelpers.WithinInclusive<decimal>(2, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<double>(2, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<float>(2, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<int>(2, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<uint>(2, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<short>(2, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<ushort>(2, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<long>(2, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<ulong>(2, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<byte>(2, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<sbyte>(2, 1, 3));


            Assert.False(SharedHelpers.WithinInclusive<decimal>(4, 1, 3));
            Assert.False(SharedHelpers.WithinInclusive<double>(4, 1, 3));
            Assert.False(SharedHelpers.WithinInclusive<float>(4, 1, 3));
            Assert.False(SharedHelpers.WithinInclusive<int>(4, 1, 3));
            Assert.False(SharedHelpers.WithinInclusive<uint>(4, 1, 3));
            Assert.False(SharedHelpers.WithinInclusive<short>(4, 1, 3));
            Assert.False(SharedHelpers.WithinInclusive<ushort>(4, 1, 3));
            Assert.False(SharedHelpers.WithinInclusive<long>(4, 1, 3));
            Assert.False(SharedHelpers.WithinInclusive<ulong>(4, 1, 3));
            Assert.False(SharedHelpers.WithinInclusive<byte>(4, 1, 3));
            Assert.False(SharedHelpers.WithinInclusive<sbyte>(4, 1, 3));

            Assert.True(SharedHelpers.WithinInclusive<decimal>(3, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<double>(3, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<float>(3, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<int>(3, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<uint>(3, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<short>(3, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<ushort>(3, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<long>(3, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<ulong>(3, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<byte>(3, 1, 3));
            Assert.True(SharedHelpers.WithinInclusive<sbyte>(3, 1, 3));
        }

        [Fact]
        public void IsPowerOfTwo_Works()
        {
            Assert.False(SharedHelpers.IsPowerOfTwo(0));
            Assert.True(SharedHelpers.IsPowerOfTwo(1));
            Assert.True(SharedHelpers.IsPowerOfTwo(2));
            Assert.True(SharedHelpers.IsPowerOfTwo(4));
            Assert.True(SharedHelpers.IsPowerOfTwo(8));
            Assert.True(SharedHelpers.IsPowerOfTwo(16));
            Assert.False(SharedHelpers.IsPowerOfTwo(-32));
        }
    }
}
