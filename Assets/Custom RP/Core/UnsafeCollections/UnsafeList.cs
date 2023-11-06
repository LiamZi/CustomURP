using System;
using UnityEditor.Searcher;


namespace Core
{
    public unsafe partial struct UnsafeList
    {
        const string LIST_FULL = "Fixed size list is full";
        const string LIST_FIXED_CANT_CHANGE_CAPACITY = "Fixed size list can't change its capacity";

        const string LIST_INIT_TOO_SMALL =
            "Pointer length for must be large enough to contain both header and at least 1 item";

        private UnsafeBuffer _items;
        private int _count;

        public static UnsafeList* Allocate<T>(int capacity, bool fixedSize = false)
            where T : unmanaged
        {
            return Allocate(capacity, sizeof(T), fixedSize);
        }

        public static UnsafeList* Allocate(int capacity, int stride, bool fixedSize = false)
        {
            Assert.Check(capacity > 0);
            Assert.Check(stride > 0);

            UnsafeList* list;

            if (fixedSize)
            {
                var alignment = Native.GetAlignment(stride);
                var sizeOfHeader = Native.RoundToAlignment(sizeof(UnsafeList), alignment);
                var sizeofBuffer = stride * capacity;

                var ptr = Native.MallocAndClear(sizeOfHeader + sizeofBuffer);
                list = (UnsafeList*)ptr;
                UnsafeBuffer.InitFixed(&list->_items, (byte*)ptr + sizeOfHeader, capacity, stride);
            }
            else
            {
                list = Native.MallocAndClear<UnsafeList>();
                UnsafeBuffer.InitDynamic(&list->_items, capacity, stride);
            }

            list->_count = 0;
            return list;
        }

        public static void Free(UnsafeList* list)
        {
            Native.Free(list);
        }

        public static int Count(UnsafeList* list)
        {
            Assert.Check(list != null);
            return list->_count;
        }

        public static void Clear(UnsafeList* list)
        {
            Assert.Check(list != null);
            list->_count = 0;
        }

        public static int Capacity(UnsafeList* list)
        {
            Assert.Check(list != null);
            return list->_items._length;
        }

        public static bool IsFixedSize(UnsafeList* list)
        {
            Assert.Check(list != null);
            return list->_items._dynamic == 0;
        }

        public static void SetCapacity(UnsafeList* list, int capacity)
        {
            Assert.Check(list != null);

            if (list->_items._dynamic == 0)
            {
                throw new InvalidOperationException(LIST_FIXED_CANT_CHANGE_CAPACITY);
            }

            if (capacity == list->_items._length)
            {
                return;
            }

            if (capacity <= 0)
            {
                list->_count = 0;
                if (list->_items._ptr != null)
                {
                    UnsafeBuffer.Free(&list->_items);
                }

                return;
            }

            UnsafeBuffer newItems = default;
            UnsafeBuffer.InitDynamic(&newItems, capacity, list->_items._stride);
            if (list->_count > 0)
            {
                if (list->_count > capacity)
                {
                    list->_count = capacity;
                }

                UnsafeBuffer.Copy(list->_items, 0, newItems, 0, list->_count);

            }

            if (list->_items._ptr != null)
            {
                UnsafeBuffer.Free(&list->_items);
            }

            list->_items = newItems;
        }

        public static void Add<T>(UnsafeList* list, T item)
            where T : unmanaged
        {
            Assert.Check(list != null);

            var count = list->_count;
            var items = list->_items;

            if (count < items._length)
            {
                *(T*)UnsafeBuffer.Element(items._ptr, count, items._stride) = item;
                list->_count = count + 1;
                return;
            }

            if (list->_items._dynamic == 0)
            {
                throw new InvalidOperationException(LIST_FULL);
            }

            SetCapacity(list, Math.Max(2, items._length * 2));
            items = list->_items;
            Assert.Check(count < items._length);
            *(T*)UnsafeBuffer.Element(items._ptr, count, items._stride) = item;
            list->_count = count + 1;
        }

        public static T Get<T>(UnsafeList* list, int index)
            where T : unmanaged
        {
            Assert.Check(list != null);

            if ((uint)index >= (uint)list->_count)
            {
                throw new IndexOutOfRangeException();
            }

            var items = list->_items;
            return *(T*)UnsafeBuffer.Element(items._ptr, index, items._stride);
        }

        public static T* GetPtr<T>(UnsafeList* list, int index)
            where T : unmanaged
        {
            Assert.Check(list != null);
            if ((uint)index >= (uint)list->_count)
            {
                throw new IndexOutOfRangeException();
            }

            var items = list->_items;
            return (T*)UnsafeBuffer.Element(items._ptr, index, items._stride);
        }

        public static void RemoveAt(UnsafeList* list, int index)
        {
            Assert.Check(list != null);
            var count = list->_count;

            if ((uint)index >= (uint)count)
            {
                throw new IndexOutOfRangeException();
            }

            list->_count = count;
            if (index < count)
            {
                UnsafeBuffer.Move(list->_items, index + 1, index, count - index);
            }
        }

        public static void RemoveAtUnordered(UnsafeList* list, int index)
        {
            Assert.Check(list != null);

            var count = list->_count;

            if ((uint)index >= (uint)count)
            {
                throw new IndexOutOfRangeException();
            }

            list->_count = --count;

            if (index < count)
            {
                UnsafeBuffer.Move(list->_items, index + 1, index, 1);
            }
        }

        public static int IndexOf<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            Assert.Check(list != null);

            var count = list->_count;
            var items = list->_items;

            for (int i = 0; i < count; ++i)
            {
                var cmp = *(T*)UnsafeBuffer.Element(items._ptr, i, items._stride);
                if (cmp.Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int LastIndexOf<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            Assert.Check(list != null);

            var count = list->_count;
            var items = list->_items;

            for (int i = count - 1; i >= 0; --i)
            {
                var cmp = *(T*)UnsafeBuffer.Element(items._ptr, i, items._stride);
                if (cmp.Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool Remove<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            Assert.Check(list != null);

            int index = IndexOf<T>(list, item);
            if (index < 0)
            {
                return false;
            }

            RemoveAt(list, index);
            return true;
        }

        public static bool RemoveUnordered<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            Assert.Check(list != null);

            int index = IndexOf<T>(list, item);
            if (index < 0)
            {
                return false;
            }

            RemoveAtUnordered(list, index);
            return true;
        }

        public static Iterator<T> GetIterator<T>(UnsafeList* list) 
            where T : unmanaged
        {
            return new Iterator<T>(list->_items, 0, list->_count);
        }
    };
};