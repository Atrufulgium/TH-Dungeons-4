using System.Collections.ObjectModel;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents a block of statements.
    /// </summary>
    internal class Block : Node {
        public ReadOnlyCollection<Statement> Statements { get; private set; }

        public Block(IList<Statement> statements, Location location) : base(location) {
            Statements = new(statements);
        }

        public override string ToString()
            => $"[block]\nstatements:\n{Indent(Statements)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Statements.SelectMany(s => s.ValidateTree(path.Append(this)));
    }
}
