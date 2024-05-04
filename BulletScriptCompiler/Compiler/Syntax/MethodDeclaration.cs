using System.Collections.ObjectModel;
using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

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
            Type type,
            IList<LocalDeclarationStatement> arguments,
            Block body,
            Location location = default
        ) : base(identifier, type, location) {
            Arguments = new(arguments);
            Body = body;
        }

        public override string ToString()
            => $"[method declaration]\nidentifier:\n{Indent(Identifier)}\ntype:\n{Indent(Type.ToString())}\n"
            + $"arguments:\n{Indent(Arguments)}\nblock:\n{Indent(Body)}";

        // The identifier is trivially fine and don't need to be checked.
        // The arguments may not have initializers.
        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var ret = new List<Diagnostic>();

            if (Type == Type.Error)
                ret.Add(FunctionTypeWrong(Location));

            foreach (var arg in Arguments) {
                if (arg.Declaration.Initializer != null)
                    ret.Add(MethodDeclarationInitializer(arg.Declaration.Initializer));
                if (arg.Declaration.Type == Type.MatrixUnspecified)
                    ret.Add(ParamMatricesNeedSize(arg));
            }

            return ret.Concat(Arguments.SelectMany(s => s.ValidateTree(path.Append(this))))
            .Concat(Body.ValidateTree(path.Append(this)));
        }

        public MethodDeclaration WithIdentifier(IdentifierName identifier)
            => new(identifier, Type, Arguments, Body, Location);
        public MethodDeclaration WithType(Type type)
            => new(Identifier, type, Arguments, Body, Location);
        public MethodDeclaration WithArguments(IList<LocalDeclarationStatement> arguments)
            => new(Identifier, Type, arguments, Body, Location);
        public MethodDeclaration WithBody(Block body)
            => new(Identifier, Type, Arguments, body, Location);
    }
}
