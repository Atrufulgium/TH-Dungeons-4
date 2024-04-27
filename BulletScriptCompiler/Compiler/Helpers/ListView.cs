using System.Collections;

namespace Atrufulgium.BulletScript.Compiler.Helpers {

    /// <summary>
    /// <para>
    /// Represents a live view of a part of a list.
    /// </para>
    /// <para>
    /// It's indexed such thatTo access the first index
    /// of this view, simply use <c>[0]</c>.
    /// </para>
    /// <para>
    /// The "from end" <c>^</c> of the overload that constructs a view from a
    /// <see cref="Range"/>, gives dynamic indices: if the list grows, the
    /// upper ends grow as well.
    /// </para>
    /// </summary>
    internal class ListView<TIReadOnlyList, T> : IReadOnlyList<T>
        where TIReadOnlyList : IReadOnlyList<T> {
        private readonly TIReadOnlyList list;
        // The two endpoints of allowed indices.
        readonly Range viewRange;

        int RangeLower => viewRange.Start.GetOffset(list.Count);
        int RangeUpper => viewRange.End.GetOffset(list.Count);
        public int Count => RangeUpper - RangeLower;

        /// <summary>
        /// Creates a view of an existing list. Changes in the list are
        /// reflected in the list view.
        /// </summary>
        /// <param name="list"> What list to make a view of. </param>
        /// <param name="lower"> The lower bound of the list to consider. </param>
        public ListView(TIReadOnlyList list, int lower) {
            if (lower < 0)
                throw new ArgumentOutOfRangeException(nameof(lower), $"Lower index is {lower} < 0.");

            this.list = list;
            viewRange = lower..;
        }

        /// <inheritdoc cref="ListView{TIReadOnlyList, T}.ListView(TIReadOnlyList, int)"/>
        /// <param name="upper"> The upper bound of this list to consider. </param>
        public ListView(TIReadOnlyList list, int lower, int upper) {
            if (lower < 0)
                throw new ArgumentOutOfRangeException(nameof(lower), $"Lower index is {lower} < 0.");
            if (upper < 0)
                throw new ArgumentOutOfRangeException(nameof(upper), $"Upper index is {upper} < 0.");
            if (lower > upper)
                throw new ArgumentException($"Lower index is {lower} > upper index {upper}.");

            this.list = list;
            viewRange = lower..upper;
        }

        /// <inheritdoc cref="ListView{TIReadOnlyList, T}.ListView(TIReadOnlyList, int)"/>
        /// <param name="viewRange">
        /// The range of this view to consider. Just like the list, this is a
        /// live range. If you pas <c>^2</c>, it will always exclude
        /// <i>exactly</i> the last element, no matter what you add or remove.
        /// </param>
        public ListView(TIReadOnlyList list, Range viewRange) {
            this.list = list;
            this.viewRange = viewRange;
        }

        public T this[int i] {
            get {
                if (i < 0)
                    throw new ArgumentOutOfRangeException(nameof(i), $"Attempted to grab index {i} < 0.");
                int lower = RangeLower;
                int upper = RangeUpper;
                int maxIndex = upper - lower;
                if (i >= maxIndex)
                    throw new ArgumentOutOfRangeException(nameof(i), $"Attempted to grab index {i} out of view of current local range [{lower}, {upper}] ({viewRange} of original list)");
                return list[i + lower];
            }
        }

        public IEnumerator<T> GetEnumerator() {
            for (int i = RangeLower; i < RangeUpper; i++) {
                yield return list[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    internal static class ListViewExtensions {
        /// <inheritdoc cref="ListView{TIReadOnlyList, T}.ListView(TIReadOnlyList, int)"/>
        public static ListView<IReadOnlyList<T>, T> GetView<T>(this IReadOnlyList<T> list, int lower)
            => new(list, lower);
        /// <inheritdoc cref="ListView{TIReadOnlyList, T}.ListView(TIReadOnlyList, int, int)"/>
        public static ListView<IReadOnlyList<T>, T> GetView<T>(this IReadOnlyList<T> list, int lower, int upper)
            => new(list, lower, upper);
        /// <inheritdoc cref="ListView{TIReadOnlyList, T}.ListView(TIReadOnlyList, Range)"/>
        public static ListView<IReadOnlyList<T>, T> GetView<T>(this IReadOnlyList<T> list, Range viewRange)
            => new(list, viewRange);
    }
}
