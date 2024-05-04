using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents the <c>break;</c> statement in a loop.
    /// </summary>
    internal class BreakStatement : Statement {
        public BreakStatement(Location location = default) : base(location) { }

        public override string ToString()
            => "[break]";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            if (path.OfType<LoopStatement>().Any())
                return new List<Diagnostic>();
            return new List<Diagnostic>() { BreakNotInLoop(this) };
        }
    }
}
