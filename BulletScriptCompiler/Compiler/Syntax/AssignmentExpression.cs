using System.Collections.Generic;
using System.Linq;
using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents an expression of the form <c>a ∘= b</c> for some (or no)
    /// operator <c>∘</c>. <see cref="OP"/> excludes the <c>=</c>-sign when in
    /// <c>∘=</c> form.
    /// </summary>
    internal class AssignmentExpression : Expression {
        public IdentifierName LHS { get; private set; }
        public AssignmentOp OP { get; private set; }
        public Expression RHS { get; private set; }

        public AssignmentExpression(
            IdentifierName lhs,
            AssignmentOp op,
            Expression rhs,
            Location location = default
        ) : base(location) {
            LHS = lhs;
            OP = op;
            RHS = rhs;
        }

        public override string ToString()
            => $"[assignment]\nlhs:\n{Indent(LHS)}\nop:\n{Indent(OP.ToString())}\nrhs:\n{Indent(RHS)}";

        public override string ToCompactString() {
            string op = OP.ToString();
            if (op != "=")
                op = $"{op}=";
            return $"{LHS.Name} {op} {RHS.ToCompactString()}";
        }

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var childValidation = LHS.ValidateTree(path.Append(this));
            if (OP == AssignmentOp.Error)
                childValidation = childValidation.Append(InvalidAssignment(this));
            childValidation = childValidation.Concat(RHS.ValidateTree(path.Append(this)));

            // Only allow assignments as a root level statement.
            // i.e. the parent must directly be an ExpressionStatement.
            var parent = path.LastOrDefault();
            if (parent != null && parent is not ExpressionStatement)
                return childValidation.Prepend(AssignmentOnlyAsStatement(this));
            return childValidation;
        }

        public AssignmentExpression WithLHS(IdentifierName lhs)
            => new(lhs, OP, RHS, Location);
        public AssignmentExpression WithOP(AssignmentOp op)
            => new(LHS, op, RHS, Location);
        public AssignmentExpression WithRHS(Expression rhs)
            => new(LHS, OP, rhs, Location);
    }
}
