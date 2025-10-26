using BenchmarkDotNet.Attributes;
using System.Numerics;

namespace Benchmark.Benchmarks
{
    // [MemoryDiagnoser]
    public class VectorShiftVsMultiply
    {
        private Vector<int> _x;
        private Vector<int> _y;
        private Vector<int> _64;
        private const int ShiftAmount = 6;
        private const int MultiplyFactor = 64;

        [GlobalSetup]
        public void Setup()
        {
            int[] data = new int[Vector<int>.Count];
            for (int i = 0; i < data.Length; i++)
                data[i] = i;

            _x = new Vector<int>(data);
            _y = new Vector<int>(data);
            _64 = Vector.Create(MultiplyFactor);
        }

        [Benchmark]
        public Vector<int> ShiftLeftAndAdd()
        {
            return Vector.ShiftLeft(_y, ShiftAmount) + _x;
        }

        [Benchmark]
        public Vector<int> MultiplyAndAdd()
        {
            return _y * MultiplyFactor + _x;
        }


        [Benchmark]
        public Vector<int> MultiplyAndAddV()
        {
            return _y * _64 + _x;
        }
    }
}
