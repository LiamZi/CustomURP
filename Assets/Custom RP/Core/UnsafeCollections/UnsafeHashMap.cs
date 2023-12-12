using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Core
{
    public unsafe partial struct UnsafeHashMap
    {
        UnsafeHashCollection _collection;
        int _valueOffset;

        public static int Capacity(UnsafeHashMap* map)
        {
            return map->_collection._enteries._length;
        }

        public static int Count(UnsafeHashMap* map)
        {
            return map->_collection._usedCount - map->_collection._freeCount;
        }

        public static void Clear(UnsafeHashMap* map)
        {
            UnsafeHashCollection.Clear(&map->_collection);
        }

        public static UnsafeHashMap* Allocate<K, V>(int capacity, bool fixedSize = false)
            // where K : unmanaged, IEquatable<K>
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            return Allocate(capacity, sizeof(K), sizeof(V), fixedSize);
        }

        public static UnsafeHashMap* Allocate(int capacity, int keyStride, int valueStride, bool fixedSize = false)
        {
            
            int entryStride = sizeof(UnsafeHashCollection.Entry);
            capacity = UnsafeHashCollection.GetNextPrime(capacity);
            
// #if UNITY_WINDOW
             // Assert.Check(entryStride == 16);
// #elif UNITY_ANDROID
             // Assert.Check(entryStride == 12);
// #endif

            var keyAlignment = Native.GetAlignment(keyStride);
            var valueAlignment = Native.GetAlignment(valueStride);

            var alignment = Math.Max(UnsafeHashCollection.Entry.ALIGNMENT, Math.Max(keyAlignment, valueAlignment));
            keyStride = Native.RoundToAlignment(keyStride, alignment);
            valueStride = Native.RoundToAlignment(valueStride, alignment);
            entryStride = Native.RoundToAlignment(sizeof(UnsafeHashCollection.Entry), alignment);

            UnsafeHashMap* map;

            if (fixedSize)
            {
                var sizeOfHeader = Native.RoundToAlignment(sizeof(UnsafeHashMap), alignment);
                var sizeOfBucketsBuffer =
                    Native.RoundToAlignment(sizeof(UnsafeHashCollection.Entry**) * capacity, alignment);
                var sizeOfEntiresBuffer = (entryStride + keyStride + valueStride) * capacity;

                var ptr = Native.MallocAndClear(sizeOfHeader + sizeOfBucketsBuffer + sizeOfEntiresBuffer, alignment);
                map = (UnsafeHashMap*)ptr;
                map->_collection._buckets = (UnsafeHashCollection.Entry**)((byte*)ptr + sizeOfHeader);
                UnsafeBuffer.InitFixed(&map->_collection._enteries, (byte*)ptr + (sizeOfHeader + sizeOfBucketsBuffer),
                    capacity, entryStride + keyStride + valueStride);
            }
            else
            {
                map = Native.MallocAndClear<UnsafeHashMap>();
                map->_collection._buckets = (UnsafeHashCollection.Entry**)Native.MallocAndClear(
                    sizeof(UnsafeHashCollection.Entry**) * capacity, sizeof(UnsafeHashCollection.Entry**));

                UnsafeBuffer.InitDynamic(&map->_collection._enteries, capacity, entryStride + keyStride + valueStride);
            }

            map->_collection._freeCount = 0;
            map->_collection._usedCount = 0;
            map->_collection._keyOffset = entryStride;
            map->_valueOffset = entryStride + keyStride;

            return map;
        }

        public static void Free(UnsafeHashMap* map)
        {
            if (map->_collection._enteries._dynamic == 1)
            {
                UnsafeHashCollection.Free(&map->_collection);
            }

            Native.Free(map);
        }

        public static Iterator<K, V> GetIterator<K, V>(UnsafeHashMap* map)
            where K : unmanaged
            where V : unmanaged
        {
            return new Iterator<K, V>(map);
        }

        public static bool ContainsKey<K>(UnsafeHashMap* map, K key)
            where K : unmanaged,
            IEquatable<K>
        {
            return UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode()) != null;
        }

        public static void AddOrGet<K, V>(UnsafeHashMap* map, K key, ref V value)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            var hash = key.GetHashCode();
            var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, hash);
            if (entry == null)
            {
                entry = UnsafeHashCollection.Insert<K>(&map->_collection, key, hash);
                *(V*)GetValue(map, entry) = value;
            }
            else
            {
                value = *(V*)GetValue(map, entry);
            }
        }

        public static void Add<K, V>(UnsafeHashMap* map, K key, V value)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            var hash = key.GetHashCode();
            var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, hash);
            if (entry == null)
            {
                // insert new entry for key
                entry = UnsafeHashCollection.Insert<K>(&map->_collection, key, hash);

                // assign value to entry
                *(V*)GetValue(map, entry) = value;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static void Set<K, V>(UnsafeHashMap* map, K key, V value)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            var hash = key.GetHashCode();
            var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, hash);
            if (entry == null)
            {
                entry = UnsafeHashCollection.Insert<K>(&map->_collection, key, hash);
            }

            *(V*)GetValue(map, entry) = value;
        }


        public static V Get<K, V>(UnsafeHashMap* map, K key)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode());
            if (entry == null)
            {
                throw new KeyNotFoundException(key.ToString());
            }

            return *(V*)GetValue(map, entry);
        }

        public static V* GetPtr<K, V>(UnsafeHashMap* map, K key)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode());
            if (entry == null)
            {
                throw new KeyNotFoundException(key.ToString());
            }

            return (V*)GetValue(map, entry);
        }

        public static bool TryGetValue<K, V>(UnsafeHashMap* map, K key, out V value)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode());
            if (entry != null)
            {
                value = *(V*)GetValue(map, entry);
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetValuePtr<K, V>(UnsafeHashMap* map, K key, out V* value)
            where K : unmanaged, IEquatable<K>, IEnumerable<K>
            where V : unmanaged
        {
            var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode());
            if (entry != null)
            {
                value = (V*)GetValue(map, entry);
                return true;
            }

            value = null;
            return false;
        }

        public static bool Remove<K>(UnsafeHashMap* map, K key)
            where K : unmanaged, IEquatable<K>
        {
            return UnsafeHashCollection.Remove<K>(&map->_collection, key, key.GetHashCode());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void* GetValue(UnsafeHashMap* map, UnsafeHashCollection.Entry* entry)
        {
            return (byte*)entry + map->_valueOffset;
        }
    };
}