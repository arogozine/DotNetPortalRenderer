using RenderingEngine.Tooling;
using SoftwareRendererModels;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        [Conditional("DEBUG")]
        private void ProperlyClamped(scoped ReadOnlySpan<int> span)
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            for (int i = 0; i < span.Length; i++)
            {
                RenderColumnStatus statusY = status[i];

                int val = span[i];
                Debug.Assert(val >= 0);
                Debug.Assert(val < PixelHeight);
            }
        }

        [Conditional("DEBUG")]
        private void ProperlyClamped()
        {
            ProperlyClamped(memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart));
            ProperlyClamped(memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd));
            ProperlyClamped(memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped));
            ProperlyClamped(memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped));
        }

        [Conditional("DEBUG")]
        private void ProperlyClamped(scoped ReadOnlySpan<int> span, int from, int to)
        {
            Debug.Assert(from <= to);


            for (int i = from; i <= to; i++)
            {
                int val = span[i];
                Debug.Assert(val >= 0);
                Debug.Assert(val < PixelHeight);
            }
        }

        [Conditional("DEBUG")]
        private void ProperlyClamped(int from, int to)
        {
            ProperlyClamped(memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart), from, to);
            ProperlyClamped(memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd), from, to);
            ProperlyClamped(memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped), from, to);
            ProperlyClamped(memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped), from, to);
        }


        [Conditional("DEBUG")]
        private unsafe void RenderOutline(int* from, int* to, BGRA topColor, BGRA bottomColor, int fromX = 0, int toX = int.MaxValue)
        {
            ref BGRA screen = ref Unsafe.AsRef<BGRA>(buffer);

            int length = PixelHeight * PixelWidth;

            fromX = int.Max(0, fromX);
            toX = int.Min(PixelWidth, toX);

            for (int x = fromX; x < toX; x++)
            {
                int ceiling = from[x];
                int floor = to[x];

                Render(ref screen, ceiling, x, topColor);
                Render(ref screen, floor, x, bottomColor);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Render(ref BGRA screen, int y, int x, BGRA color)
            {
                int index = (y - 1) * PixelWidth + x;

                if (index > 0 && index < length)
                {
                    Unsafe.Add(ref screen, index) = color;
                }

                index += PixelWidth;
                if (index > 0 && index < length)
                {
                    Unsafe.Add(ref screen, index) = color;
                }

                index += PixelWidth;
                if (index > 0 && index < length)
                {
                    Unsafe.Add(ref screen, index) = color;
                }
            }
        }


        [Conditional("DEBUG")]
        private unsafe void RenderOutline(Span<int> from, Span<int> to, BGRA topColor, BGRA bottomColor, int fromX = 0, int toX = int.MaxValue)
        {
            ref BGRA screen = ref Unsafe.AsRef<BGRA>(buffer);

            int length = PixelHeight * PixelWidth;

            fromX = int.Max(0, fromX);
            toX = int.Min(PixelWidth, toX);

            for (int x = fromX; x < toX; x++)
            {
                int ceiling = from[x];
                int floor = to[x];

                Render(ref screen, ceiling, x, topColor);
                Render(ref screen, floor, x, bottomColor);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Render(ref BGRA screen, int y, int x, BGRA color)
            {
                int index = (y - 1) * PixelWidth + x;

                if (index > 0 && index < length)
                {
                    Unsafe.Add(ref screen, index) = color;
                }

                index += PixelWidth;
                if (index > 0 && index < length)
                {
                    Unsafe.Add(ref screen, index) = color;
                }

                index += PixelWidth;
                if (index > 0 && index < length)
                {
                    Unsafe.Add(ref screen, index) = color;
                }
            }
        }
    }
}
