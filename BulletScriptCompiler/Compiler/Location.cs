namespace Atrufulgium.BulletScript.Compiler {
    /// <summary>
    /// Represents a location in the source code.
    /// </summary>
    public readonly struct Location {
        /// <summary>
        /// The ONE-indexed line of interest.
        /// </summary>
        public readonly int line;
        /// <summary>
        /// The ONE-indexed column of interest.
        /// </summary>
        public readonly int col;

        public Location(int line, int col) {
            this.line = line;
            this.col = col;
        }

        public override string ToString() => $"Line {line}, col {col}";
    }
}
