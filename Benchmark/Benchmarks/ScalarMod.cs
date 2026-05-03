using BenchmarkDotNet.Attributes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Benchmark.Benchmarks
{
    [DisassemblyDiagnoser]
    public class ScalarMod
    {
        private readonly uint[] _data;
        public ScalarMod()
        {
            _data = new uint[1024];
            var rand = new Random();

            for (int i = 0; i < 1024; i++)
            {
                _data[i] = (uint)rand.Next(0, 255);
            }
        }

        [Benchmark(Baseline = true)]
        public void BasicMod() {
            for (int i = 0; i < _data.Length; i++)
            {
                _data[i] %= 7u;
            }
        }

        [Benchmark]
        public void TweakedMod()
        {
            uint texMask = 7u;
            uint recip = (1 << 16) / texMask + 1;

            for (int i = 0; i < _data.Length; i++)
            {
                uint raw = _data[i];

                _data[i] = raw - texMask * ((recip * raw) >> 16);
            }
        }


        /*
        [Benchmark]
        public void Vectorized()
        {
            var b = Vector256.Create(7u);

            for (int i = 0; i < _data.Length; i += Vector<int>.Count)
            {
                Vector256<uint> a = Vector256.LoadUnsafe(ref _data[i]);

                // a / b
                Vector256<uint> quotient = Vector256.Divide(a, b);

                // (a / b) * b
                quotient = Vector256.Multiply(quotient, b);

                // a - multiple
                Vector256.StoreUnsafe(Vector256.Subtract(a, quotient), ref _data[i]);
            }

        }
        */

        /*
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
        */
    }
}
