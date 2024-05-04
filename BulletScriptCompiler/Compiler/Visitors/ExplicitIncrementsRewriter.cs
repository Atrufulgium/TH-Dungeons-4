using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrite:
    /// <list type="bullet">
    /// <item>
    /// Increments and decrements
    /// <code>
    ///     name++;
    ///     name--;
    /// </code>
    /// turn into
    /// <code>
    ///     name = name + 1;
    ///     name = name - 1;
    /// </code>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> None. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> There are no more `++` and `--` operators. </item>
    /// </list>
    /// </remarks>
    internal class ExplicitIncrementsRewriter : AbstractTreeRewriter {

        protected override Node? VisitPostfixUnaryExpression(PostfixUnaryExpression node) {
            BinaryOp? convertedOp = null;
            if (node.OP == PostfixUnaryOp.Increment) {
                convertedOp = BinaryOp.Add;
            } else if (node.OP == PostfixUnaryOp.Decrement) {
                convertedOp = BinaryOp.Sub;
            }
            if (convertedOp == null)
                return base.VisitPostfixUnaryExpression(node);

            // We're name++ or name--.
            var id = (IdentifierName)node.Expression;
            return new AssignmentExpression(
                id,
                AssignmentOp.Set,
                new BinaryExpression(
                    id,
                    convertedOp.Value,
                    new LiteralExpression(1)
                )
            );
        }
    }
}