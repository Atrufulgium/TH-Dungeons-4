using System.Collections.ObjectModel;

namespace Atrufulgium.BulletScript.Compiler.Syntax {

    /// <summary>
    /// Represents a function call, whether built-in or not.
    /// </summary>
    internal class InvocationExpression : Expression {
        public IdentifierName Target { get; private set; }
        public ReadOnlyCollection<Expression> Arguments { get; private set; }

        public InvocationExpression(
            IdentifierName target,
            IList<Expression> arguments,
            Location location
        ) : base(location) {
            Target = target;
            Arguments = new(arguments);
        }

        public override string ToString()
            => $"[invocation]\ntarget:\n{Indent(Target)}\nargs:\n{Indent(Arguments)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Target.ValidateTree(path.Append(this))
            .Concat(Arguments.SelectMany(e => e.ValidateTree(path.Append(this))));
    }
}
