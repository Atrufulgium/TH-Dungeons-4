using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents the <c>continue;</c> statement in a loop.
    /// </summary>
    internal class ContinueStatement : Statement {
        public ContinueStatement(Location location = default) : base(location) { }

        public override string ToString()
            => "[continue]";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            if (path.OfType<LoopStatement>().Any())
                return new List<Diagnostic>();
            return new List<Diagnostic>() { ContinueNotInLoop(this) };
        }
    }
}
