using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrite:
    /// <list type="bullet">
    /// <item>
    /// Equality and comparison operators inside not statements
    /// <code>
    ///     !(a == b)
    ///     !(a != b)
    ///     !(a &lt;= b)
    ///     !(a &lt; b)
    ///     !(a &gt;= b)
    ///     !(a &gt; b)
    /// </code>
    /// turn into
    /// <code>
    ///     a != b
    ///     a == b
    ///     a &gt; b
    ///     a &gt;= b
    ///     a &lt; b
    ///     a &lt;= b
    /// </code>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> None. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> None. </item>
    /// </list>
    /// </remarks>
    internal class SimplifyNotRewriter : AbstractTreeRewriter {

        readonly Dictionary<BinaryOp, BinaryOp> negateMap = new() {
            { BinaryOp.Eq, BinaryOp.Neq },
            { BinaryOp.Neq, BinaryOp.Eq },
            { BinaryOp.Lte, BinaryOp.Gt },
            { BinaryOp.Lt, BinaryOp.Gte },
            { BinaryOp.Gte, BinaryOp.Lt },
            { BinaryOp.Gt, BinaryOp.Lte },
        };

        protected override Node? VisitPrefixUnaryExpression(PrefixUnaryExpression node) {
            if (node.OP != PrefixUnaryOp.Not
                || node.Expression is not BinaryExpression bin
                || !negateMap.ContainsKey(bin.OP))
                return base.VisitPrefixUnaryExpression(node);
            return bin.WithOP(negateMap[bin.OP]);
        }
    }
}
