using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void Meh(Span<BGRA> screen, RenderWindowSpriteSnapshot sectorSprites)
        {
            var floorEnd = sectorSprites.FloorEnd;
            var ceilingStart = sectorSprites.CeilingStart;

            for (int x = 0; x < PixelWidth; x++)
            {
                int floor = floorEnd[x];
                int ceiling = ceilingStart[x];

                Render(screen, ceiling, x, BGRA.Green);
                Render(screen, floor, x, BGRA.White);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Render(Span<BGRA> screen, int y, int x, BGRA color)
            {
                int index = (y - 1) * PixelWidth + x;

                if (index > 0 && index < screen.Length)
                {
                    screen[index] = color;
                }

                index += PixelWidth;
                if (index > 0 && index < screen.Length)
                {
                    screen[index] = color;
                }

                index += PixelWidth;
                if (index > 0 && index < screen.Length)
                {
                    screen[index] = color;
                }
            }
        }

        private void DebugZBuffer(Span<BGRA> screen, Span<RenderWindow> window)
        {
            int height = Math.Min(PixelHeight, 20);
            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));


            float min = float.MaxValue;
            float max = float.MinValue;
            for (int x = 0; x < PixelWidth; x++)
            {
                ref RenderWindow renderWindow = ref window[x];
                float value = renderWindow.Distance;

                min = MathF.Min(value, min);
                max = MathF.Max(value, max);
            }

            float range = byte.MaxValue / max;

            for (int x = 0; x < PixelWidth; x++)
            {
                ref RenderWindow renderWindow = ref window[x];
                float value = renderWindow.Distance;
                uint val = (uint)Math.Clamp(float.ConvertToIntegerNative<int>(value * range), 0, byte.MaxValue);

                const uint Alpha = (uint)byte.MaxValue << 24;
                uint b = val;
                uint g = val << 8;
                uint r = val << 16;

                val = b | g | r | Alpha;


                for (int y = 0; y < height; y++)
                {
                    int index = y * PixelWidth + x;
                    Unsafe.Add(ref screenPtr, index) = val;
                }
            }
        }

        private void DebugPortal(
            Span<BGRA> screen,
            Span<RenderWindow> renderedArea)
        {
            for (int x = 0; x < PixelWidth; x++)
            {
                ref RenderWindow rendered = ref renderedArea[x];

                if (!rendered.Calculated)
                {
                    for (int y = 0; y < PixelHeight; y++)
                    {
                        screen[y * PixelWidth + x] = BGRA.Red;
                    }

                    continue;
                }


                if (x % 2 == 0)
                {
                    Render(screen, rendered.CeilingStart, x, BGRA.Red);
                    Render(screen, rendered.FloorEnd, x, BGRA.Blue);
                }
                else
                {
                    Render(screen, rendered.WallStart, x, BGRA.Green);
                    Render(screen, rendered.WallEnd, x, BGRA.Yellow);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Render(Span<BGRA> screen, int y, int x, BGRA color)
            {
                int index = (y - 1) * PixelWidth + x;

                if (index > 0 && index < screen.Length)
                {
                    screen[index] = color;
                }

                index += PixelWidth;
                if (index > 0 && index < screen.Length)
                {
                    screen[index] = color;
                }

                index += PixelWidth;
                if (index > 0 && index < screen.Length)
                {
                    screen[index] = color;
                }
            }
        }

    }
}
