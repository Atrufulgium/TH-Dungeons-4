namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents any looping mechanism.
    /// </summary>
    internal abstract class LoopStatement : Statement {
        public LoopStatement(Location location) : base(location) { }
    }
}
