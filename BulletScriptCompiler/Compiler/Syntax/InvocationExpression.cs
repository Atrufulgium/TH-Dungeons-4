using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
            Location location = default
        ) : base(location) {
            Target = target;
            Arguments = new(arguments);
        }

        public override string ToString()
            => $"[invocation]\ntarget:\n{Indent(Target)}\nargs:\n{Indent(Arguments)}";

        public override string ToCompactString() {
            string res = $"{Target.Name}(";
            bool first = true;
            foreach (var a in Arguments) {
                if (first) {
                    first = false;
                } else {
                    res += ",";
                }
                res += a.ToCompactString();
            }
            res += $")";
            return res;
        }

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Target.ValidateTree(path.Append(this))
            .Concat(Arguments.SelectMany(e => e.ValidateTree(path.Append(this))));

        public InvocationExpression WithTarget(IdentifierName target)
            => new(target, Arguments, Location);
        public InvocationExpression WithArguments(IList<Expression> arguments)
            => new(Target, arguments, Location);
    }
}
