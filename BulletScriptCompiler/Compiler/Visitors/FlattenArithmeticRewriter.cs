using Atrufulgium.BulletScript.Compiler.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrite:
    /// <list type="bullet">
    /// <item>
    /// Nested arithmetic
    /// <code>
    ///     .. a ∘ (b ∘ c) ..;
    /// </code>
    /// turns into
    /// <code>
    ///     type temp = b ∘ c;
    ///     .. a ∘ temp ..;
    /// </code>
    /// </item>
    /// of course including arbitrary nesting, and for more than just binary
    /// operations.
    /// <br/>
    /// In fact, it handles <i>all</i> expressions, and for instance also pulls
    /// matrix entries out of matrices, and invocation arguments out of calls.
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> All assignments are simple `set` assignments. There are no postfix unitaries. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> The RHS of any assignment is a single, non-nested expression. </item>
    /// </list>
    /// </remarks>
    internal class FlattenArithmeticRewriter : AbstractTreeRewriter {

        readonly Dictionary<Syntax.Type, int> varIDs = new();
        IdentifierName GetNewVar(Syntax.Type type) {
            if (!varIDs.TryGetValue(type, out int val)) {
                val = 0;
            }
            varIDs[type] = val + 1;
            return new($"arithmetic#result#{type}#{val}");
        }

        // Depth-first, whenever we encounter arithmetic beyond the first op,
        // make it a variable of the correct type, and replace the calculation
        // with a reference to that variable.
        readonly List<Statement> prependedStatements = new();
        // Bottom layer invocation does not need to be replaced.
        int layer = -1;
        protected override Node? VisitStatement(Statement node) {
            prependedStatements.Clear();
            varIDs.Clear();

            node = (Statement)base.VisitStatement(node)!;
            if (prependedStatements.Count == 0)
                return node;
            return new MultipleStatements(
                new MultipleStatements(prependedStatements),
                node
            );
        }

        protected override Node? VisitExpression(Expression node) {
            // Indices have seperate handling (to prevent the matrix being
            // converted to an identifier).
            if (node is IndexExpression i)
                return VisitIndexExpression(i);

            layer++;
            var key = node;
            node = (Expression)base.VisitExpression(node)!;
            bool bottomMostLayer = layer == 0;
            layer--;
            if (bottomMostLayer)
                return node;

            // Don't do anything to the two end results that are acceptable:
            // - Identifiers
            // - Literals
            if (node is LiteralExpression or IdentifierName)
                return node;

            // When not the bottommost layer ina expression tree, replace this
            // node with a fresh name that has been set to what this node
            // wanted to achieve, earlier than this return.

            var type = Model.GetExpressionType(key);
            IdentifierName name = GetNewVar(type);
            prependedStatements.Add(
                new MultipleStatements(
                    new LocalDeclarationStatementWithoutInit(name, type),
                    new ExpressionStatement(
                        new AssignmentExpression(
                            name,
                            AssignmentOp.Set,
                            node
                        )
                    )
                )
            );
            return name;
        }

        protected override Node? VisitIndexExpression(IndexExpression node) {
            // The LHS expression may be modified.
            // The RHS matrix should stay in place, and only have its
            // entries be updated.
            layer++;
            var ret = node.WithExpression(
                (Expression)VisitExpression(node.Expression)!
            ).WithIndex(
                node.Index.WithEntries(
                    node.Index.Entries.Select(
                        i => (Expression)VisitExpression(i)!
                    ).ToList(),
                    node.Index.Rows,
                    node.Index.Cols
                )
            );
            layer--;
            return ret;
        }

        protected override Node? VisitAssignmentExpression(AssignmentExpression node) {
            if (node.OP != AssignmentOp.Set)
                throw new VisitorAssumptionFailedException($"Assumed all assignments are simple ops `=`, but this one was `{node.OP}=`");
            // Assignments do *not* count towards the expression limit.
            // Otherwise `a = b + c` would introduce a temp.
            // That's whack.
            layer--;
            var ret = base.VisitAssignmentExpression(node);
            layer++;
            return ret;
        }

        protected override Node? VisitPostfixUnaryExpression(PostfixUnaryExpression node)
            => throw new VisitorAssumptionFailedException("Assumed there were no more postfix ++ or --.");
    }
}
