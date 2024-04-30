namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Everything that can roughly be seen as "one" line of code.
    /// </summary>
    internal abstract class Statement : Node {
        public Statement(Location location) : base(location) { }
    }
}
