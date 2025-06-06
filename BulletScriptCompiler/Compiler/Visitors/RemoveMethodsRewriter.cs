﻿using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

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
    /// <item>
    /// Non-root level variable declarations
    /// <code>
    ///     function void my_method( .. ) {
    ///         .. my_var ..
    ///     }
    /// </code>
    /// become fully-qualified in the flattened tree:
    /// <code>
    ///     .. my_method#my_var ..
    /// </code>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> Methods do not significantly use arguments, and return void. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> There are no methods. Labels for VM methods are surrounded by ##. </item>
    /// </list>
    /// Furthermore, compiler special methods get their goto label surrounded
    /// by pairs of `##`.
    /// <br/>
    /// (Does this rewrite do too much? Very much yes.)
    /// </remarks>
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
        readonly List<Statement> mainStatements = new(); // Main must come first so that init can smoothly go into this
        readonly List<Statement> methodDeclarationStatements = new();

        readonly Dictionary<MethodSymbol, int> callCount = new();
        readonly Dictionary<MethodSymbol, int> callsProcessed = new();

        readonly Dictionary<MethodSymbol, GotoLabelStatement[]> entryLabels = new();
        readonly Dictionary<MethodSymbol, GotoLabelStatement> methodLabels = new();

        readonly Dictionary<MethodSymbol, IdentifierName> methodEntryVariableName = new();

        readonly GotoLabelStatement endLabel = new("the end");

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
                
                // The number in the i'th label and `i+1` here match.
                // This in order to allow the VM to temporarily zero-set
                // certain variables sometimes.
                for (int i = 0; i < callCount[s]; i++) {
                    sEntryLabels[i] = new($"{s.FullyQualifiedName}#return-to-entry#{i+1}");
                }
                entryLabels[s] = sEntryLabels;
                methodEntryVariableName[s] = new($"{s.FullyQualifiedName}#entry");
            }

            node = (Root)base.VisitRoot(node)!;

            if (mainStatements.Count == 0) {
                // If there's a main we smoothly go in there from init.
                // Otherwise, we don't want to enter any method.
                mainStatements.Add(new GotoStatement(endLabel));
            }

            // Those endLabel gotos need a final resting place to go.
            methodDeclarationStatements.Add(endLabel);

            return node.WithRootLevelStatements(
                variableDeclarationStatements
                .Concat(mainStatements)
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
        bool methodHasReturn = false;
        protected override Node? VisitMethodDeclaration(MethodDeclaration node) {
            methodHasReturn = false;
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

            List<Statement> writeTarget = methodDeclarationStatements;
            if (methodSymbol.FullyQualifiedName == "main(float)")
                writeTarget = mainStatements;

            writeTarget.Add(methodLabels[methodSymbol]);
            foreach (var statement in body.Statements) {
                if (statement is MultipleStatements ms)
                    writeTarget.AddRange(ms.Flatten());
                else
                    writeTarget.Add(statement);
            }
            
            if (methodHasReturn)
                writeTarget.Add(currentReturnTarget);
            var retEntryLabels = entryLabels[methodSymbol];

            if (retEntryLabels.Length == 0) {
                writeTarget.Add(new GotoStatement(endLabel));
                return null;
            }

            IdentifierName returnTest = new("global#returntest");
            writeTarget.Add(
                new LocalDeclarationStatementWithoutInit(
                    returnTest, Syntax.Type.Float
                )
            );
            for (int i = 0; i < retEntryLabels.Length; i++) {
                // The number in the i'th label and `i+1` here match.
                writeTarget.Add(
                    new ExpressionStatement(
                        new AssignmentExpression(
                            returnTest,
                            AssignmentOp.Set,
                            new BinaryExpression(
                                methodEntryVarName,
                                BinaryOp.Eq,
                                new LiteralExpression(i+1)
                            )
                        )
                    )
                );
                writeTarget.Add(
                    new ConditionalGotoStatement(
                        returnTest,
                        retEntryLabels[i]
                    )
                );
            }

            // VM-called methods may also just, well, exist without the
            // relevant variable being set, and then the above branches are not
            // reliable.
            if (methodSymbol.IsSpecialMethod)
                writeTarget.Add(new GotoStatement(endLabel));

            return null;
        }

        protected override Node? VisitReturnStatement(ReturnStatement node) {
            methodHasReturn = true;
            return new GotoStatement(currentReturnTarget);
        }

        // (Visited via ExpressionStatement instead.)
        protected override Node? VisitInvocationExpression(InvocationExpression node)
            => Model.GetSymbolInfo(node).IsIntrinsic
            ? base.VisitInvocationExpression(node)
            : throw new VisitorAssumptionFailedException("Assumed invocations are completely on their own.");

        protected override Node? VisitExpressionStatement(ExpressionStatement expressionStatement) {
            if (expressionStatement.Statement is not InvocationExpression node)
                return base.VisitExpressionStatement(expressionStatement);

            var methodSymbol = Model.GetSymbolInfo(node);
            if (methodSymbol.IsIntrinsic)
                return base.VisitExpressionStatement(expressionStatement);

            int i = callsProcessed[methodSymbol];
            callsProcessed[methodSymbol] += 1;

            // The number in the i'th label and `i+1` here match.
            return new MultipleStatements(
                new ExpressionStatement(
                    new AssignmentExpression(
                        methodEntryVariableName[methodSymbol],
                        AssignmentOp.Set,
                        new LiteralExpression(i+1)
                    )
                ),
                new GotoStatement(methodLabels[methodSymbol]),
                entryLabels[methodSymbol][i]
            );
        }

        protected override Node? VisitIdentifierName(IdentifierName node) {
            var symbol = Model.GetSymbolInfo(node);
            if (symbol == null)
                throw new InvalidOperationException("Identifier symbols should always exist?");

            // The only variables that _can_ be fully qualified are those the
            // user introduce. So if it's a variable with null original def,
            // we're in the clear anyways.
            if (symbol.OriginalDefinition == null || symbol.OriginalDefinition is not VariableDeclaration) {
                return node;
            }
            return node.WithName(symbol.FullyQualifiedName.Replace('.', '#'));
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
                // Regular methods can have whatever symbol they want.
                // VM communicating methods get their label surrounded by `##`s.
                if (symbol.IsSpecialMethod)
                    labels[symbol] = new($"##{symbol.FullyQualifiedName}##");
                else
                    labels[symbol] = new(symbol.FullyQualifiedName);

                if (symbol.FullyQualifiedName is "main(float)")
                    HasMain = true;
                base.VisitMethodDeclaration(node);
            }
        }
    }
}
