using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            return Allocate(capacity, sizeof(K), sizeof(V), fixedSize);
        }

        public static UnsafeHashMap* Allocate(int capacity, int keyStride, int valueStride, bool fixedSize = false)
        {
            var entryStride = sizeof(UnsafeHashCollection.Entry);
            capacity = UnsafeHashCollection.GetNextPrime(capacity);
            Assert.Check(entryStride == 16);

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
        
        
    };
}