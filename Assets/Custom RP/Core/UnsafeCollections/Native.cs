﻿using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Core
{
    public static unsafe class Native
    {
        public const int CACHE_LINE_SIZE = 64;

        public static void MemMove(void* destination, void* source, int size)
        {
            UnsafeUtility.MemMove(destination, source, size);
        }

        public static void MemCpy(void* destination, void* source, int size)
        {
            UnsafeUtility.MemCpy(destination, source, size);
        }

        public static void MemClear(void* ptr, long size)
        {
            UnsafeUtility.MemClear(ptr, size);
        }

        public static void* Malloc(long size, int alignment = 8,
            Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent)
        {
            return UnsafeUtility.Malloc(size, alignment, allocator);
        }

        public static void Free(void* memory,
            Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent)
        {
            UnsafeUtility.Free(memory, allocator);
        }

        public static void* MallocAndClear(int size, int alignment = 8,
            Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent)
        {
            var memory = UnsafeUtility.Malloc(size, alignment, allocator);
            UnsafeUtility.MemClear(memory, size);
            return memory;
        }

        public static void* MallocAndClear(long size, int alignment = 8,
            Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent)
        {
            var memory = UnsafeUtility.Malloc(size, alignment, allocator);
            UnsafeUtility.MemClear(memory, size);
            return memory;
        }

        public static T* MallocAndClear<T>(
            Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : unmanaged
        {
            var memory = UnsafeUtility.Malloc(sizeof(T), GetAlignment<T>(), allocator);
            UnsafeUtility.MemClear(memory, sizeof(T));
            return (T*)memory;
        }

        public static T* Malloc<T>(Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent)
            where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(sizeof(T), GetAlignment<T>(), allocator);
        }

        public static T* MallocAndClearArray<T>(int length) where T : unmanaged
        {
            var ptr = Malloc(sizeof(T) * length, GetAlignment<T>());
            MemClear(ptr, sizeof(T) * length);
            return (T*)ptr;
        }

        public static void ArrayCopy(void* source, int sourceIndex, void* destination, int destinationIndex, int count,
            int elementStride)
        {
            MemCpy(((byte*)destination) + (destinationIndex * elementStride),
                ((byte*)source) + (sourceIndex * elementStride), count * elementStride);
        }

        public static void ArrayClear<T>(T* ptr, int size) where T : unmanaged
        {
            MemClear(ptr, sizeof(T) * size);
        }

        public static void* Expand(void* buffer, int currentSize, int newSize)
        {
            Assert.Check(newSize > currentSize);

            var oldBuffer = buffer;
            var newBuffer = MallocAndClear(newSize);

            // copy old contents
            MemCpy(newBuffer, oldBuffer, currentSize);

            // free old buffer
            Free(oldBuffer);

            // return the new size
            return newBuffer;
        }

        public static void MemCpyFast(void* d, void* s, int size)
        {
            switch (size)
            {
                case 4:
                    *(uint*)d = *(uint*)s;
                    break;

                case 8:
                    *(ulong*)d = *(ulong*)s;
                    break;

                case 12:
                    *((ulong*)d) = *((ulong*)s);
                    *(((uint*)d) + 2) = *(((uint*)s) + 2);
                    break;

                case 16:
                    *((ulong*)d) = *((ulong*)s);
                    *((ulong*)d + 1) = *((ulong*)s + 1);
                    break;

                default:
                    MemCpy(d, s, size);
                    break;
            }
        }

        public static int RoundToAlignment(int stride, int alignment)
        {
            switch (alignment)
            {
                case 1: return stride;
                case 2: return ((stride + 1) >> 1) * 2;
                case 4: return ((stride + 3) >> 2) * 4;
                case 8: return ((stride + 7) >> 3) * 8;
                default:
                    throw new InvalidOperationException($"Invalid Alignment: {alignment}");
            }
        }

        public static int GetAlignment<T>() where T : unmanaged
        {
            return GetAlignment(sizeof(T));
        }

        public static int GetAlignment(int stride)
        {
            if ((stride % 8) == 0)
            {
                return 8;
            }

            if ((stride % 4) == 0)
            {
                return 4;
            }

            return (stride % 2) == 0 ? 2 : 1;
        }

        public static int GetMaxAlignment(int a, int b)
        {
            return Math.Max(GetAlignment(a), GetAlignment(b));
        }

        public static int GetMaxAlignment(int a, int b, int c)
        {
            return Math.Max(GetMaxAlignment(a, b), GetAlignment(c));
        }

        public static int GetMaxAlignment(int a, int b, int c, int d)
        {
            return Math.Max(GetMaxAlignment(a, b, c), GetAlignment(d));
        }

        public static int GetMaxAlignment(int a, int b, int c, int d, int e)
        {
            return Math.Max(GetMaxAlignment(a, b, c, e), GetAlignment(e));
        }
    };
}