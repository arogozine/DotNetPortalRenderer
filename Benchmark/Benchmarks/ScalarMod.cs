using BenchmarkDotNet.Attributes;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Benchmark.Benchmarks
{
    [DisassemblyDiagnoser]
    public class ScalarMod
    {
        private readonly int[] _data;
        public ScalarMod()
        {
            _data = new int[1024];
            var rand = new Random();

            for (int i = 0; i < 1024; i++)
            {
                _data[i] = rand.Next(0, 255);
            }
        }

        [Benchmark(Baseline = true)]
        public void BasicMod() {
            for (int i = 0; i < _data.Length; i++)
            {
                _data[i] %= 7;
            }
        }

        [Benchmark]
        public void VectorAssistedMod_Unsafe()
        {
            for (int i = 0; i < _data.Length; i += Vector<int>.Count)
            {
                Vector<int> v = Vector.LoadUnsafe(ref _data[i]);
                ref int vPtr = ref Unsafe.As<Vector<int>, int>(ref v);

                for (int j = 0; j < Vector<int>.Count; j++)
                {
                    Unsafe.Add(ref vPtr, j) %= 7;
                }

                Vector.StoreUnsafe(v, ref _data[i]);
            }
        }

        [Benchmark]
        public void VectorAssistedMod_Unsafe2()
        {
            Span<int> numbers = stackalloc int[Vector<int>.Count];

            for (int i = 0; i < _data.Length; i += Vector<int>.Count)
            {
                Vector<int> v = Vector.LoadUnsafe(ref _data[i]);
                Vector.StoreUnsafe(v, ref numbers[0]);

                ref int vPtr = ref Unsafe.As<Vector<int>, int>(ref v);

                for (int j = 0; j < Vector<int>.Count; j++)
                {
                    Unsafe.Add(ref vPtr, j) %= 7;
                }

                Vector.StoreUnsafe(v, ref _data[i]);
            }
        }
    }
}
