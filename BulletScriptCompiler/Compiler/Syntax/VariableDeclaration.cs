using System.Text.RegularExpressions;
using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

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

        readonly Regex matrixType = new(@"^matrix[1-4]x[1-4]$");

        // The identifier is trivially fine and don't need to be checked.
        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            IEnumerable<Diagnostic> diags = new List<Diagnostic>();
            if (Type.Name is not ("float" or "matrix" or "string")) {
                if (matrixType.Matches(Type.Name).Count == 0)
                    diags = diags.Append(UnknownType(Location));
            }

            if (Initializer != null)
                diags = diags.Concat(Initializer.ValidateTree(path.Append(this)));
            return diags;
        }

        public VariableDeclaration WithIdentifier(IdentifierName identifier)
            => new(identifier, Type, Location, Initializer);
        public VariableDeclaration WithType(IdentifierName type)
            => new(Identifier, type, Location, Initializer);
        public VariableDeclaration WithInitializer(Expression? initializer)
            => new(Identifier, Type, Location, initializer);
    }
}
