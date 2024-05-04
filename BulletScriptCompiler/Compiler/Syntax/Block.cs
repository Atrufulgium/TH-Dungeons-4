using System.Collections.ObjectModel;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents a block of statements.
    /// </summary>
    internal class Block : Node {
        public ReadOnlyCollection<Statement> Statements { get; private set; }

        public Block(IList<Statement> statements, Location location = default) : base(location) {
            Statements = new(statements);
        }
        public Block(params Statement[] statements)
            : this(statements.ToList()) { }

        public override string ToString()
            => $"[block]\nstatements:\n{Indent(Statements)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Statements.SelectMany(s => s.ValidateTree(path.Append(this)));

        public Block WithStatements(IList<Statement> statements)
            => new(statements, Location);

        public Block WithPrependedStatements(IList<Statement> statements)
            => new(statements.Concat(Statements).ToList(), Location);
        public Block WithPrependedStatements(params Statement[] statements)
            => new(statements.Concat(Statements).ToList(), Location);

        public Block WithAppendedStatements(IList<Statement> statements)
            => new(Statements.Concat(statements).ToList(), Location);
        public Block WithAppendedStatements(params Statement[] statements)
            => new(Statements.Concat(statements).ToList(), Location);
    }
}
