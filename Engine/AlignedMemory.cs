using System.Numerics;

namespace RenderingEngine
{
    internal sealed unsafe class AlignedMemory<T> : IDisposable
        where T : unmanaged
    {
        private readonly int _size;
        private readonly nint _pointer;

        public AlignedMemory(int size)
        {
            int overflow = (Vector<float>.Count - size % Vector<float>.Count) + Vector<float>.Count;

            _size = size;
            _pointer = (IntPtr)NativeMemory.AlignedAlloc((uint)((size + overflow) * sizeof(T)), (uint)(Vector<byte>.Count));
        }

        public void Dispose()
        {
            NativeMemory.AlignedFree((void*)_pointer);
            GC.SuppressFinalize(this);
        }

        public static implicit operator Span<T> (AlignedMemory<T> alignedMemory)
        {
            return new Span<T>((void*)alignedMemory._pointer, alignedMemory._size);
        }

        ~AlignedMemory()
        {
            NativeMemory.AlignedFree((void*)_pointer);
        }
    }
}
