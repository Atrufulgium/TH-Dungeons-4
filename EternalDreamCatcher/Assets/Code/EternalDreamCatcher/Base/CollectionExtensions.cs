using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Atrufulgium.EternalDreamCatcher.Base {
    internal static class CollectionExtensions {
        /// <summary>
        /// Adds one to a key that already exists.
        /// Sets to one a key that does not yet exist.
        /// </summary>
        public static void Increment<TKey>(this NativeParallelHashMap<TKey, int> dict, TKey key)
            where TKey : struct, IEquatable<TKey> {
            if (!dict.TryGetValue(key, out int val)) {
                val = 0;
            }
            dict[key] = val + 1;
        }

        public unsafe static T* GetUnsafeTypedPtr<T>(this NativeReference<T> reference) where T : unmanaged
            => (T*)reference.GetUnsafePtr();

        public static unsafe T* GetUnsafeTypedPtr<T>(this NativeArray<T> nativeArray) where T : unmanaged
            => (T*)nativeArray.GetUnsafePtr();

        public static unsafe T* GetUnsafeTypedReadOnlyPtr<T>(this NativeArray<T> nativeArray) where T : unmanaged
            => (T*)nativeArray.GetUnsafeReadOnlyPtr();
    }
}
