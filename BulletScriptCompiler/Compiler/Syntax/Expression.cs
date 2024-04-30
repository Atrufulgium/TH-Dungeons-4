namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Expressions are the "meat" of the code. They, roughly, correspond to
    /// something you can put on the right-hand side of an equality.
    /// </summary>
    internal abstract class Expression : Node {
        public Expression(Location location) : base(location) { }
    }
}
