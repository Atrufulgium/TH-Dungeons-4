namespace Atrufulgium.BulletScript.Compiler.Helpers {
    internal static class StringExtensions {
        /// <summary>
        /// Truncates strings. <paramref name="dotsPosition"/> indicates the
        /// index of the first dots, either counted from start or end.
        /// <list type="bullet">
        /// <item><c>"lorem ipsum".Truncate(7, 1)</c> gives "l...sum".</item>
        /// <item><c>"lorem ipsum".Truncate(7, ^2)</c> gives "lor...m".</item>
        /// </list>
        /// </summary>
        public static string Truncate(this string str, int maxLength, Index dotsPosition) {
            if (str.Length < maxLength)
                return str;

            string left, right;
            // Just grab a pen and draw in your notebook.
            int value = dotsPosition.Value;
            if (dotsPosition.IsFromEnd) {
                left = str[..(maxLength - 3 - value)];
                right = str[^value..];
            } else {
                left = str[..value];
                right = str[^(maxLength - 3 - value)..];
            }
            return left + "..." + right;
        }
    }
}
