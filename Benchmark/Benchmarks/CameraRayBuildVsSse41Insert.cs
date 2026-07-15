// AI Assisted
using BenchmarkDotNet.Attributes;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Benchmark.Benchmarks
{
    [DisassemblyDiagnoser]
    public class CameraRayBuildVsSse41Insert
    {
        private float _cameraRay;
        private float _cameraWidthIncr;

        [GlobalSetup]
        public void Setup()
        {
            _cameraRay = 0.1234f;
            _cameraWidthIncr = 0.0057f;
        }

        [Benchmark(Baseline = true)]
        public Vector128<float> StackallocSpan()
        {
            float cameraRay = _cameraRay;
            float cameraWidthIncr = _cameraWidthIncr;

            Span<float> cameraRaySpan = stackalloc float[Vector128<float>.Count];
            for (int i = 0; i < Vector128<float>.Count; i++)
            {
                cameraRaySpan[i] = cameraRay;
                cameraRay += cameraWidthIncr;
            }

            return Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(cameraRaySpan));
        }

        [Benchmark]
        public Vector128<float> Sse41InsertC()
        {
            float cameraRay = _cameraRay;
            float cameraWidthIncr = _cameraWidthIncr;

            Vector128<int> cameraRayV = Vector128.CreateScalarUnsafe(cameraRay)
                .AsInt32();

            for (byte i = 0; i < Vector128<float>.Count; i++)
            {
                int cameraRayI = BitConverter.SingleToInt32Bits(cameraRay);
                cameraRayV = Sse41.Insert(cameraRayV, cameraRayI, i);
                cameraRay += cameraWidthIncr;
            }

            return cameraRayV.AsSingle();
        }

        [Benchmark]
        public Vector128<float> Sse41InsertE()
        {
            float cameraRay = _cameraRay;
            float cameraWidthIncr = _cameraWidthIncr;

            Vector128<int> cameraRayV = Vector128.CreateScalarUnsafe(cameraRay)
                .AsInt32();

            cameraRayV = Sse41.Insert(cameraRayV, BitConverter.SingleToInt32Bits(cameraRay), 0);
            cameraRay += cameraWidthIncr;

            cameraRayV = Sse41.Insert(cameraRayV, BitConverter.SingleToInt32Bits(cameraRay), 1);
            cameraRay += cameraWidthIncr;

            cameraRayV = Sse41.Insert(cameraRayV, BitConverter.SingleToInt32Bits(cameraRay), 2);
            cameraRay += cameraWidthIncr;

            cameraRayV = Sse41.Insert(cameraRayV, BitConverter.SingleToInt32Bits(cameraRay), 3);
            cameraRay += cameraWidthIncr;

            /*
            for (byte i = 0; i < Vector128<float>.Count; i++)
            {
                int cameraRayI = BitConverter.SingleToInt32Bits(cameraRay);
                cameraRayV = Sse41.Insert(cameraRayV, cameraRayI, i);
                cameraRay += cameraWidthIncr;
            }
            */

            return cameraRayV.AsSingle();
        }

        [Benchmark]
        public Vector128<float> Sse41InsertB()
        {
            float cameraRay = _cameraRay;
            float cameraWidthIncr = _cameraWidthIncr;

            // Manually unrolled: Sse41.Insert only compiles to INSERTPS when
            // imm8 is a JIT-time constant, so the index can't be a loop variable.
            Vector128<float> cameraRayV = Vector128.CreateScalarUnsafe(cameraRay);
            cameraRay += cameraWidthIncr;

            cameraRayV = Sse41.Insert(cameraRayV, Vector128.CreateScalarUnsafe(cameraRay), 0b0001_0000);
            cameraRay += cameraWidthIncr;

            cameraRayV = Sse41.Insert(cameraRayV, Vector128.CreateScalarUnsafe(cameraRay), 0b0010_0000);
            cameraRay += cameraWidthIncr;

            cameraRayV = Sse41.Insert(cameraRayV, Vector128.CreateScalarUnsafe(cameraRay), 0b0011_0000);

            return cameraRayV;
        }
    }
}
