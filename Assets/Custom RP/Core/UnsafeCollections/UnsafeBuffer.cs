using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;


namespace Core
{
    unsafe struct UnsafeBuffer
    {
        [NativeDisableUnsafePtrRestriction] public void* _ptr;

        public int _length;
        public int _stride;
        public int _dynamic;

        public static void Free(UnsafeBuffer* buffer)
        {
            Assert.Check(buffer != null);

            if (buffer->_dynamic == 0)
            {
                throw new InvalidOperationException("Can't free a fixed buffer");
            }

            Assert.Check(buffer->_ptr != null);
            Native.Free(buffer->_ptr);

            *buffer = default;
        }

        public static void Clear(UnsafeBuffer* buffer)
        {
            Native.MemClear(buffer->_ptr, buffer->_length * buffer->_stride);
        }

        public static void InitFixed(UnsafeBuffer* buffer, void* ptr, int length, int stride)
        {
            Assert.Check(buffer != null);
            Assert.Check(ptr != null);
            Assert.Check(length > 0);
            Assert.Check(stride > 0);

            Assert.Check((((IntPtr)ptr).ToInt64() % Native.GetAlignment(stride)) == 0);

            buffer->_ptr = ptr;
            buffer->_length = length;
            buffer->_stride = stride;
            buffer->_dynamic = 0;
        }

        public static void InitDynamic<T>(UnsafeBuffer* buffer, int length) where T : unmanaged
        {
            InitDynamic(buffer, length, sizeof(T));
        }

        public static void InitDynamic(UnsafeBuffer* buffer, int length, int stride)
        {
            Assert.Check(buffer != null);
            Assert.Check(length > 0);
            Assert.Check(stride > 0);

            buffer->_ptr = Native.MallocAndClear(length * stride, Native.GetAlignment(stride));
            buffer->_length = length;
            buffer->_stride = stride;
            buffer->_dynamic = 1;
        }

        public static void Copy(UnsafeBuffer source, int sourceIndex, UnsafeBuffer destination, int destinationIndex,
            int count)
        {
            Assert.Check(source._ptr != null);
            Assert.Check(source._ptr != destination._ptr);
            Assert.Check(source._stride == destination._stride);
            Assert.Check(source._stride > 0);
            Assert.Check(destination._ptr != null);
            Native.MemCpy((byte*)destination._ptr + (destinationIndex * source._stride),
                (byte*)source._ptr + (sourceIndex * source._stride), count * source._stride);
        }

        public static void Move(UnsafeBuffer source, int fromIndex, int toIndex, int count)
        {
            Assert.Check(source._ptr != null);
            Native.MemMove((byte*)source._ptr + (toIndex * source._stride),
                (byte*)source._ptr + (fromIndex * source._stride), count * source._stride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Element(void* bufferPtr, int index, int stride)
        {
            return (byte*)bufferPtr + (index * stride);
        }
    };
}