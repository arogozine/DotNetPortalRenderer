using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BuildEngineFormulas
{
    /// <summary>
    /// "Build Engine & Tools" Copyright (c) 1993-1997 Ken Silverman
    /// Ken Silverman's official web site: "http://www.advsys.net/ken"
    /// 
    /// Ported from Belgian Chocolate Duke Nukem Source Code
    /// </summary>
    public static class SquareRoot
    {
        private static readonly uint[] sqrtable = new uint[4096];
        private static readonly uint[] shiftLookup = new uint[4096 + 256];

        static SquareRoot()
        {
            InitializeSquareRootLookup();
        }

        // classic integer square-root algorithm
        private static uint FixedSquareRoot(uint input)
        {
            uint a = 0x40000000;
            uint b = 0x20000000;

            do
            {
                if (input >= a)
                {
                    input -= a;
                    a += b * 4;
                }

                a -= b;
                a >>= 1;
                b >>= 2;
            }
            while (b != 0);

            if (input >= a)
            {
                a++;
            }

            a >>= 1;

            return a;
        }

        private static void InitializeSquareRootLookup()
        {
            uint i, j, k;

            j = 1;
            k = 0;
            for (i = 0; i < 4096; i++)
            {
                // how many times to shift mantissa
                if (i >= j)
                {
                    j <<= 2;
                    k++;
                }

                // precomputes a base square-root value for the mantissa
                sqrtable[i] = FixedSquareRoot((i << 18) + 131072) << 1;
                shiftLookup[i] = (k << 1) + ((10 - k) << 8);
                if (i < 256) shiftLookup[i + 4096] = ((k + 6) << 1) + ((10 - (k + 6)) << 8);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Lookup(uint param)
        {
            ref uint shlookup_a = ref MemoryMarshal.GetArrayDataReference(shiftLookup);
            ref uint sqrtable_a = ref MemoryMarshal.GetArrayDataReference(sqrtable);

            uint cx;

            if ((param & 0xff000000) != 0U) // large values
                cx = Unsafe.Add(ref shlookup_a, (param >> 24) + 4096);
            else
                cx = Unsafe.Add(ref shlookup_a, param >> 12);

            param >>= (int)(cx & 0xff);
            param = ((param & 0xffff0000) | Unsafe.Add(ref sqrtable_a, param));
            param >>= (int)((cx & 0xff00) >> 8);

            return param;
        }

    }
}
