using RenderingEngine.Models;
using System.Buffers;

namespace RenderingEngine.Engine
{
    internal sealed class TempBuffer<T> : IDisposable
        where T : unmanaged
    {
        public int Index { get; set; } = -1;
        private readonly int _length;
        private readonly T[] _buffer;

        public ref T Pointer => ref MemoryMarshal.GetArrayDataReference(_buffer);

        public Span<T> Span => _buffer.AsSpan(0, _length);

        public TempBuffer(int length)
        {
            _length = length;
            _buffer = ArrayPool<T>.Shared.Rent(length);
        }

        public static implicit operator Span<T>(TempBuffer<T> buffer) => buffer.Span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempBuffer<T> GetBuffer(int length) => new(length);

        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(_buffer);
            GC.SuppressFinalize(this);
        }

        ~TempBuffer() => ArrayPool<T>.Shared.Return(_buffer);
    }
}
