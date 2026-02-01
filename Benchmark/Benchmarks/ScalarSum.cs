using BenchmarkDotNet.Attributes;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Benchmark.Benchmarks
{
    [DisassemblyDiagnoser]
    public class ScalarSum
    {
        private readonly int[] _data;
        public ScalarSum()
        {
            _data = new int[1024];

            var rand = new Random();
            for (int i = 0; i < 1024; i++)
            {
                _data[i] = rand.Next(0, 255);
            }
        }

        [Benchmark(Baseline = true)]
        public int BasicSum() {
            int sum = 0;
            for (int i = 0; i < _data.Length; i++)
            {
                sum += _data[i];
            }
            return sum;
        }

        [Benchmark]
        public int VectorLoadScalarSum()
        {
            int sum = 0;
            for (int i = 0; i < _data.Length; i+= Vector<int>.Count)
            {
                Vector<int> v = Vector.LoadUnsafe(ref _data[i]);
                for (int j = 0; j < Vector<int>.Count; j++)
                {
                    sum += v[j];
                }
            }
            return sum;
        }

        [Benchmark]
        public int VectorLoadScalarSum_GetElement()
        {
            int sum = 0;
            for (int i = 0; i < _data.Length; i += Vector<int>.Count)
            {
                Vector<int> v = Vector.LoadUnsafe(ref _data[i]);
                for (int j = 0; j < Vector<int>.Count; j++)
                {
                    sum += v.GetElement(j);
                }
            }
            return sum;
        }


        [Benchmark]
        public int VectorLoadScalarSum_Unsafe()
        {
            int sum = 0;
            for (int i = 0; i < _data.Length; i += Vector<int>.Count)
            {
                Vector<int> v = Vector.LoadUnsafe(ref _data[i]);
                ref int vPtr = ref Unsafe.As<Vector<int>, int>(ref v);
                for (int j = 0; j < Vector<int>.Count; j++)
                {
                    sum += Unsafe.Add(ref vPtr, j);
                }
            }
            return sum;
        }

        [Benchmark]
        public int VectorLoadAndSum()
        {
            int sum = 0;
            for (int i = 0; i < _data.Length; i += Vector<int>.Count)
            {
                Vector<int> v = Vector.LoadUnsafe(ref _data[i]);
                sum += Vector.Sum(v);
            }

            return sum;
        }
    }
}
