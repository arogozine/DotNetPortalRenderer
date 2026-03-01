namespace BuildEngineFormulas
{
    public static class FixedPointMath
    {
        public static int DivScale(int a, int b, int shift)
        {
            return (int)(((long)a << shift) / b);
        }

        public static int MulScale(int a, int b, int shift)
        {
            return (int)(((long)a * b) >> shift);
        }

        public static int DMulScale(int a, int b, int c, int d, int shift)
        {
            return (int)(((long)a * b + (long)c * d) >> shift);
        }

        public static int TMulScale(int a, int b, int c, int d, int e, int f, int shift)
        {
            return (int)(((long)a * b + (long)c * d + (long)e * f) >> shift);
        }

        public static long Mul32_64(int i1, int i2)
        {
            return (long)i1 * i2;
        }

        public static int Scale(int input1, int input2, int input3)
        {
            return (int)(Mul32_64(input1, input2) / (long)input3);
        }
    }
}
