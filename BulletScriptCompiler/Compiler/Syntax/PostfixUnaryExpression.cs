using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents an expression of the form <c>a∘</c> for some
    /// operator <c>∘</c>.
    /// </summary>
    internal class PostfixUnaryExpression : Expression {
        public Expression Expression { get; private set; }
        public string OP { get; private set; }

        public PostfixUnaryExpression(
            Expression expression,
            string op,
            Location location
        ) : base(location) {
            Expression = expression;
            OP = op;
        }

        public override string ToString()
            => $"[postfix]\nop:\n{Indent(OP)}\nexpression:\n{Indent(Expression)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var diags = Expression.ValidateTree(path.Append(this));
            if (OP is not ("++" or "--"))
                diags = diags.Append(NotAPostfixUnary(Location));
            return diags;
        }

        public PostfixUnaryExpression WithExpression(Expression expression)
            => new(expression, OP, Location);
        public PostfixUnaryExpression WithOP(string op)
            => new(Expression, op, Location);
    }
}
