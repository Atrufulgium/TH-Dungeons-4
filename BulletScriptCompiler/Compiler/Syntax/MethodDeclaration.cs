using System.Collections.ObjectModel;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// <para>
    /// Represents declaring a function, such as <c>function void main(float value)</c>,
    /// <c>function matrix2x2 my_use_function(float value)</c>, or
    /// <c>function void on_health&lt;0.75&gt;()</c>.
    /// </para>
    /// <para>
    /// Note that the "generic" part is considered part of the identifier name.
    /// </para>
    /// </summary>
    internal class MethodDeclaration : Declaration {
        public ReadOnlyCollection<LocalDeclarationStatement> Arguments { get; private set; }
        public Block Body { get; private set; }

        public MethodDeclaration(
            IdentifierName identifier,
            IdentifierName type,
            IList<LocalDeclarationStatement> arguments,
            Block body,
            Location location
        ) : base(identifier, type, location) {
            Arguments = new(arguments);
            Body = body;
        }

        public override string ToString()
            => $"[method declaration]\nidentifier:\n{Indent(Identifier)}\ntype:\n{Indent(Type)}\n"
            + $"arguments:\n{Indent(Arguments)}\nblock:\n{Indent(Body)}";

        // IdentifierNames are trivially fine and don't need to be checked.
        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Arguments.SelectMany(s => s.ValidateTree(path.Append(this)))
            .Concat(Body.ValidateTree(path.Append(this)));
    }
}
