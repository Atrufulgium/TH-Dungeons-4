namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Non-user node representing simple assignments
    /// <code>
    ///     name = a;
    ///     name = ∘a;
    ///     name = a∘b;
    ///     name = a[b];
    ///     name = [a ... b];
    ///     name = [a : b];
    /// </code>
    /// Here a and b may be identifiers or literals.  For binops, not both may
    /// be literals, and for indexing, `a` may also not be a literal.
    /// </summary>
    internal class SimpleAssignmentStatement : Statement, IEmittable {
        public IdentifierName LHS { get; private set; }
        public Expression RHS { get; private set; }

        public SimpleAssignmentStatement(
            IdentifierName lhs,
            Expression rhs,
            Location location = default
        ) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }

        public override string ToString()
            => $"[simple assignment]\nlhs:\n{Indent(LHS)}\nrhs:\n{Indent(RHS)}";

        public override string ToCompactString()
            => $"<simple assignment>      {LHS.Name} = {RHS.ToCompactString()}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var childValidation = LHS.ValidateTree(path.Append(this))
                .Concat(RHS.ValidateTree(path.Append(this)));

            var notSimpleDiagnostic = DiagnosticRules.InternalSimpleAssignmentNotSimple(this);

            // Check the stuff above.
            if (IsSimple(RHS)) { } // always fine
            else if (RHS is PrefixUnaryExpression unary) {
                if (!IsSimple(unary.Expression))
                    childValidation = childValidation.Append(notSimpleDiagnostic);
            } else if (RHS is BinaryExpression binary) {
                if (!IsSimple(binary.LHS) || !IsSimple(binary.RHS))
                    childValidation = childValidation.Append(notSimpleDiagnostic);
                if (binary.LHS is LiteralExpression && binary.RHS is LiteralExpression)
                    childValidation = childValidation.Append(DiagnosticRules.InternalSimpleAssBinopCannotBeConst(this));
            } else if (RHS is IndexExpression ind) {
                if (!IsSimple(ind.Expression) || ind.Index.Entries.Any(e => !IsSimple(e)))
                    childValidation = childValidation.Append(notSimpleDiagnostic);
                // really, *so much* needs to not throw an error for const[index]
                // to happen, but for completeness' sake, lets check it
                if (ind.Expression is LiteralExpression)
                    childValidation = childValidation.Append(DiagnosticRules.InternalSimpleAssIndexCannotBeConst(this));
            } else if (RHS is MatrixExpression mat) {
                foreach (var e in mat.Entries)
                    if (!IsSimple(e)) {
                        childValidation = childValidation.Append(notSimpleDiagnostic);
                        break;
                    }
            } else if (RHS is PolarExpression pol) {
                if (!IsSimple(pol.Angle) || !IsSimple(pol.Radius))
                    childValidation = childValidation.Append(notSimpleDiagnostic);
            } else {
                throw new NotImplementedException($"Unrecognised node type {RHS.GetType()}");
            }
            return childValidation;
        }

        public SimpleAssignmentStatement WithLHS(IdentifierName identifier)
            => new(identifier, RHS, Location);
        public SimpleAssignmentStatement WithRHS(Expression expression)
            => new(LHS, expression, Location);

        static bool IsSimple(Expression expression)
            => expression is IdentifierName or LiteralExpression;
    }
}
