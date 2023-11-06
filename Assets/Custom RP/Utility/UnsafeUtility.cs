using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

namespace CustomPipeline
{
    public unsafe static class UnsafeUtility
    {
        private struct Ptr
        {
            public object value;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* GetPtr(object obj)
        {
            Ptr ptr = new Ptr { value = obj };
            ulong* tmp = (ulong*)AddressOf(ref ptr);
            return (void*)*tmp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetObject<T>(void* ptr) where T : class
        {
            Ptr p = new Ptr();
            ulong* pp = (ulong*)AddressOf(ref p);
            *pp = (ulong)ptr;
            return p.value as T;
        }
    }
}