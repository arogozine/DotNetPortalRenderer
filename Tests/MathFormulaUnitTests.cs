using RenderingEngine.Engine;
using System.Diagnostics;

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
    }
}
