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
    }
}
