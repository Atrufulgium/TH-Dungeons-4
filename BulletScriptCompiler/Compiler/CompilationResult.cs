namespace Atrufulgium.BulletScript.Compiler {
    /// <summary>
    /// Contains the result of compilation:
    /// <list type="bullet">
    /// <item>
    /// If <see cref="Succesful"/>, contains the resulting <see cref="BytecodeOutput"/>.
    /// This output can be requested with <see cref="TryGetBytecodeOutput(out BytecodeOutput)"/>.
    /// </item>
    /// <item>
    /// <see cref="Diagnostics"/> are always included, and may help discover
    /// why the compilation failed, if unsuccesful.
    /// </item>
    /// </list>
    /// </summary>
    public class CompilationResult {
        public bool Succesful { get; internal set; }
        internal BytecodeOutput SuccessOutput { get; set; }

        public Diagnostic[] Diagnostics { get; internal set; }

        public bool TryGetBytecodeOutput(out BytecodeOutput output) {
            output = null;
            if (!Succesful)
                return false;
            output = SuccessOutput;
            return true;
        }
    }
}
