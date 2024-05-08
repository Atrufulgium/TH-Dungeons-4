using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrite:
    /// <list type="bullet">
    /// <item>
    /// Constrant expressions
    /// <code>
    ///     1 + 2 * 3 / 4 % 5 ^ 6
    /// </code>
    /// turn into their evaluated values
    /// <code>
    ///     2.5
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
    internal class ConstantFoldRewriter : AbstractTreeRewriter {

        protected override Node? VisitPrefixUnaryExpression(PrefixUnaryExpression node) {
            node = (PrefixUnaryExpression)base.VisitPrefixUnaryExpression(node)!;
            if (node.Expression is LiteralExpression lit && lit.FloatValue is float f) {
                return node.OP.Handle(
                    errorFunc: () => throw new NotSupportedException("Encountered error operator?"),
                    negFunc: () => lit.WithFloatValue(-f),
                    notFunc: () => lit.WithFloatValue(f == 0 ? 1 : 0)
                );
            }
            return node;
        }

        // no postfixes as those are currently statement-ish only

        protected override Node? VisitBinaryExpression(BinaryExpression node) {
            node = (BinaryExpression)base.VisitBinaryExpression(node)!;
            if (node.LHS is LiteralExpression lit1 && lit1.FloatValue is float f1
                && node.RHS is LiteralExpression lit2 && lit2.FloatValue is float f2) {
                return node.OP.Handle(
                    errorFunc: () => throw new NotSupportedException("Encountered error operator?"),
                    addFunc: () => lit1.WithFloatValue(f1 + f2),
                    subFunc: () => lit1.WithFloatValue(f1 - f2),
                    mulFunc: () => lit1.WithFloatValue(f1 * f2),
                    divFunc: () => lit1.WithFloatValue(f1 / f2),
                    modFunc: () => lit1.WithFloatValue(f1 % f2),
                    powFunc: () => lit1.WithFloatValue(MathF.Pow(f1, f2)),
                    andFunc: () => lit1.WithFloatValue(f1 != 0 && f2 != 0 ? 1 : 0),
                    orFunc:  () => lit1.WithFloatValue(f1 != 0 || f2 != 0 ? 1 : 0),
                    eqFunc:  () => lit1.WithFloatValue(f1 == f2 ? 1 : 0),
                    neqFunc: () => lit1.WithFloatValue(f1 != f2 ? 1 : 0),
                    gteFunc: () => lit1.WithFloatValue(f1 >= f2 ? 1 : 0),
                    gtFunc:  () => lit1.WithFloatValue(f1 > f2 ? 1 : 0),
                    lteFunc: () => lit1.WithFloatValue(f1 <= f2 ? 1 : 0),
                    ltFunc:  () => lit1.WithFloatValue(f1 < f2 ? 1 : 0)
                );
            }
            return node;
        }
    }
}
