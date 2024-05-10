using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrite:
    /// <list type="bullet">
    /// <item>
    /// <b>All</b> simple assignments
    /// <code>
    ///     name = a;
    ///     name = ∘a;
    ///     name = a∘b;
    ///     name = a[b];
    ///     name = [a ... b];
    ///     name = [a : b];
    /// </code>
    /// turn into emittable nodes. Here a and b may be identifiers or literals.
    /// <br/>
    /// Exception: For binops, not both may be literals, and for indexing, `a`
    /// may also not be a literal.
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b>
    /// The tree must be in statement form. All expressions are either one of
    /// the above, or IntrinsicInvocation[X]Statements. AcknowledgeIntrinsicRewriter
    /// has already run.
    /// </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> There are no more non-emittable statements involving expressions. </item>
    /// </list>
    /// </remarks>
    // Do I want one node, or many? I have to do an ugly split *at some point*,
    // but is not doing it here correct?
    internal class AcknowledgeSimpleAssignmentsRewriter : AbstractTreeRewriter {

        protected override Node? VisitStatement(Statement node) {
            if (node is not ExpressionStatement expr)
                return base.VisitStatement(node);
            // By AcknowledgeIntrinsicRewriter being run, the only thing that
            // remains for expressionstatements is assignments.
            if (expr.Statement is not AssignmentExpression ass)
                throw NotSimpleException;
            return VisitAssignmentExpression(ass);
        }

        protected override Node? VisitAssignmentExpression(AssignmentExpression node) {
            var rhs = node.RHS;
            if (IsSimple(rhs)
                || (rhs is PrefixUnaryExpression unary
                    && IsSimple(unary.Expression))
                || (rhs is BinaryExpression binary && IsSimple(binary.LHS)
                    && IsSimple(binary.RHS)
                    && !(binary.LHS is LiteralExpression && binary.RHS is LiteralExpression))
                || (rhs is IndexExpression ind
                    && IsSimple(ind.Expression)
                    && ind.Index.Entries.All(e => IsSimple(e))
                    && ind.Expression is not LiteralExpression)
                || (rhs is MatrixExpression mat
                    && mat.Entries.All(e => IsSimple(e)))
                || (rhs is PolarExpression pol
                    && IsSimple(pol.Angle)
                    && IsSimple(pol.Radius))
            ) {
                return new SimpleAssignmentStatement(node.LHS, rhs);
            }
            throw NotSimpleException;
        }

        protected override Node? VisitIntrinsicInvocationAssignmentStatement(IntrinsicInvocationAssignmentStatement node)
            => node;
        protected override Node? VisitIntrinsicInvocationStatement(IntrinsicInvocationStatement node)
            => node;
        // By the above two, this is only reached when there's non-intrinsic invocations somewhere.
        protected override Node? VisitInvocationExpression(InvocationExpression node)
            => throw new VisitorAssumptionFailedException("Assumed only intrinsic invocations.");

        protected override Node? VisitRoot(Root node) {
            if (node.Declarations.Count > 0)
                throw new VisitorAssumptionFailedException("Assumed the tree was in statements form.");
            return base.VisitRoot(node);
        }

        private static bool IsSimple(Expression node)
            => node is IdentifierName or LiteralExpression;

        private static VisitorAssumptionFailedException NotSimpleException
            => new("Assumed the tree is simple -- the RHS of an assignment may be only ONE expression, with all arguments identifiers or literals.");
    }
}