using Newtonsoft.Json.Linq;
using RenderingEngine.Engine;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tests
{
    public class MathFormulaUnitTests
    {

        [Fact]
        public void ClampAngle_Works()
        {
            for (float i = -9.9f; i < 9.9f; i += 0.1f)
            {
                (float expectedA, float expectedB) = MathF.SinCos(i);

                float iClamped = MathFormulas.ClampAngle(i);
                Assert.True(iClamped >= 0f);

                (float a, float b) = MathF.SinCos(i);
                Assert.Equal(expectedA, a);
                Assert.Equal(expectedB, b);
            }
        }

        [Fact]
        public void FastConvertionFloatToIntNative()
        {
            Random r = new();

            for (int i = 0; i < 512; i++)
            {
                // random less than 1.0
                float iF = i + r.NextSingle();

                int expected = (int)iF;
                int fast = float.ConvertToIntegerNative<int>(iF);

                Debug.WriteLine($"For {iF}: {expected} vs {fast}");
                Assert.Equal(expected, fast);
            }
        }

        [Fact]
        public void FastMod()
        {
            // https://lemire.me/blog/2019/02/08/faster-remainders-when-the-divisor-is-a-constant-beating-compilers-and-libdivide/
            const int d = 7;
            ulong c = GetFastModC(d);

            Random r = new();

            for (int i = 0; i < 4096; i++)
            {
                uint n = (uint)r.Next(0, short.MaxValue);
                // 30065, 28315

                uint mod = n % d;
                uint mod2 = FastMod(c, n, d);
                uint mod3 = FastMod2(c, n, d);
                uint mod4 = FastMod3(c, n, d);

                Assert.Equal(mod, mod2);
                Assert.Equal(mod, mod3);
                Assert.Equal(mod, mod4);

                /*
                ulong recip = (1UL << 32) / d;
                ulong q = (recip * (ulong)n) >> 32;
                ulong mod5 = n - d * q;
                Assert.Equal(mod, mod5);
                */
            }

            static ulong GetFastModC(uint divisor)
            {
                return 1UL + ulong.MaxValue / divisor;
            }

            static uint FastMod(ulong c, uint n, uint d)
            {
                ulong lowbits = c * n;
                return (uint)(((UInt128)lowbits * d) >> 64);
            }

            static uint FastMod2(ulong c, uint n, uint d)
            {
                ulong lowbits = c * n;
                return (uint)Math.BigMul((ulong)lowbits, (ulong)d, out _);
                // return (uint)(((UInt128)lowbits * d) >> 64);
            }

            static uint FastMod3(ulong c, uint n, uint d)
            {
                ulong lowbits = c * n;
                return (uint)Bmi2.X64.MultiplyNoFlags(lowbits, d);
                //return (uint)Math.BigMul((ulong)lowbits, (ulong)d, out _);
                // return (uint)(((UInt128)lowbits * d) >> 64);
            }

            /*
             
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
            */
        }

        [Fact]
        public void FastModV()
        {
            // https://lemire.me/blog/2019/02/08/faster-remainders-when-the-divisor-is-a-constant-beating-compilers-and-libdivide/
            const int d = 7;
            Vector256<ulong> c = Vector256.Create(GetFastModC(d));

            Random r = new();

            for (int i = 0; i < 4096; i += Vector256<uint>.Count)
            {
                var v = Vector256.Create((uint)r.Next(), (uint)r.Next(), (uint)r.Next(), (uint)r.Next(), (uint)r.Next(), (uint)r.Next(), (uint)r.Next(), (uint)r.Next());
                (var vA, var vB) = Vector256.Widen(v);
                vA *= c;
                vB *= c;

                int mI = 0;
                Span<uint> modV = [v[0] % d, v[1] % d, v[2] % d, v[3] % d, v[4] % d, v[5] % d, v[6] % d, v[7] % d];

                for (int j = 0; j < Vector256<ulong>.Count; j++, mI++)
                {
                    uint mod = modV[mI];
                    uint mod2 = FastMod(vA[j], 7u);
                    Assert.Equal(mod, mod2);
                }

                for (int j = 0; j < Vector256<ulong>.Count; j++, mI++)
                {
                    uint mod = modV[mI];
                    uint mod2 = FastMod(vB[j], 7u);
                    Assert.Equal(mod, mod2);
                }

            }

            static ulong GetFastModC(uint divisor)
            {
                return 1UL + ulong.MaxValue / divisor;
            }

            static uint FastMod(ulong lowbits, uint d)
            {
                return (uint)Bmi2.X64.MultiplyNoFlags(lowbits, d);

                //ulong lowbits = c * n;
                // return (uint)(((UInt128)lowbits * d) >> 64);
            }
        }
    }
}
