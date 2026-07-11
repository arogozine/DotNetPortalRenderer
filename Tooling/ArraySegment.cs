// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Modified by Alexandre Rogozine
// to include Span<T> support

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Tooling
{
    [DebuggerDisplay("Count = {Count}")]
    public readonly struct ArraySegment<T> : IList<T>, IReadOnlyList<T>
    {
        public static readonly ArraySegment<T> Empty = new ArraySegment<T>([]);

        private readonly T[]? _array;
        private readonly int _offset;
        private readonly int _count;

        public ArraySegment(T[] array)
        {
            ArgumentNullException.ThrowIfNull(array);

            _array = array;
            _offset = 0;
            _count = array.Length;
        }

        public ArraySegment(T[] array, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if ((uint)offset > (uint)array.Length || (uint)count > (uint)(array.Length - offset))
                throw new ArgumentException("Offset and count were out of bounds for the array.");

            _array = array;
            _offset = offset;
            _count = count;
        }

        public T[]? Array => _array;

        public int Offset => _offset;

        public int Count => _count;

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _array![_offset + index];
            }
            set
            {
                if ((uint)index >= (uint)_count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                _array![_offset + index] = value;
            }
        }

        public Enumerator GetEnumerator()
        {
            ThrowInvalidOperationIfDefault();
            return new Enumerator(this);
        }

        public override int GetHashCode() =>
            _array is null ? 0 : HashCode.Combine(_offset, _count, _array.GetHashCode());

        public void CopyTo(T[] destination) => CopyTo(destination, 0);

        public void CopyTo(T[] destination, int destinationIndex)
        {
            ThrowInvalidOperationIfDefault();
            System.Array.Copy(_array!, _offset, destination, destinationIndex, _count);
        }

        public void CopyTo(ArraySegment<T> destination)
        {
            ThrowInvalidOperationIfDefault();
            destination.ThrowInvalidOperationIfDefault();

            if (_count > destination._count)
            {
                throw new ArgumentException("Destination is too short.", nameof(destination));
            }

            System.Array.Copy(_array!, _offset, destination._array!, destination._offset, _count);
        }

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is ArraySegment<T> other && Equals(other);

        public bool Equals(ArraySegment<T> obj) =>
            obj._array == _array && obj._offset == _offset && obj._count == _count;

        public ArraySegment<T> Slice(int index)
        {
            ThrowInvalidOperationIfDefault();

            if ((uint)index > (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return new ArraySegment<T>(_array!, _offset + index, _count - index);
        }

        public ArraySegment<T> Slice(int index, int count)
        {
            ThrowInvalidOperationIfDefault();

            if ((uint)index > (uint)_count || (uint)count > (uint)(_count - index))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return new ArraySegment<T>(_array!, _offset + index, count);
        }

        public T[] ToArray()
        {
            ThrowInvalidOperationIfDefault();

            if (_count == 0)
            {
                return Empty._array!;
            }

            var array = new T[_count];
            System.Array.Copy(_array!, _offset, array, 0, _count);
            return array;
        }

        public static bool operator ==(ArraySegment<T> a, ArraySegment<T> b) => a.Equals(b);

        public static bool operator !=(ArraySegment<T> a, ArraySegment<T> b) => !(a == b);

        public static implicit operator ArraySegment<T>(T[]? array) =>
            array != null ? new ArraySegment<T>(array) : default;

        public Span<T> AsSpan()
        {
            ThrowInvalidOperationIfDefault();
            return new Span<T>(_array, _offset, _count);
        }

        public Span<T> AsSpan(int start)
        {
            ThrowInvalidOperationIfDefault();

            if ((uint)start > (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            return new Span<T>(_array, _offset + start, _count - start);
        }

        public Span<T> AsSpan(int start, int length)
        {
            ThrowInvalidOperationIfDefault();

            if ((uint)start > (uint)_count || (uint)length > (uint)(_count - start))
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            return new Span<T>(_array, _offset + start, length);
        }

        public Memory<T> AsMemory() => new(_array, _offset, _count);

        public static implicit operator Span<T>(ArraySegment<T> segment) => new(segment._array, segment._offset, segment._count);

        public static implicit operator ReadOnlySpan<T>(ArraySegment<T> segment) => new(segment._array, segment._offset, segment._count);

        public static implicit operator Memory<T>(ArraySegment<T> segment) => new(segment._array, segment._offset, segment._count);

        public static implicit operator ReadOnlyMemory<T>(ArraySegment<T> segment) => new(segment._array, segment._offset, segment._count);

        #region IList<T>

        T IList<T>.this[int index]
        {
            get
            {
                ThrowInvalidOperationIfDefault();
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _array![_offset + index];
            }

            set
            {
                ThrowInvalidOperationIfDefault();
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                _array![_offset + index] = value;
            }
        }

        int IList<T>.IndexOf(T item)
        {
            ThrowInvalidOperationIfDefault();

            int index = System.Array.IndexOf(_array!, item, _offset, _count);

            Debug.Assert(index < 0 ||
                         (index >= _offset && index < _offset + _count));

            return index >= 0 ? index - _offset : -1;
        }

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

        #endregion

        #region IReadOnlyList<T>

        T IReadOnlyList<T>.this[int index]
        {
            get
            {
                ThrowInvalidOperationIfDefault();
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _array![_offset + index];
            }
        }

        #endregion IReadOnlyList<T>

        #region ICollection<T>

        bool ICollection<T>.IsReadOnly =>
            // the indexer setter does not throw an exception although IsReadOnly is true.
            // This is to match the behavior of arrays.
            true;

        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        void ICollection<T>.Clear() => throw new NotSupportedException();

        bool ICollection<T>.Contains(T item)
        {
            ThrowInvalidOperationIfDefault();

            int index = System.Array.IndexOf(_array!, item, _offset, _count);

            Debug.Assert(index < 0 ||
                         (index >= _offset && index < _offset + _count));

            return index >= 0;
        }

        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

        #endregion

        #region IEnumerable<T>

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            ThrowInvalidOperationIfDefault();
            return new Enumerator(this);
        }

        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        #endregion

        private void ThrowInvalidOperationIfDefault()
        {
            if (_array == null)
            {
                throw new InvalidOperationException("Operation is not valid due to the current state of the object (array is null).");
            }
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[]? _array;
            private readonly int _start;
            private readonly int _end; // cache Offset + Count, since it's a little slow
            private int _current;

            internal Enumerator(ArraySegment<T> arraySegment)
            {
                Debug.Assert(arraySegment.Array != null);
                Debug.Assert(arraySegment.Offset >= 0);
                Debug.Assert(arraySegment.Count >= 0);
                Debug.Assert(arraySegment.Offset + arraySegment.Count <= arraySegment.Array.Length);

                _array = arraySegment.Array;
                _start = arraySegment.Offset;
                _end = arraySegment.Offset + arraySegment.Count;
                _current = arraySegment.Offset - 1;
            }

            public bool MoveNext()
            {
                if (_current < _end)
                {
                    _current++;
                    return _current < _end;
                }

                return false;
            }

            public readonly T Current
            {
                get
                {
                    if (_current < _start)
                        throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
                    if (_current >= _end)
                        throw new InvalidOperationException("Enumeration already finished.");
                    return _array![_current];
                }
            }

            readonly object? IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                _current = _start - 1;
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
