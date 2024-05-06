using System.Collections.ObjectModel;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// When you want to return multiple statements in a context where that is
    /// supported (e.g. a <see cref="Block"/>) when doing rewrites, return this
    /// and the statements get flattened into the surrounding list of statements.
    /// <br/>
    /// These may be nested. This node is only valid when it ends up in the
    /// statement list of:
    /// <list type="bullet">
    /// <item>
    /// <see cref="Visitors.AbstractTreeRewriter.VisitBlock(Block)"/>; and
    /// </item>
    /// <item>
    /// <see cref="Visitors.AbstractTreeRewriter.VisitRoot(Root)"/>.
    /// </item>
    /// </list>
    /// </summary>
    internal class MultipleStatements : Statement, ITransientNode {
        public ReadOnlyCollection<Statement> Statements { get; private set; }

        public MultipleStatements(IList<Statement> statements)
            : base(Location.CompilerIntroduced) {
            Statements = new(statements);
        }

        public MultipleStatements(params Statement[] statements)
            : this(statements.ToList()) { }

        public override string ToString()
            => $"[multiple statements]\nstatements:\n{Indent(Statements)}";

        public override string ToCompactString()
            => $"[multiple statements]\n{CompactIndent(Statements)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => throw new PersistentTransientException(this);

        public MultipleStatements WithStatements(IList<Statement> statements)
            => new(statements);
        public MultipleStatements WithStatements(params Statement[] statements)
            => new(statements);

        public MultipleStatements WithPrependedStatements(IList<Statement> statements)
            => new(statements.Concat(Statements).ToList());
        public MultipleStatements WithPrependedStatements(params Statement[] statements)
            => new(statements.Concat(Statements).ToList());

        public MultipleStatements WithAppendedStatements(IList<Statement> statements)
            => new(Statements.Concat(statements).ToList());
        public MultipleStatements WithAppendedStatements(params Statement[] statements)
            => new(Statements.Concat(statements).ToList());

        /// <summary>
        /// Removes nested <see cref="MultipleStatements"/> into one list.
        /// </summary>
        public IEnumerable<Statement> Flatten() {
            foreach(var s in Statements) {
                if (s is MultipleStatements m) {
                    foreach (var s2 in m.Flatten())
                        yield return s2;
                } else {
                    yield return s;
                }
            }
        }
    }
}
