using BenchmarkDotNet.Attributes;
using SoftwareRendererModels;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Benchmark
{
    public class BGRATest2
    {
        private readonly BGRA[] Meh;
        private readonly float RandF;

        public BGRATest2()
        {
            Meh = new BGRA[5000];
            var rand = new Random();
            rand.NextBytes(MemoryMarshal.Cast<BGRA, byte>(Meh.AsSpan()));


            RandF = (float)rand.NextDouble();

        }

        // [Benchmark]
        public BGRA ShadeByBrightness1()
        {
            BGRA result = default;
            float f = RandF;

            for (int i = 0; i < Meh.Length; i++)
            {
                var old = Meh[i];
                result = ShadeByBrightness1(in old, f);
            }

            return result;
        }

        [Benchmark]
        public BGRA ShadeByBrightness2()
        {
            BGRA result = default;
            float f = RandF;

            for (int i = 0; i < Meh.Length; i++)
            {
                ref var old = ref Meh[i];
                result = ShadeByBrightness2(in old, f);
            }

            return result;
        }

        [Benchmark]
        public BGRA ShadeByBrightness3()
        {
            BGRA result = default;
            float f = RandF;

            for (int i = 0; i < Meh.Length; i++)
            {
                var old = Meh[i];
                result = ShadeByBrightness3(old, f);
            }

            return result;
        }

        [Benchmark]
        public uint ShadeByBrightness4()
        {
            uint result = default;
            float f = RandF;

            for (int i = 0; i < Meh.Length; i++)
            {
                var old = Meh[i];
                result = ShadeByBrightness4(old, f);
            }

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BGRA ShadeByBrightness1(in BGRA color, float brightness)
        {
            if (brightness <= 0)
            {
                return BGRA.Black;
            }

            return new BGRA(
                (byte)(brightness * color.B),
                (byte)(brightness * color.G),
                (byte)(brightness * color.R)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BGRA ShadeByBrightness2(in BGRA color, float brightness)
        {
            if (brightness <= 0)
            {
                return BGRA.Black;
            }

            uint scale = (uint)(brightness * 255f); // Fixed-point scale

            return new BGRA(
                (byte)((color.B * scale) >> 8),
                (byte)((color.G * scale) >> 8),
                (byte)((color.R * scale) >> 8),
                byte.MaxValue
            );

            /*
            return new BGRA(
                (byte)(brightness * color.B),
                (byte)(brightness * color.G),
                (byte)(brightness * color.R)
            );
            */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BGRA ShadeByBrightness3(BGRA color, float brightness)
        {
            if (brightness <= 0)
            {
                return BGRA.Black;
            }

            uint scale = (uint)(brightness * 255f); // Fixed-point scale

            return new BGRA(
                (byte)((color.B * scale) >> 8),
                (byte)((color.G * scale) >> 8),
                (byte)((color.R * scale) >> 8),
                byte.MaxValue
            );

            /*
            return new BGRA(
                (byte)(brightness * color.B),
                (byte)(brightness * color.G),
                (byte)(brightness * color.R)
            );
            */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ShadeByBrightness4(BGRA color, float brightness)
        {
            const uint Alpha = (uint)byte.MaxValue << 24;

            if (brightness <= 0)
            {
                return BGRA.Black.Value;
            }

            uint scale = (uint)(brightness * 255f); // Fixed-point scale

            uint b = ((color.B * scale) >> 8);
            uint g = ((color.G * scale) >> 8) << 8;
            uint r = ((color.R * scale) >> 8) << 16;

            return b | g | r | Alpha;
        }
    }
}
