using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrites:
    /// <list type="bullet">
    /// <item>
    /// Method calls (standalone!) (non-intrinsic)
    /// <code>
    ///     ..
    ///     my_method();
    ///     ..
    /// </code>
    /// turn into
    /// <code>
    ///     ..
    ///     my_method#entry = UNIQUEID; // Also added before with other root-variable decls!
    ///     goto my_method;
    ///     my_method#return#UNIQUEID:
    ///     ..
    /// </code>
    /// </item>
    /// <item>
    /// Method declarations
    /// <code>
    ///     function void my_method( .. ) {
    ///         ..
    ///     }
    /// </code>
    /// turn into
    /// <code>
    ///     my_method:
    ///     ..
    ///     my_method#return:
    ///     float return#test = my_method#entry == ID0;
    ///     if (return#test) goto my_method#return#ID0;
    ///     float return#test = my_method#entry == ID1;
    ///     if (return#test) goto my_method#return#ID1;
    ///     etc
    /// </code>
    /// </item>
    /// <item>
    /// Returns (always empty)
    /// <code>
    ///     return;
    /// </code>
    /// turn into
    /// <code>
    ///     goto my_method#return;
    /// </code>
    /// </item>
    /// <item>
    /// Root level variable declarations
    /// <code>
    ///     type name [= value];
    /// </code>
    /// get hoisted to the top of the file.
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> Methods do not significantly use arguments, and return void. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> There are no methods. </item>
    /// </list>
    /// </remarks>
    // TODO: Add labels so that main is respected when starting at instruction 0.
    // Alternatively, for setup, the VM has to run everything until it encounters
    // a method label naturally.
    internal class RemoveMethodsRewriter : AbstractTreeRewriter {

        // The approach here is a little different from the usual trees.
        // The root calls children as usual, but VisitDeclaration does not
        // return "properly".
        // It instead writes to a list of statements that will eventually be
        // the root's new statements.
        // This is necessary as we go from one type of root (the declatory kind)
        // to a completely different type of root (the statement kind) and my
        // abstractions are bad.

        readonly List<Statement> variableDeclarationStatements = new();
        readonly List<Statement> methodDeclarationStatements = new();

        readonly Dictionary<MethodSymbol, int> callCount = new();
        readonly Dictionary<MethodSymbol, int> callsProcessed = new();

        readonly Dictionary<MethodSymbol, GotoLabelStatement[]> entryLabels = new();
        readonly Dictionary<MethodSymbol, GotoLabelStatement> methodLabels = new();

        readonly Dictionary<MethodSymbol, IdentifierName> methodEntryVariableName = new();

        protected override Node? VisitRoot(Root node) {
            if (node.Declarations.Count == 0)
                return node; // Nothing to do

            // The fun path
            // First some prep: each method call needs a unique ID.
            // The method itself needs to handle those.
            // For this we have `calCount` and `callsProcessed`.
            new PrepWalker(callCount, methodLabels, Model).Visit(node);
            foreach (var s in callCount.Keys) {
                callsProcessed[s] = 0;
                GotoLabelStatement[] sEntryLabels = new GotoLabelStatement[callCount[s]];
                for (int i = 0; i < callCount[s]; i++) {
                    sEntryLabels[i] = new($"{s.FullyQualifiedName}#return-to-entry#{i}");
                }
                entryLabels[s] = sEntryLabels;
                methodEntryVariableName[s] = new($"{s.FullyQualifiedName}#entry");
            }

            node = (Root)base.VisitRoot(node)!;
            return node.WithRootLevelStatements(
                variableDeclarationStatements
                .Concat(methodDeclarationStatements)
                .ToList()
            );
        }

        // Edge case to take into account:
        bool inLocalDeclaration = false;
        protected override Node? VisitDeclaration(Declaration node) {
            if (inLocalDeclaration)
                return base.VisitDeclaration(node);
            if (node is MethodDeclaration m) {
                VisitMethodDeclaration(m);
                return null;
            }
            if (node is VariableDeclaration v) {
                variableDeclarationStatements.Add(
                    new LocalDeclarationStatement(v)
                );
                VisitVariableDeclaration(v);
                return null;
            }
            throw new ArgumentException($"Unknown node type {node.GetType()}");
        }

        protected override Node? VisitLocalDeclarationStatement(LocalDeclarationStatement node) {
            inLocalDeclaration = true;
            node = (LocalDeclarationStatement)base.VisitLocalDeclarationStatement(node)!;
            inLocalDeclaration = false;
            return node;
        }

        GotoLabelStatement currentReturnTarget = new("");
        protected override Node? VisitMethodDeclaration(MethodDeclaration node) {
            if (node.Type != Syntax.Type.Void)
                throw new VisitorAssumptionFailedException("Assumed methods return void.");

            var methodSymbol = Model.GetSymbolInfo(node);
            foreach (var a in methodSymbol.Parameters)
                if (a.ReadFrom)
                    throw new VisitorAssumptionFailedException("Assumed method parameters are unused.");

            currentReturnTarget = new($"{methodSymbol.FullyQualifiedName}#return");

            var methodEntryVarName = methodEntryVariableName[methodSymbol];
            variableDeclarationStatements.Add(
                new LocalDeclarationStatementWithoutInit(
                    methodEntryVarName, Syntax.Type.Float
                )
            );

            var body = (Block)base.VisitBlock(node.Body)!;

            methodDeclarationStatements.Add(methodLabels[methodSymbol]);
            foreach (var statement in body.Statements) {
                if (statement is MultipleStatements ms)
                    methodDeclarationStatements.AddRange(ms.Flatten());
                else
                    methodDeclarationStatements.Add(statement);
            }
            methodDeclarationStatements.Add(currentReturnTarget);
            var retEntryLabels = entryLabels[methodSymbol];
            IdentifierName returnTest = new("global#returntest");
            methodDeclarationStatements.Add(
                new LocalDeclarationStatementWithoutInit(
                    returnTest, Syntax.Type.Float
                )
            );
            for (int i = 0; i < retEntryLabels.Length; i++) {
                // The number in the label and `i` here match.
                methodDeclarationStatements.Add(
                    new ExpressionStatement(
                        new AssignmentExpression(
                            returnTest,
                            AssignmentOp.Set,
                            new BinaryExpression(
                                methodEntryVarName,
                                BinaryOp.Eq,
                                new LiteralExpression(i)
                            )
                        )
                    )
                );
                methodDeclarationStatements.Add(
                    new ConditionalGotoStatement(
                        returnTest,
                        retEntryLabels[i]
                    )
                );
            }
            return null;
        }

        protected override Node? VisitReturnStatement(ReturnStatement node)
            => new GotoStatement(currentReturnTarget);

        // (Visited via ExpressionStatement instead.)
        protected override Node? VisitInvocationExpression(InvocationExpression node)
            => throw new VisitorAssumptionFailedException("Assumed invocations are completely on their own.");

        protected override Node? VisitExpressionStatement(ExpressionStatement expressionStatement) {
            if (expressionStatement.Statement is not InvocationExpression node)
                return base.VisitExpressionStatement(expressionStatement);

            var methodSymbol = Model.GetSymbolInfo(node);
            if (methodSymbol.IsIntrinsic)
                return node;

            int i = callsProcessed[methodSymbol];
            callsProcessed[methodSymbol] += 1;

            return new MultipleStatements(
                new ExpressionStatement(
                    new AssignmentExpression(
                        methodEntryVariableName[methodSymbol],
                        AssignmentOp.Set,
                        new LiteralExpression(i)
                    )
                ),
                new GotoStatement(methodLabels[methodSymbol]),
                entryLabels[methodSymbol][i]
            );
        }

        // For correctness all we need to do is simply _count_ the number of
        // invocations for each method.
        // Note that the symbol info is no good as that's symbol-wise and not
        // node-wise. One symbol might represent multiple calls.
        // We also need unambiguous labels for every method.
        private class PrepWalker : AbstractTreeWalker {
            readonly Dictionary<MethodSymbol, int> callCount = new();
            readonly Dictionary<MethodSymbol, GotoLabelStatement> labels = new();
            public bool HasMain { get; private set; } = false;

            public PrepWalker(
                Dictionary<MethodSymbol, int> callCount,
                Dictionary<MethodSymbol, GotoLabelStatement> labels,
                SemanticModel model
            ) {
                this.callCount = callCount;
                this.labels = labels;
                Model = model;
            }

            protected override void VisitInvocationExpression(InvocationExpression node) {
                var symbol = Model.GetSymbolInfo(node); // (intrinsic? sure whatever)
                if (callCount.TryGetValue(symbol, out var value))
                    callCount[symbol] = value + 1;
                else
                    callCount[symbol] = 1;
                base.VisitInvocationExpression(node);
            }

            // To ensure that methods that aren't called don't throw errors,
            // also add those.
            // Also have a unique goto label for every method.
            protected override void VisitMethodDeclaration(MethodDeclaration node) {
                var symbol = Model.GetSymbolInfo(node);
                if (!callCount.ContainsKey(symbol))
                    callCount[symbol] = 0;
                labels[symbol] = new(symbol.FullyQualifiedName);
                if (symbol.FullyQualifiedName is "main(float)")
                    HasMain = true;
                base.VisitMethodDeclaration(node);
            }
        }
    }
}
