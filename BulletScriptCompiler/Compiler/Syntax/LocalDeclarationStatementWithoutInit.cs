namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// A non-user node representing the declaration of a variable that does
    /// not have initialization.
    /// </summary>
    internal class LocalDeclarationStatementWithoutInit : LocalDeclarationStatement, IEmittable {
        
        public LocalDeclarationStatementWithoutInit(
            IdentifierName name,
            Type type,
            Location location = default
        ) : base(new(name, type), location) { }

        public override string ToString()
            => $"[local declaration noinit]\nidentifier:\n{Indent(Declaration.Identifier)}\ntype:\n{Indent(Declaration.Type.ToString())}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Declaration.ValidateTree(path.Append(this));

        public LocalDeclarationStatementWithoutInit WithDeclaration(IdentifierName name, Type type)
            => new(name, type);
    }
}
