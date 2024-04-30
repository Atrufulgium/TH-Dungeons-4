namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents declaring a variable.
    /// </summary>
    internal class VariableDeclaration : Declaration {
        public Expression? Initializer { get; private set; }

        public VariableDeclaration(
            IdentifierName identifier,
            IdentifierName type,
            Location location,
            Expression? initializer = null
        ) : base(identifier, type, location) {
            Initializer = initializer;
        }

        public override string ToString()
            => $"[variable declaration]\nidentifier:\n{Indent(Identifier)}\ntype:\n{Indent(Type)}\n"
            + $"initializer:\n{Indent(Initializer)}";

        // IdentifierNames are trivially fine and don't need to be checked.
        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Initializer?.ValidateTree(path.Append(this)) ?? new List<Diagnostic>();
    }
}
