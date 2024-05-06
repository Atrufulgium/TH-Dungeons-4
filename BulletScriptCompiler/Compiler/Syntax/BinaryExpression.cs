using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// <para>
    /// Represents an expression of the form <c>a ∘ b</c> for some
    /// operator <c>∘</c>.
    /// </para>
    /// </summary>
    internal class BinaryExpression : Expression {
        public Expression LHS { get; private set; }
        public BinaryOp OP { get; private set; }
        public Expression RHS { get; private set; }

        public BinaryExpression(
            Expression lhs,
            BinaryOp op,
            Expression rhs,
            Location location = default
        ) : base(location) {
            LHS = lhs;
            OP = op;
            RHS = rhs;
        }

        public override string ToString()
            => $"[binop]\nlhs:\n{Indent(LHS)}\nop:\n{Indent(OP.ToString())}\nrhs:\n{Indent(RHS)}";

        public override string ToCompactString()
            => $"({LHS.ToCompactString()} {OP} {RHS.ToCompactString()})";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var childValidation = LHS.ValidateTree(path.Append(this));
            if (OP == BinaryOp.Error)
                childValidation = childValidation.Append(InvalidBinop(this));
            return childValidation.Concat(RHS.ValidateTree(path.Append(this)));
        }

        public BinaryExpression WithLHS(Expression lhs)
            => new(lhs, OP, RHS, Location);
        public BinaryExpression WithOP(BinaryOp op)
            => new(LHS, op, RHS, Location);
        public BinaryExpression WithRHS(Expression rhs)
            => new(LHS, OP, rhs, Location);
    }
}
