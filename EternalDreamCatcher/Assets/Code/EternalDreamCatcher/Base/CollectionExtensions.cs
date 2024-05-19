using System;
using System.Collections.Generic;
using Unity.Collections;

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
    }
}
