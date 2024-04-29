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

        /// <summary>
        /// Considering a range a collection, take a subrange of a range.
        /// Some examples:
        /// <list type="bullet">
        /// <item><c>(2..6).SubRange(1..3)</c> is the same as <c>3..5</c>;</item>
        /// <item><c>(2..6).SubRange(1..)</c> is the same as <c>3..6</c>;</item>
        /// <item><c>(^6..).SubRange(1..3)</c> is the same as <c>^5..^3</c>.</item>
        /// </list>
        /// You get the gist.
        /// </summary>
        public static Range SubRange(this Range range, Range subrange)
            => new(range.Offset(subrange.Start), range.Offset(subrange.End));

        /// <summary>
        /// Computes where Index <paramref name="i"/> lies in range <paramref name="r"/>:
        /// <list type="bullet">
        /// <item><c>  r1..[^]r2,  i </c> give <c>   r1 + i  </c>;</item>
        /// <item><c> ^r1..[^]r2,  i </c> give <c> ^(r1 - i) </c>;</item>
        /// <item><c>  [^]r1..r2, ^i </c> give <c>   r2 - i  </c>;</item>
        /// <item><c> [^]r1..^r2, ^i </c> give <c> ^(r2 + i) </c>.</item>
        /// </list>
        /// </summary>
        public static Index Offset(this Range r, Index i) {
            Index r1 = r.Start;
            Index r2 = r.End;
            // I don't like this approach.
            // Oh well, it's provably correct so who cares.
            // (Note that Index' constructor throws when <0, which is what we
            //  want here.)
            if (i.IsFromEnd) {
                if (r2.IsFromEnd) {
                    return new(r2.Value + i.Value, true);
                }
                return new(r2.Value - i.Value, false);
            } else {
                if (r1.IsFromEnd) {
                    return new(r1.Value - i.Value, true);
                }
                return new(r1.Value + i.Value, false);
            }
        }
    }
}
