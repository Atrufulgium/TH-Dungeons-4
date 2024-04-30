using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents an expression of the form <c>a ∘= b</c> for some (or no)
    /// operator <c>∘</c>. <see cref="OP"/> excludes the <c>=</c>-sign when in
    /// <c>∘=</c> form.
    /// </summary>
    internal class AssignmentExpression : Expression {
        public IdentifierName LHS { get; private set; }
        public string OP { get; private set; }
        public Expression RHS { get; private set; }

        public AssignmentExpression(
            IdentifierName lhs,
            string op,
            Expression rhs,
            Location location
        ) : base(location) {
            LHS = lhs;
            OP = op;
            RHS = rhs;
        }

        public override string ToString()
            => $"[assignment]\nlhs:\n{Indent(LHS)}\nop:\n{Indent(OP)}\nrhs:\n{Indent(RHS)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var childValidation = LHS.ValidateTree(path.Append(this))
                .Concat(RHS.ValidateTree(path.Append(this)));

            // Only allow assignments as a root level statement.
            // i.e. the parent must directly be an ExpressionStatement.
            var parent = path.LastOrDefault();
            if (parent != null && parent is not ExpressionStatement)
                return childValidation.Prepend(AssignmentOnlyAsStatement(this));
            return childValidation;
        }
    }
}
