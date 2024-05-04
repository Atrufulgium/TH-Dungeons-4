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

            var methodSymbol = (MethodSymbol)Model.GetSymbolInfo(node)!;
            node = (MethodDeclaration)base.VisitMethodDeclaration(node)!;

            // Conveniently updates everything but `main` and `on_message`.
            if (!methodSymbol.IsSpecialMethod)
                node = node.WithArguments(Array.Empty<LocalDeclarationStatement>());

            return new MultipleDeclarations(
                new MultipleDeclarations(addedDeclarations),
                node
            );
        }

        // Not overriding Invocation as that is part of a this.
        // Doing a this directly is more convenient then.
        // By assumption, all invocations are part of an ExpressionStatement.
        // Any stragglers will turn into a semantic error afterwards.
        protected override Node? VisitExpressionStatement(ExpressionStatement expressionStatement) {
            if (expressionStatement.Statement is not InvocationExpression node)
                return base.VisitExpressionStatement(expressionStatement);

            // Hooray, we're relevant.
            var methodSymbol = Model.GetSymbolInfo(node);
            if (methodSymbol.IsIntrinsic)
                return base.VisitInvocationExpression(node);

            // The fun part
            var argSymbols = methodSymbol.Parameters.ToList();
            node = (InvocationExpression)base.VisitInvocationExpression(node)!;

            List<Statement> prependedStatements = new();
            var args = node.Arguments;
            for (int i = 0; i < args.Count; i++) {
                prependedStatements.Add(
                    new ExpressionStatement(
                        new AssignmentExpression(
                            GetUpdatedVarName(argSymbols[i]),
                            AssignmentOp.Set,
                            args[i]
                        )
                    )
                );
            }

            node = node.WithArguments(Array.Empty<Expression>());
            if (methodSymbol.IsSpecialMethod && methodSymbol.Parameters.Count == 1)
                node = node.WithArguments(new List<Expression> { new LiteralExpression(0) });

            return new MultipleStatements(
                new MultipleStatements(prependedStatements),
                new ExpressionStatement(node)
            );
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
