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

        /// <summary>
        /// The location to use when something was not found in usercode, but
        /// instead introduced by the compiler.
        /// </summary>
        public static Location CompilerIntroduced => default;

        public override string ToString() => line > 0 && col > 0 ? $"Line {line}, col {col}" : "Not user code";
    }
}
