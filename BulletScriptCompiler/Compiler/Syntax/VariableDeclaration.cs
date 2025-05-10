using System.Collections.Generic;
using System.Linq;
using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents declaring a variable.
    /// </summary>
    internal class VariableDeclaration : Declaration {
        public Expression? Initializer { get; private set; }

        public VariableDeclaration(
            IdentifierName identifier,
            Type type,
            Location location = default,
            Expression? initializer = null
        ) : base(identifier, type, location) {
            Initializer = initializer;
        }

        public override string ToString()
            => $"[variable declaration]\nidentifier:\n{Indent(Identifier)}\ntype:\n{Indent(Type.ToString())}\n"
            + $"initializer:\n{Indent(Initializer)}";

        public override string ToCompactString() {
            var res = Initializer == null ? "<variable declaration>" : "[variable declaration]";
            res += $"   {Type} {Identifier.Name}";
            if (Initializer != null)
                res += $" = {Initializer.ToCompactString()}";
            return res;
        }

        // The identifier is trivially fine and don't need to be checked.
        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            IEnumerable<Diagnostic> diags = new List<Diagnostic>();
            if (Type == Type.Error || Type == Type.Void) {
                diags = diags.Append(UnknownType(Location));
            }

            if (Initializer != null)
                diags = diags.Concat(Initializer.ValidateTree(path.Append(this)));
            return diags;
        }

        public VariableDeclaration WithIdentifier(IdentifierName identifier)
            => new(identifier, Type, Location, Initializer);
        public VariableDeclaration WithType(Type type)
            => new(Identifier, type, Location, Initializer);
        public VariableDeclaration WithInitializer(Expression? initializer)
            => new(Identifier, Type, Location, initializer);
    }
}
