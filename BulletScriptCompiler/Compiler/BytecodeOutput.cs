namespace Atrufulgium.BulletScript.Compiler {
    /// <summary>
    /// Represents everything the VM needs to run this BulletScript code.
    /// See the extended documentation for more info.
    /// </summary>
    public class BytecodeOutput {
        /// <summary>
        /// All opcodes and their args.
        /// </summary>
        public int[] OpCodes { get; internal set; }
        /// <summary>
        /// A sufficiently large pre-initialized memory.
        /// </summary>
        public float[] Memory { get; internal set; }
        /// <summary>
        /// All strings this bytecode references.
        /// </summary>
        public string[] Strings { get; internal set; }

        internal BytecodeOutput() { }
    }
}
