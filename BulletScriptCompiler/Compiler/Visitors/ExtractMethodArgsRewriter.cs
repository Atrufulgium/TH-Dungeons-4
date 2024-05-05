using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrites:
    /// <list type="bullet">
    /// <item>
    /// Method definitions
    /// <code>
    ///     type method(type1 arg1, .., typeN argN) {
    ///         ..
    ///     }
    /// </code>
    /// turn into
    /// <code>
    ///     type1 method_arg1;
    ///     ..
    ///     typeN method_argN;
    ///     type method() {
    ///         ..
    ///     }
    /// </code>
    /// where <c>method_argN</c> is the fully qualified name of <c>argN</c>.
    /// All variables inside the method also get updated.
    /// <br/>
    /// Exception: `main(float value)` and `on_message(float value)` get similar
    /// rewrites but <i>without</i> removing the argument from the definition.
    /// <br/>
    /// The argument is copied to outside as with all other methods, and
    /// ignored thereafter in favour of the copy.
    /// </item>
    /// <item>
    /// Method calls
    /// <code>
    ///     method(arg1, .., argN)
    /// </code>
    /// turn into
    /// <code>
    ///     method_arg1 = arg_1;
    ///     ..
    ///     method_argN = arg_N;
    ///     method();
    /// </code>
    /// similar to the above.
    /// <br/>
    /// Exception: calling `main` or `on_message` has the arguments `0`.
    /// <br/>
    /// Intrinsics are unaffected.
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> Methods calls are not contained in one another. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> Idem, and methods mostly have no arguments anymore. </item>
    /// </list>
    /// </remarks>
    internal class ExtractMethodArgsRewriter : AbstractTreeRewriter {

        // What to rename each parameter to when putting it outside the method.
        readonly Dictionary<VariableSymbol, IdentifierName> identifierNameUpdates = new();
        readonly List<Declaration> addedDeclarations = new();
        protected override Node? VisitMethodDeclaration(MethodDeclaration node) {
            identifierNameUpdates.Clear();
            addedDeclarations.Clear();

            var methodSymbol = Model.GetSymbolInfo(node);

            // Don't give updates for intrinsics
            foreach (var arg in node.Arguments) {
                // if null, just throw...
                // it should be impossible to be null
                var symbol = (VariableSymbol)(Model.GetSymbolInfo(arg.Declaration.Identifier) ?? throw new ArgumentException("Okay how is this null"));
                var update = GetUpdatedVarName(symbol);
                identifierNameUpdates[symbol] = update;
                addedDeclarations.Add
                    (new VariableDeclaration(
                        update,
                        arg.Declaration.Type
                    )
                );
            }

            node = (MethodDeclaration)base.VisitMethodDeclaration(node)!;

            // Conveniently updates everything but `main` and `on_message`.
            if (!methodSymbol.IsSpecialMethod)
                node = node.WithArguments(Array.Empty<LocalDeclarationStatement>());

            return new MultipleDeclarations(
                new MultipleDeclarations(addedDeclarations),
                node
            );
        }

        // Not overriding Invocation as you'd expected as then we'd return a
        // MultipleStatement in a place that's not supported.
        // Instead, keep track of a list of added statements and replace
        // invocations with an empty list.
        // This is guaranteed to go well as methods are not contained in one
        // another.
        bool insideInvocation = false;
        readonly List<Statement> prependedStatements = new();

        protected override Node? VisitStatement(Statement node) {
            prependedStatements.Clear();
            node = (Statement)base.VisitStatement(node)!;

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

            var args = node.Arguments;
            for (int i = 0; i < args.Count; i++) {
                prependedStatements.Add(
                    new ExpressionStatement(
                        new AssignmentExpression(
                            GetUpdatedVarName(methodSymbol.Parameters[i]),
                            AssignmentOp.Set,
                            args[i]
                        )
                    )
                );
            }

            node = node.WithArguments(Array.Empty<Expression>());
            // The `main` and `on_message` exceptions.
            if (methodSymbol.IsSpecialMethod && methodSymbol.Parameters.Count == 1)
                node = node.WithArguments(new List<Expression> { new LiteralExpression(0) });

            return node;
        }

        protected override Node? VisitIdentifierName(IdentifierName node) {
            var symbol = Model.GetSymbolInfo(node);
            // can actually be null! and a method! ガーン!
            // When wrong, this may be a method's identifier instead.
            if (symbol == null || symbol is MethodSymbol)
                return node;
            if (identifierNameUpdates.TryGetValue((VariableSymbol)symbol, out var update))
                return update;
            return node;
        }

        protected override Node? VisitRoot(Root node) {
            if (node.Declarations.Count == 0)
                return node;
            return base.VisitRoot(node);
        }

        static IdentifierName GetUpdatedVarName(VariableSymbol symbol)
            => new(symbol.FullyQualifiedName.Replace('.', '#'));
    }
}
