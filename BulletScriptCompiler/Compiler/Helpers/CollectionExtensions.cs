namespace Atrufulgium.BulletScript.Compiler.Helpers {
    internal static class CollectionExtensions {
        /// <summary>
        /// Returns the index of the first item in the list matching a given
        /// predicate. If none are found, it will return the length of the list.
        /// </summary>
        public static int FirstIndexWhere<T>(this IReadOnlyList<T> list, Func<T, bool> predicate) {
            for (int i = 0; i < list.Count; i++) {
                if (predicate(list[i]))
                    return i;
            }
            return list.Count;
        }
    }
}
