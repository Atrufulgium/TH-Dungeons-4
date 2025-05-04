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
    /// <item><b>ASSUMPTIONS AFTER:</b> There is no arithmetic containing only literals. </item>
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

            // Strings are exceptional.
            // Not that anyone *ever* triggers this branch.
            // But it's still needed for the assumption that there is no
            // `"literal" == "literal"` anywhere.
            if ((node.OP == BinaryOp.Eq || node.OP == BinaryOp.Neq)
                && node.LHS is LiteralExpression str1 && str1.StringValue != null
                && node.RHS is LiteralExpression str2 && str2.StringValue != null) {
                bool res = str1.StringValue == str2.StringValue;
                res ^= node.OP == BinaryOp.Neq;
                return str1.WithFloatValue(res ? 1 : 0);
            }

            return node;
        }

        protected override Node? VisitInvocationExpression(InvocationExpression node) {
            var symbolInfo = Model.GetSymbolInfo(node);
            node = (InvocationExpression)base.VisitInvocationExpression(node)!;
            if (!symbolInfo.IsIntrinsic) {
                return node;
            }

            // Exception: Other than what's listed below, we can also do matrix
            // size getters here.
            if (symbolInfo.FullyQualifiedName is "mcols(matrix)" or "mrows(matrix)") {
                var argType = Model.GetExpressionType(node.Arguments[0]);
                if (!argType.TryGetMatrixSize(out var size))
                    throw new InvalidOperationException("`mcols`/`mrows` argument somehow wasn't a matrix?");

                return new LiteralExpression(
                    symbolInfo.FullyQualifiedName == "mcols(matrix)"
                        ? size.cols
                        : size.rows
                );
            }

            // Most math intrinsics have opcodes only allowing variable args,
            // and not literals. In this case, we _have_ to constant-fold them
            // for the emitter to be able to do anything.
            // This is fairly painful LOL.
            // Note that multi-arg intrinsics usually allow some literal args.
            // We can of course only fold if all args are literal.

            List<float> args = new(node.Arguments.Count);

            foreach (var a in node.Arguments) {
                if (a is LiteralExpression alit && alit.FloatValue.HasValue) {
                    args.Add(alit.FloatValue.Value);
                    continue;
                }
                return node;
            }

            return symbolInfo.FullyQualifiedName switch {
                "sin(float)" => new LiteralExpression(MathF.Sin(args[0])),
                "cos(float)" => new LiteralExpression(MathF.Cos(args[0])),
                "tan(float)" => new LiteralExpression(MathF.Tan(args[0])),
                "asin(float)" => new LiteralExpression(MathF.Asin(args[0])),
                "acos(float)" => new LiteralExpression(MathF.Acos(args[0])),
                "atan(float)" => new LiteralExpression(MathF.Atan(args[0])),
                "atan2(float,float)" => new LiteralExpression(MathF.Atan2(args[0], args[1])),
                // TODO: Check these two
                "angle2rad(float)" => new LiteralExpression((1.75f - args[0]) * MathF.Tau % MathF.Tau),
                "rad2angle(float)" => new LiteralExpression((1.75f - args[0] / MathF.Tau) % 1),

                "ceil(float)" => new LiteralExpression(MathF.Ceiling(args[0])),
                "floor(float)" => new LiteralExpression(MathF.Floor(args[0])),
                "round(float)" => new LiteralExpression(MathF.Round(args[0])),
                
                "abs(float)" => new LiteralExpression(MathF.Abs(args[0])),
                "length(float)" => new LiteralExpression(MathF.Abs(args[0])),
                "distance(float,float)" => new LiteralExpression(MathF.Abs(args[0] - args[1])),
                _ => node
            };
        }
    }
}
