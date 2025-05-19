using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrites:
    /// <list type="bullet">
    /// <item>
    /// Finite repeat loops
    /// <code>
    ///     repeat(count) {
    ///         ...
    ///     }
    /// </code>
    /// turn into
    /// <code>
    ///     float temp = count;
    ///     repeat {
    ///         temp--;
    ///         if (temp &gt;= 0) {
    ///             ...
    ///             continue;
    ///         }
    ///         break;
    ///     }
    /// </code>
    /// </item>
    /// <item>
    /// While loops
    /// <code>
    ///     while(condition) {
    ///         ...
    ///     }
    /// </code>
    /// turn into
    /// <code>
    ///     repeat {
    ///         if (condition) {
    ///             ...
    ///             continue;
    ///         }
    ///         break;
    ///     }
    /// </code>
    /// </item>
    /// <item>
    /// For loops
    /// <code>
    ///     for ([init]; cond; [incr]) {
    ///         ...
    ///     }
    /// </code>
    /// turn into
    /// </item>
    /// <code>
    ///     [init];
    ///     [float temp_first = true];
    ///     repeat {
    ///         [if (temp_first) {
    ///             temp_first = false;
    ///         } else {
    ///             incr;
    ///         }]
    ///         if (condition) {
    ///             ...
    ///             continue;
    ///         }
    ///         break;
    ///     }
    /// </code>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> There are no goto control flow structures. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> The only loop mechanism remaining is <c>repeat { .. }</c></item>
    /// </list>
    /// </remarks>
    // The for loop in particular is a bit whack because I don't have goto
    // labels yet. Maybe I should reorder this.
    // It's correct though. That's what matters.
    internal class LoopsToEternalRepeatRewriter : AbstractTreeRewriter {

        int identifierID = 0;
        IdentifierName GetLoopTemp()
            => new($"looptemp#{identifierID++}");

        protected override Node? VisitRepeatStatement(RepeatStatement node) {
            node = (RepeatStatement) base.VisitRepeatStatement(node)!;
            if (node.Count == null)
                return node;

            var name = GetLoopTemp();
            return new MultipleStatements(
                // float temp = count;
                new LocalDeclarationStatement(
                    new VariableDeclaration(
                        name,
                        Syntax.Type.Float,
                        initializer: node.Count
                    )
                ),
                // repeat {
                node.WithCount(null)
                    .WithBody(
                    new Block(
                        // temp--;
                        new ExpressionStatement(
                            new PostfixUnaryExpression(name, PostfixUnaryOp.Decrement)
                        ),
                        // if (temp >= 0) { ... continue; }
                        new IfStatement(
                            new BinaryExpression(
                                name,
                                BinaryOp.Gte,
                                new LiteralExpression(0)
                            ),
                            node.Body.WithAppendedStatements(
                                new ContinueStatement()
                            )
                        ),
                        // break;
                        new BreakStatement()
                    )
                )
            );
        }

        protected override Node? VisitWhileStatement(WhileStatement node) {
            node = (WhileStatement) base.VisitWhileStatement(node)!;
            return new RepeatStatement(
                // repeat {
                new Block(
                    // if (condition) { ... continue; }
                    new IfStatement(
                        node.Condition,
                        node.Body.WithAppendedStatements(
                            new ContinueStatement()
                        )
                    ),
                    // break;
                    new BreakStatement()
                )
            );
        }

        protected override Node? VisitForStatement(ForStatement node) {
            node = (ForStatement) base.VisitForStatement(node)!;
            // Conditionally on `node` having `incr`, represents the inner
            // [if (temp_first) {
            //     temp_first = false;
            // } else {
            //     incr;
            // }]
            // if (condition) { ... continue; }
            //
            // The outer inits come later.
            var loopBodyPrefix = new MultipleStatements();
            var name = GetLoopTemp();
            if (node.Increment != null) {
                loopBodyPrefix = loopBodyPrefix.WithAppendedStatements(
                    new IfStatement(
                        name,
                        new Block(
                            new ExpressionStatement(
                                new AssignmentExpression(
                                    name, AssignmentOp.Set, new LiteralExpression(0)
                                )
                            )
                        ),
                        falseBranch: new Block(
                            new ExpressionStatement(node.Increment)
                        )
                    )
                );            }
            loopBodyPrefix = loopBodyPrefix.WithAppendedStatements(
                new IfStatement(
                    node.Condition,
                    node.Body.WithAppendedStatements(
                        new ContinueStatement()
                    )
                )
            );
            var loopBody = new Block(
                new BreakStatement()
            ).WithPrependedStatements(loopBodyPrefix.Statements);
            var repeatStatement = new RepeatStatement(loopBody);
            // Now the [init] and [temp_first = true] optionals.
            var ret = new MultipleStatements();
            if (node.Initializer != null)
                ret = ret.WithAppendedStatements(node.Initializer);
            if (node.Increment != null)
                ret = ret.WithAppendedStatements(
                    new LocalDeclarationStatement(
                        new VariableDeclaration(name, Syntax.Type.Float, initializer: new LiteralExpression(1))
                    )
                );
            return ret.WithAppendedStatements(repeatStatement);
        }

        protected override Node? VisitConditionalGotoStatement(ConditionalGotoStatement node)
            => throw new VisitorAssumptionFailedException("Assume only user-code nodes exist.");
        protected override Node? VisitGotoLabelStatement(GotoLabelStatement node)
            => throw new VisitorAssumptionFailedException("Assume only user-code nodes exist.");
        protected override Node? VisitGotoStatement(GotoStatement node)
            => throw new VisitorAssumptionFailedException("Assume only user-code nodes exist.");
    }
}
