using System;
using System.Collections;
using System.Collections.Generic;

namespace Core
{
    public partial struct UnsafeList
    {
        public unsafe struct Iterator<T> : IUnsafeIterator<T>
            where T : unmanaged
        {
            T* _current;
            int _index;
            int _count;
            int _offset;
            UnsafeBuffer _buffer;

            internal Iterator(UnsafeBuffer buffer, int offset, int count)
            {
                _index = -1;
                _count = count;
                _offset = offset;
                _buffer = buffer;
                _current = null;
            }

            public bool MoveNext()
            {
                if (++_index < _count)
                {
                    _current = (T*)UnsafeBuffer.Element(_buffer._ptr, (_offset + _index) % _buffer._length,
                        _buffer._stride);
                    return true;
                }

                _current = null;
                return false;
            }

            public void Reset()
            {
                _index = -1;
                _current = null;
            }

            public T Current
            {
                get
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return *_current;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
            }

            public Iterator<T> GetEnumerator()
            {
                return this;
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        };
    };
}