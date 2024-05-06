using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents an expression of the form <c>∘a</c> for some
    /// operator <c>∘</c>.
    /// </summary>
    internal class PrefixUnaryExpression : Expression {
        public Expression Expression { get; private set; }
        public PrefixUnaryOp OP { get; private set; }

        public PrefixUnaryExpression(
            Expression expression,
            PrefixUnaryOp op,
            Location location = default
        ) : base(location) {
            Expression = expression;
            OP = op;
        }

        public override string ToString()
            => $"[prefix]\nop:\n{Indent(OP.ToString())}\nexpression:\n{Indent(Expression)}";

        public override string ToCompactString()
            => $"({OP}{Expression.ToCompactString()})";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var diags = Expression.ValidateTree(path.Append(this));
            if (OP == PrefixUnaryOp.Error)
                diags = diags.Append(NotAPrefixUnary(Location));
            return diags;
        }


        public PrefixUnaryExpression WithExpression(Expression expression)
            => new(expression, OP, Location);
        public PrefixUnaryExpression WithOP(PrefixUnaryOp op)
            => new(Expression, op, Location);
    }
}
