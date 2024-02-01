using UnityEngine;
namespace Core
{
    public class Array<T>
    {
        public T[] _data;
        public int Length { get; private set; }
        
        public Array(int len)
        {
            Reallocate(len);
        }
        public void Reallocate(int len)
        {
            if (_data != null && len < _data.Length)
                return;
            _data = new T[len];
            Length = 0;
        }
        public void Reset()
        {
            Length = 0;
        }
        
        public void Add(T item)
        {
            if (_data == null || Length >= _data.Length)
            {
                Debug.LogError("Array overflow : " + typeof(T));
            }
            _data[Length] = item;
            ++Length;
        }
        
        public bool Contains(T item)
        {
            for (int i = 0; i < Length; ++i)
            {
                if (_data[i].Equals(item))
                    return true;
            }
            return false;
        }

    };
}
