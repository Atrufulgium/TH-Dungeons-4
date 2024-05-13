using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrites:
    /// <list type="bullet">
    /// <item>
    /// Method definitions
    /// <code>
    ///     type method( .. ) {
    ///         ..
    ///     }
    /// </code>
    /// turn into
    /// <code>
    ///     type method#return;
    ///     void method( .. ) {
    ///         ..
    ///     }
    /// </code>
    /// </item>
    /// <item>
    /// Non-empty return statements
    /// <code>
    ///     return expression;
    /// </code>
    /// (re)turn into
    /// <code>
    ///     method#return = expression;
    ///     return;
    /// </code>
    /// </item>
    /// <item>
    /// Method calls whose returns are used
    /// <code>
    ///     identifier = method( .. );
    ///     // and similar
    /// </code>
    /// turn into
    /// <code>
    ///     method();
    ///     identifier = method#return;
    ///     // and similar
    /// </code>
    /// <br/>
    /// Exception: intrinsics are unaffected.
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> ExtractMethodArgsRewriter has run, and all branched return. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> Idem, and all methods are void-typed. </item>
    /// </list>
    /// </remarks>
    internal class ExtractReturnRewriter : AbstractTreeRewriter {

        static IdentifierName ReturnName(MethodSymbol symbol) => new($"{symbol.FullyQualifiedName}#return");

        // What to rename each parameter to when putting it outside the method.
        IdentifierName currentReturnName = new("");
        protected override Node? VisitMethodDeclaration(MethodDeclaration node) {
            var symbol = Model.GetSymbolInfo(node);
            currentReturnName = ReturnName(symbol);

            // Don't do anything when void.
            if (symbol.Type == Syntax.Type.Void)
                return base.VisitMethodDeclaration(node);
            
            node = (MethodDeclaration)base.VisitMethodDeclaration(node)!;
            return new MultipleDeclarations(
                new VariableDeclaration(currentReturnName, symbol.Type),
                node.WithType(Syntax.Type.Void)
            );
        }

        protected override Node? VisitReturnStatement(ReturnStatement node) {
            node = (ReturnStatement)base.VisitReturnStatement(node)!;
            if (node.ReturnValue == null)
                return node;
            return new MultipleStatements(
                new ExpressionStatement(
                    new AssignmentExpression(
                        currentReturnName,
                        AssignmentOp.Set,
                        node.ReturnValue
                    )
                ),
                new ReturnStatement()
            );
        }

        // The logic here is entirely analogous to the logic in
        /// <see cref="ExtractMethodArgsRewriter"/>
        // so see those comments.
        bool insideInvocation = false;
        readonly List<Statement> prependedStatements = new();

        protected override Node? VisitStatement(Statement node) {
            prependedStatements.Clear();

            node = (Statement)base.VisitStatement(node)!;

            // *Usually* we want to extract the returned value with the name.
            // However, if we're directly an invocation without assignment,
            // we end up with `Identifier;`. This is nonsense.
            if (node is ExpressionStatement e && e.Statement is IdentifierName)
                return new MultipleStatements(prependedStatements);

            if (prependedStatements.Count == 0)
                return node;
            return new MultipleStatements(
                new MultipleStatements(prependedStatements),
                node
            );
        }

        protected override Node? VisitInvocationExpression(InvocationExpression node) {
            if (insideInvocation)
                throw new VisitorAssumptionFailedException("Assumed no invocation is contained in another.");

            var methodSymbol = Model.GetSymbolInfo(node);

            insideInvocation = true;
            node = (InvocationExpression)base.VisitInvocationExpression(node)!;
            insideInvocation = false;

            // Intrinsics don't need to do anything.
            if (methodSymbol.IsIntrinsic)
                return node;

            prependedStatements.Add(
                new ExpressionStatement(node)
            );

            return ReturnName(methodSymbol);
        }

        protected override Node? VisitRoot(Root node) {
            if (node.Declarations.Count == 0)
                return node;
            return base.VisitRoot(node);
        }
    }
}
