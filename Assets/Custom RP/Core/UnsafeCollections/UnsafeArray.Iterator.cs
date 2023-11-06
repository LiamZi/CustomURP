using System;
using System.Collections;
using System.Collections.Generic;

namespace Core
{
    public partial struct UnsafeArray
    {
        public unsafe struct Iterator<T> : IUnsafeIterator<T> where T : unmanaged
        {
            T* _current;
            int _index;
            UnsafeArray* _array;

            internal Iterator(UnsafeArray* array)
            {
                _index = -1;
                _array = array;
                _current = null;
            }

            public bool MoveNext()
            {
                if (++_index < _array->_length)
                {
                    _current = GetPtr<T>(_array, _index);
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