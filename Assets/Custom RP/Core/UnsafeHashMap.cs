using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Core
{
    public unsafe partial struct UnsafeHashMap
    {
        UnsafeHashCollection _collection;
        int _valueOffset;
        
        
    }
}