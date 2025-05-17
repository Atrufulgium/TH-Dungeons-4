using System;
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

        /// <summary>
        /// Gets a typed pointer to the collection underlying a NativeList.
        /// <br/>
        /// <b><i>Warning:</i></b> Lists' underlying pointer may be moved
        /// around after changing capacity. Persistent use of this pointer, or
        /// use of this pointer in contexts where capacity changes, is dangerous
        /// and likely to result in unintended behaviour.
        /// </summary>
        public static unsafe T* GetUnsafeTypedPtr<T>(this NativeList<T> nativeArray) where T : unmanaged
            => (T*)nativeArray.GetUnsafePtr();

        /// <inheritdoc cref="GetUnsafeTypedPtr{T}(NativeList{T})"/>
        public static unsafe T* GetUnsafeTypedReadOnlyPtr<T>(this NativeList<T> nativeArray) where T : unmanaged
            => (T*)nativeArray.GetUnsafeReadOnlyPtr();

        // Just here for consistency, imma not remember that this one specifically has a public field
        public static unsafe T* GetUnsafeTypedPtr<T>(this UnsafeList<T> unsafeList) where T : unmanaged
            => unsafeList.Ptr;

        public static unsafe T* GetUnsafeTypedPtr<T>(this UnsafeArray<T> unsafeArray) where T : unmanaged
            => unsafeArray.Ptr;

        /// <summary>
        /// Makes a copy of the contents of a list into a new list. This copies
        /// over all values.
        /// <br/>
        /// (But due to the existence of <see cref="NativeReference{T}"/>, the
        ///  referenced variables may end up being the same.)
        /// </summary>
        public static unsafe UnsafeList<T> Clone<T>(ref this UnsafeList<T> list, Allocator allocator) where T : unmanaged {
            UnsafeList<T> ret = new(list.Length, allocator);
            UnsafeUtility.MemCpy(ret.Ptr, list.Ptr, list.Length);
            return ret;
        }

        /// <inheritdoc cref="Clone{T}(ref UnsafeList{T}, Allocator)"/>
        public static unsafe UnsafeArray<T> Clone<T>(ref this UnsafeArray<T> array, Allocator allocator) where T : unmanaged {
            return new(array, allocator);
        }

        /// <summary>
        /// Resets all values in an array to `default`.
        /// </summary>
        public static unsafe void Clear<T>(ref this NativeArray<T> array) where T : unmanaged {
            UnsafeUtility.MemClear(array.GetUnsafePtr(), array.Length * (long)UnsafeUtility.SizeOf<T>());
        }

        /// <summary>
        /// Resets the first <paramref name="length"/> values in an array to
        /// `default`.
        /// </summary>
        public static unsafe void Clear<T>(ref this NativeArray<T> array, int length) where T : unmanaged {
            UnsafeUtility.MemClear(array.GetUnsafePtr(), length * (long)UnsafeUtility.SizeOf<T>());
        }

        /// <summary>
        /// Resets all values in an array to `default`.
        /// </summary>
        // Put this here instead of the UnsafeArray class as that isn't really
        // my code and it'd be weird.
        public static unsafe void Clear<T>(ref this UnsafeArray<T> array) where T : unmanaged {
            UnsafeUtility.MemClear(array.Ptr, array.Length * (long)UnsafeUtility.SizeOf<T>());
        }
    }
}
