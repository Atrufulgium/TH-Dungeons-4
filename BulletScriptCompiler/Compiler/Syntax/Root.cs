using System.Collections.ObjectModel;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents the root node of the tree. This either contains a block of
    /// statements representing an implicit <c>main()</c> method, OR, function
    /// and variable definitions.
    /// </summary>
    internal class Root : Node {
        public ReadOnlyCollection<Declaration> Declarations { get; private set; }
        public ReadOnlyCollection<Statement> RootLevelStatements { get; private set; }

        public Root(IList<Declaration> declarations, Location location) : base(location) {
            Declarations = new(declarations);
            RootLevelStatements = new(Array.Empty<Statement>());
        }

        public Root(IList<Statement> statements, Location location) : base(location) {
            Declarations = new(Array.Empty<Declaration>());
            RootLevelStatements = new(statements);
        }

        public override string ToString() {
            string ret = "[root]\n";
            if (Declarations.Count > 0) {
                ret += $"declarations:\n{Indent(Declarations)}";
            } else {
                ret += $"statements:\n{Indent(RootLevelStatements)}";
            }
            return ret;
        }

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Declarations.SelectMany(d => d.ValidateTree(path.Append(this)))
            .Concat(RootLevelStatements.SelectMany(s => s.ValidateTree(path.Append(this))));

        public Root WithDeclarations(IList<Declaration> declarations)
            => new(declarations, Location);
        public Root WithRootLevelStatements(IList<Statement> statements)
            => new(statements, Location);
    }
}
