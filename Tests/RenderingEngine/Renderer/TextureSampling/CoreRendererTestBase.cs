// AI Assisted
using RenderingEngine.Tooling;

namespace Tests.RenderingEngine.Renderer.TextureSampling;

public abstract class CoreRendererTestBase
{
    protected const int ScreenWidth = 480;
    protected const int ScreenHeight = 360;
    protected const int TextureWidth = 64;
    protected const int TextureHeight = 64;
    // BGRA: A=255, R=0, G=255, B=0
    protected const uint GreenPixel = 0xFF00FF00u;

    // Bucket indices for the aligned per-column buffers RenderWall reads via IMemoryAlignment.
    protected const int RepeatedCountBucket = 0;
    protected const int FromClampedBucket = 1;
    protected const int ToClampedBucket = 2;
    protected const int TextureXLocBucket = 3;
    protected const int TextureYLocBucket = 4;
    protected const int TextureYIncrBucket = 5;
    protected const int RenderWallBucketCount = 6;

    private readonly AlignedMemoryPool _pool;
    protected readonly uint[] Texture;

    protected CoreRendererTestBase()
    {
        _pool = AlignedMemoryPool.GeneratePool(ScreenWidth * ScreenHeight, 1);
        Texture = new uint[TextureWidth * TextureHeight];
        Array.Fill(Texture, GreenPixel);
    }

    protected Span<uint> ClearScreen()
    {
        Span<uint> screen = _pool.GetBucket<uint>(0);
        screen.Clear();
        return screen;
    }

    protected static void AssertOnlyRectRendered(
        ReadOnlySpan<uint> screen, int screenWidth,
        int xFrom, int xToExclusive, int yFrom, int yToExclusive)
    {
        int height = screen.Length / screenWidth;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < screenWidth; x++)
        {
            if (x >= xFrom && x < xToExclusive && y >= yFrom && y < yToExclusive)
                continue;
            uint pixel = screen[y * screenWidth + x];
            if (pixel != 0u)
                Assert.Fail($"Unexpected pixel 0x{pixel:X8} at ({x}, {y})");
        }
    }

    protected static void AssertScreenEmpty(ReadOnlySpan<uint> screen)
    {
        for (int i = 0; i < screen.Length; i++)
        {
            uint pixel = screen[i];
            if (pixel != 0u)
                Assert.Fail($"Unexpected pixel 0x{pixel:X8} at index {i}");
        }
    }
}
