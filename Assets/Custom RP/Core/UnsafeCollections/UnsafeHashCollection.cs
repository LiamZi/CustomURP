using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;

namespace Core
{
    unsafe struct UnsafeHashCollection
    {
        public enum EntryState
        {
            None = 0,
            Free = 1,
            Used = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Entry
        {
            public const int ALIGNMENT = 8;
            public Entry* _next;
            public int _hash;
            public EntryState _state;
        };

        public Entry** _buckets;
        public Entry* _freeHead;
        public UnsafeBuffer _enteries;
        public int _usedCount;
        public int _freeCount;
        public int _keyOffset;

        public struct Iterator
        {
            private int _index;
            public Entry* _current;
            public UnsafeHashCollection* _collection;

            public Iterator(UnsafeHashCollection* collection)
            {
                _index = -1;
                _current = null;
                _collection = collection;
            }

            public bool Next()
            {
                while (++_index < _collection->_usedCount)
                {
                    var entry = UnsafeHashCollection.GetEntry(_collection, _index);
                    if (entry->_state != EntryState.Used) continue;

                    _current = entry;
                    return true;
                }

                _current = null;
                return false;
            }

            public void Reset()
            {
                _index = -1;
            }
        };

        static readonly int[] _primeTable = new[]
        {
            3,
            7,
            17,
            29,
            53,
            97,
            193,
            389,
            769,
            1543,
            3079,
            6151,
            12289,
            24593,
            49157,
            98317,
            196613,
            393241,
            786433,
            1572869,
            3145739,
            6291469,
            12582917,
            25165843,
            50331653,
            100663319,
            201326611,
            402653189,
            805306457,
            1610612741
        };

        public static int GetNextPrime(int value)
        {
            for (int i = 0; i < _primeTable.Length; ++i)
            {
                if (_primeTable[i] > value)
                {
                    return _primeTable[i];
                }
            }

            throw new InvalidOperationException(
                $"HashCollection can't get larger than {_primeTable[_primeTable.Length - 1]}");
        }

        public static void Free(UnsafeHashCollection* collection)
        {
            Assert.Check(collection->_enteries._dynamic == 1);
            Native.Free(collection->_buckets);
            Native.Free(collection->_enteries._ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entry* GetEntry(UnsafeHashCollection* collection, int index)
        {
            return (Entry*)UnsafeBuffer.Element(collection->_enteries._ptr, index, collection->_enteries._stride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetKey<T>(UnsafeHashCollection* collection, Entry* entry) where T : unmanaged
        {
            return *(T*)((byte*)entry + collection->_keyOffset);
        }

        public static Entry* Find<T>(UnsafeHashCollection* collection, T value, int valueHash) where T : unmanaged, IEquatable<T> {
            var index = Math.Abs(valueHash) % collection->_enteries._length;
            var bucketHead = collection->_buckets[index];

            while (bucketHead != null) {
                var tmp = *(T*)((byte*)bucketHead + collection->_keyOffset);
                if (bucketHead->_hash == valueHash && value.Equals(tmp))
                {
                    return bucketHead;
                }
                else {
                    bucketHead = bucketHead->_next;
                }
            }

            return null;
        }

        public static bool Remove<T>(UnsafeHashCollection* collection, T value, int valueHash)
            where T : unmanaged, IEquatable<T>
        {
            var bucketHash = valueHash % collection->_enteries._length;
            var bucketHead = collection->_buckets[valueHash % collection->_enteries._length];
            var bucketPrev = default(Entry*);

            while (bucketHead != null)
            {
                if (bucketHead->_hash == valueHash && value.Equals(*(T*)((byte*)bucketHead + collection->_keyOffset)))
                {
                    if (bucketPrev == null)
                    {
                        collection->_buckets[bucketHash] = bucketHead->_next;
                    }
                    else
                    {
                        bucketPrev->_next = bucketHead->_next;
                    }

                    Assert.Check(bucketHead->_state == EntryState.Used);

                    bucketHead->_next = collection->_freeHead;
                    bucketHead->_state = EntryState.Free;

                    collection->_freeHead = bucketHead;
                    collection->_freeCount = collection->_freeCount + 1;
                    return true;
                }
                else
                {
                    bucketPrev = bucketHead;
                    bucketHead = bucketHead->_next;
                }
            }

            return false;
        }

        public static Entry* Insert<T>(UnsafeHashCollection* collection, T value, int valueHash) where T : unmanaged
        {
            Entry* entry;

            if (collection->_freeHead != null)
            {
                Assert.Check(collection->_freeCount > 0);


                entry = collection->_freeHead;


                collection->_freeHead = entry->_next;
                collection->_freeCount = collection->_freeCount - 1;


                Assert.Check(entry->_state == EntryState.Free);
            }
            else
            {
                if (collection->_usedCount == collection->_enteries._length)
                {
                    // !! IMPORTANT !!
                    // when this happens, it's very important to be
                    // aware of the fact that all pointers to to buckets
                    // or entries etc. are not valid anymore as we have
                    // re-allocated all of the memory
                    Expand(collection);
                }


                entry = (Entry*)UnsafeBuffer.Element(collection->_enteries._ptr, collection->_usedCount,
                    collection->_enteries._stride);


                collection->_usedCount = collection->_usedCount + 1;


                Assert.Check(entry->_state == EntryState.None);
            }


            var bucketHash = valueHash % collection->_enteries._length;


            entry->_hash = valueHash;
            entry->_next = collection->_buckets[bucketHash];
            entry->_state = EntryState.Used;


            *(T*)((byte*)entry + collection->_keyOffset) = value;


            collection->_buckets[bucketHash] = entry;


            return entry;
        }

        public static void Clear(UnsafeHashCollection* collection)
        {
            collection->_freeHead = null;
            collection->_freeCount = 0;
            collection->_usedCount = 0;

            var length = collection->_enteries._length;

            Native.MemClear(collection->_buckets, length * sizeof(Entry**));
            UnsafeBuffer.Clear(&collection->_enteries);
        }

        static void Expand(UnsafeHashCollection* collection)
        {
            Assert.Check(collection->_enteries._dynamic == 1);

            var capacity = GetNextPrime(collection->_enteries._length);

            Assert.Check(capacity >= collection->_enteries._length);

            var newBuckets = (Entry**)Native.MallocAndClear(capacity * sizeof(Entry**), sizeof(Entry**));
            var newEntries = default(UnsafeBuffer);

            UnsafeBuffer.InitDynamic(&newEntries, capacity, collection->_enteries._stride);
            UnsafeBuffer.Copy(collection->_enteries, 0, newEntries, 0, collection->_enteries._length);

            collection->_freeHead = null;
            collection->_freeCount = 0;

            for (int i = collection->_enteries._length - 1; i >= 0; --i)
            {
                var entry = (Entry*)((byte*)newEntries._ptr + (i * newEntries._stride));
                if (entry->_state == EntryState.Used)
                {
                    var bucketHash = entry->_hash % capacity;
                    entry->_next = newBuckets[bucketHash];
                    newBuckets[bucketHash] = entry;
                }


                else if (entry->_state == EntryState.Free)
                {
                    entry->_next = collection->_freeHead;
                    collection->_freeHead = entry;
                    collection->_freeCount = collection->_freeCount + 1;
                }
            }

            Native.Free(collection->_buckets);
            UnsafeBuffer.Free(&collection->_enteries);


            collection->_buckets = newBuckets;
            collection->_enteries = newEntries;
        }
    };
}