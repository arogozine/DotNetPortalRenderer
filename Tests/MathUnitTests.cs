using RenderingEngine.Engine;
using System.Diagnostics;

namespace Tests
{
    public class MathUnitTests
    {
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
