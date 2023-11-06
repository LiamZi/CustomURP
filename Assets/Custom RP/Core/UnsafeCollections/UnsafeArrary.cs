using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Core
{
    public unsafe partial struct UnsafeArray
    {
        private const string ARRAY_SIZE_LESS_THAN_ONE = "Array size can't be less than 1";

        [NativeDisableUnsafePtrRestriction] private void* _buffer;
        private int _length;
        private IntPtr _typeHandle;

        public static UnsafeArray* Allocate<T>(int size)
            where T : unmanaged
        {
            if (size < 1)
            {
                throw new InvalidOperationException(ARRAY_SIZE_LESS_THAN_ONE);
            }

            var alignment = Native.GetAlignment(sizeof(T));
            var arrayStructSize = Native.RoundToAlignment(sizeof(UnsafeArray), alignment);
            var arrayMemSize = size * sizeof(T);

            var ptr = Native.MallocAndClear(sizeof(UnsafeArray), alignment);
            UnsafeArray* array;
            array = (UnsafeArray*)ptr;

            array->_buffer = ((byte*)ptr) + arrayStructSize;
            array->_length = size;
            array->_typeHandle = typeof(T).TypeHandle.Value;

            return array;
        }

        public static void Free(UnsafeArray* array)
        {
            Native.Free(array);
        }

        public static IntPtr GetTypeHandle(UnsafeArray* array)
        {
            return array->_typeHandle;
        }

        public static void* GetBuffer(UnsafeArray* array)
        {
            return array->_buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Length(UnsafeArray* array)
        {
            Assert.Check(array != null);
            return array->_length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetPtr<T>(UnsafeArray* array, int index) where T : unmanaged
        {
            Assert.Check(array != null);
            Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)array->_length)
            {
                throw new IndexOutOfRangeException(index.ToString());
            }

            return (T*)array->_buffer + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetPtr<T>(UnsafeArray* array, long index) where T : unmanaged
        {
            Assert.Check(array != null);
            Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)array->_length)
            {
                throw new IndexOutOfRangeException(index.ToString());
            }

            return (T*)array->_buffer + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(UnsafeArray* array, int index) where T : unmanaged
        {
            Assert.Check(array != null);
            Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

            if ((uint)index >= (uint)array->_length)
            {
                throw new IndexOutOfRangeException(index.ToString());
            }

            return *((T*)array->_buffer + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(UnsafeArray* array, long index) where T : unmanaged
        {
            Assert.Check(array != null);
            Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

            if ((uint)index >= (uint)array->_length)
            {
                throw new IndexOutOfRangeException(index.ToString());
            }

            return *((T*)array->_buffer + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<T>(UnsafeArray* array, int index, T value) where T : unmanaged
        {
            Assert.Check(array != null);
            Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)array->_length)
            {
                throw new IndexOutOfRangeException(index.ToString());
            }

            *((T*)array->_buffer + index) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<T>(UnsafeArray* array, long index, T value) where T : unmanaged
        {
            Assert.Check(array != null);
            Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)array->_length)
            {
                throw new IndexOutOfRangeException(index.ToString());
            }

            *((T*)array->_buffer + index) = value;
        }

        public static Iterator<T> GetIterator<T>(UnsafeArray* array) where T : unmanaged
        {
            return new Iterator<T>(array);
        }

        public static void Copy<T>(UnsafeArray* source, int sourceIndex, UnsafeArray* destination, int destinationIndex,
            int count) where T : unmanaged
        {
            Assert.Check(source != null);
            Assert.Check(destination != null);
            Assert.Check(typeof(T).TypeHandle.Value == source->_typeHandle);
            Assert.Check(typeof(T).TypeHandle.Value == destination->_typeHandle);
            Native.MemCpy((T*)destination->_buffer + destinationIndex, (T*)source->_buffer + sourceIndex,
                count * sizeof(T));
        }

        public static int IndexOf<T>(UnsafeArray* array, T item) where T : unmanaged, IEquatable<T>
        {
            Assert.Check(array != null);
            Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);
            for (int i = 0; i < Length(array); ++i)
            {
                if (Get<T>(array, i).Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int LastIndexOf<T>(UnsafeArray* array, T item) where T : unmanaged, IEquatable<T>
        {
            Assert.Check(array != null);
            Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

            for (int i = Length(array) - 1; i >= 0; --i)
            {
                if (Get<T>(array, i).Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int FindIndex<T>(UnsafeArray* array, Func<T, bool> predicate) where T : unmanaged
        {
            Assert.Check(array != null);
            Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

            for (int i = 0; i < Length(array); ++i)
            {
                if (predicate(Get<T>(array, i)))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int FindLastIndex<T>(UnsafeArray* array, Func<T, bool> predicate) where T : unmanaged
        {
            Assert.Check(array != null);
            Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

            for (int i = Length(array) - 1; i >= 0; --i)
            {
                if (predicate(Get<T>(array, i)))
                {
                    return i;
                }
            }

            return -1;
        }
    };
}