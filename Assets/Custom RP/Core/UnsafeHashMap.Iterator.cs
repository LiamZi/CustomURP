using System;
using System.Collections;
using System.Collections.Generic;


namespace Core
{
    public partial struct UnsafeHashMap
    {
        public unsafe struct Iterator<K, V> : IUnsafeIterator<(K key, V value)>
            where K : unmanaged
            where V : unmanaged
        {
            UnsafeHashCollection.Iterator _iterator;
            int _keyOffset;
            int _valueOffset;

            public Iterator(UnsafeHashMap* map)
            {
                _valueOffset = map->_valueOffset;
                _keyOffset = map->_collection._keyOffset;
                _iterator = new UnsafeHashCollection.Iterator(&map->_collection);
            }

            public K CurrentKey
            {
                get
                {
                    if (_iterator._current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return *(K*)((byte*)_iterator._current + _keyOffset);
                }
            }

            public V CurrentValue
            {
                get
                {
                    if (_iterator._current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return *(V*)((byte*)_iterator._current + _valueOffset);
                }
            }

            public (K key, V value) Current
            {
                get
                {
                    return (CurrentKey, CurrentValue);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public bool MoveNext()
            {
                return _iterator.Next();
            }

            public void Reset()
            {
                _iterator.Reset();
            }

            public void Dispose()
            {

            }

            public IEnumerator<(K key, V value)> GetEnumerator()
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