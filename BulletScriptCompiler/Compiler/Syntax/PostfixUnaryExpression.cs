using System;
using System.Collections.Generic;
using System.Linq;
using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents an expression of the form <c>a∘</c> for some
    /// operator <c>∘</c>.
    /// </summary>
    internal class PostfixUnaryExpression : Expression {
        public Expression Expression { get; private set; }
        public PostfixUnaryOp OP { get; private set; }

        public PostfixUnaryExpression(
            Expression expression,
            PostfixUnaryOp op,
            Location location = default
        ) : base(location) {
            Expression = expression;
            OP = op;
        }

        public override string ToString()
            => $"[postfix]\nop:\n{Indent(OP.ToString())}\nexpression:\n{Indent(Expression)}";

        public override string ToCompactString()
            => $"({Expression.ToCompactString()}{OP})";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var diags = Expression.ValidateTree(path.Append(this));
            if (OP == PostfixUnaryOp.Error)
                diags = diags.Append(NotAPostfixUnary(Location));
            if (OP == PostfixUnaryOp.Increment || OP == PostfixUnaryOp.Decrement) {
                if (Expression is not IdentifierName)
                    throw new NotSupportedException("++ and -- should be applied to identifiers, and the tree should not've reached this state.");
            }
            return diags;
        }

        public PostfixUnaryExpression WithExpression(Expression expression)
            => new(expression, OP, Location);
        public PostfixUnaryExpression WithOP(PostfixUnaryOp op)
            => new(Expression, op, Location);
    }
}
