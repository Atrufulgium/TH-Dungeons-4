namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// <para>
    /// Represents an expression of the form <c>a ∘ b</c> for some
    /// operator <c>∘</c>.
    /// </para>
    /// </summary>
    internal class BinaryExpression : Expression {
        public Expression LHS { get; private set; }
        public string OP { get; private set; }
        public Expression RHS { get; private set; }

        public BinaryExpression(
            Expression lhs,
            string op,
            Expression rhs,
            Location location
        ) : base(location) {
            LHS = lhs;
            OP = op;
            RHS = rhs;
        }

        public override string ToString()
            => $"[binop]\nlhs:\n{Indent(LHS)}\nop:\n{Indent(OP)}\nrhs:\n{Indent(RHS)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => LHS.ValidateTree(path.Append(this))
            .Concat(RHS.ValidateTree(path.Append(this)));
    }
}
