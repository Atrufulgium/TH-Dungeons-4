using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Atrufulgium.EternalDreamCatcher.Base {
    /// <summary>
    /// Represents a permutation between indices <typeparamref name="T"/>.
    /// <br/>
    /// This implements the enumerable interfaces, but these only iterate the
    /// <i>changed</i> values (`from`, `to`). Identity values are implicit.
    /// </summary>
    public struct Permutation<T> : IDisposable, IEnumerable, IEnumerable<(T,T)> where T : unmanaged, IEquatable<T> {

        // Entries `t` that exist map to `perm[t]`.
        // Entries `t` that do not exist map to `t`.
        // This is a bijection at all times.
        NativeParallelHashMap<T, T> perm;

        public Permutation(int capacity, Allocator allocator) {
            perm = new(capacity, allocator);
        }

        /// <summary>
        /// Resets the permutation to t ↦ t over all of <typeparamref name="T"/>.
        /// </summary>
        public void ResetToIdentity() {
            perm.Clear();
        }

        /// <summary>
        /// Swaps the values at <paramref name="t1"/> and <paramref name="t2"/>
        /// in the permutation.
        /// </summary>
        public void Swap(T t1, T t2) {
            // *Current* values
            if (!perm.TryGetValue(t1, out T value1))
                value1 = t1;
            if (!perm.TryGetValue(t2, out T value2))
                value2 = t2;

            // Actuall swap.
            // Extra check to not include identity entries in the hashmap.
            if (t1.Equals(value2))
                perm.Remove(t1);
            else
                perm[t1] = value2;

            if (t2.Equals(value1))
                perm.Remove(t2);
            else
                perm[t2] = value1;
        }

        /// <summary>
        /// Gives the result of <paramref name="t"/> in the permutation.
        /// </summary>
        public T Permute(T t) {
            if (perm.ContainsKey(t))
                return perm[t];
            return t;
        }

        public void Dispose() {
            perm.Dispose();
        }

        public IEnumerator<(T, T)> GetEnumerator() {
            foreach (var kv in perm)
                yield return (kv.Key, kv.Value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
