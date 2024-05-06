using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrites:
    /// <list type="bullet">
    /// <item>
    /// Intrinsic assignments
    /// <code>
    ///     name = intrinsic(args);
    /// </code>
    /// turn into
    /// <code>
    ///     name = intrinsics(args);
    /// </code>
    /// (but as an emittable node).
    /// </item>
    /// <item>
    /// Intrinsic assignments with operators
    /// <code>
    ///     name ∘= intrinsic(args);
    /// </code>
    /// turn into
    /// <code>
    ///     type global#intrinsic#temp#type;
    ///     global#intrinsic#temp#type = intrinsic(args);
    ///     name ∘= global#intrinsic#temp#type;
    /// </code>
    /// (with the intrinsic as an emittable node).
    /// </item>
    /// <item>
    /// Intrinsic void calls
    /// <code>
    ///     intrinsic(args);
    /// </code>
    /// turn into
    /// <code>
    ///     intrinsic(args);
    /// </code>
    /// (with the intrinsic as an emittable node).
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b>
    /// The only methods remaining are intrinsics, and they are in one of the above forms.
    /// Furthermore, arguments are either identifiers or literals.
    /// The tree must be in statement form.
    /// </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> There are no more invocations not part of an IntrinsicX node. </item>
    /// </list>
    /// </remarks>
    internal class AcknowledgeIntrinsicsRewriter : AbstractTreeRewriter {

        protected override Node? VisitExpressionStatement(ExpressionStatement expressionStatement) {
            if (expressionStatement.Statement is InvocationExpression call)
                return HandleVoidCall(call);
            if (expressionStatement.Statement is AssignmentExpression assignment
                && assignment.RHS is InvocationExpression)
                return HandleAssCall(assignment);

            return base.VisitExpressionStatement(expressionStatement);
        }

        Statement HandleVoidCall(InvocationExpression node) {
            var methodSymbol = Model.GetSymbolInfo(node);
            if (!methodSymbol.IsIntrinsic)
                throw new VisitorAssumptionFailedException("Assumed all invocations are intrinsic.");

            // Not explicitely an _error_, but they don't do anything, so return nothing.
            if (methodSymbol.Type != Syntax.Type.Void)
                return new MultipleStatements();

            foreach (var arg in node.Arguments)
                if (arg is not IdentifierName or LiteralExpression)
                    throw new VisitorAssumptionFailedException("Assumed all invocation arguments are identifiers or literals.");

            return new IntrinsicInvocationStatement(node);
        }

        Statement HandleAssCall(AssignmentExpression assignment) {
            var node = (InvocationExpression)assignment.RHS;
            var methodSymbol = Model.GetSymbolInfo(node);
            if (!methodSymbol.IsIntrinsic)
                throw new VisitorAssumptionFailedException("Assumed all invocations are intrinsic.");

            foreach (var arg in node.Arguments)
                if (arg is not (IdentifierName or LiteralExpression))
                    throw new VisitorAssumptionFailedException("Assumed all invocation arguments are identifiers or literals.");

            if (assignment.OP == AssignmentOp.Set)
                return new IntrinsicInvocationAssignmentStatement(
                    assignment.LHS,
                    node
                );

            var globalID = new IdentifierName($"global#intrinsic#temp#{methodSymbol.Type}");
            return new MultipleStatements(
                new LocalDeclarationStatementWithoutInit(
                    globalID,
                    methodSymbol.Type
                ),
                new IntrinsicInvocationAssignmentStatement(
                    globalID,
                    node
                ),
                new ExpressionStatement(assignment.WithRHS(globalID))
            );
        }

        // (We're not visiting the assumed invocations, but leaving that to
        //  AssignmentExpression and ExpressionStatement instead.)
        protected override Node? VisitInvocationExpression(InvocationExpression node)
            => throw new VisitorAssumptionFailedException("Assumed invocation in very specific places.");

        protected override Node? VisitRoot(Root node) {
            if (node.Declarations.Count > 0)
                throw new VisitorAssumptionFailedException("Assumed the tree was in statements form.");
            return base.VisitRoot(node);
        }
    }
}