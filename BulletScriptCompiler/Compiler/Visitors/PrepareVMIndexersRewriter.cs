using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrite:
    /// <list type="bullet">
    /// <item>
    /// Two-argument matrix indexers
    /// <code>
    ///     my2x2Matrix[row; col]
    ///     myNxMMatrix[row; col]
    /// </code>
    /// turn into
    /// <code>
    ///     my2x2Matrix[2 * floor(row) + col]
    ///     myNxMMatrix[4 * floor(row) + col]
    /// </code>
    /// </item>
    /// <item>
    /// One-argument matrix indexers
    /// <code>
    ///     my2x2Matrix[index]
    ///     myNxMMatrix[index]
    /// </code>
    /// turn into
    /// <code>
    ///     my2x2Matrix[2 * floor(index/2) + index%2]
    ///     myNxMMatrix[4 * floor(index/M) + index%M]
    /// </code>
    /// </item>
    /// </list>
    /// This rewrite is to make indices compatible with the
    /// <see cref="HighLevelOpCodes.HLOP.IndexedGet(string, float)"/> and
    /// <see cref="HighLevelOpCodes.HLOP.IndexedGet(string, string)"/>
    /// opcodes directly.
    /// <br/>
    /// The weird dichotomy between 2x2 and NxM is because of how the VM stores
    /// matrices. Only 2x2 are compacted into a float4, while every other
    /// matrix uses one float4 per row, even if this is not efficient.
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> None. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> Matrix indexers are now semantically VM-friendly instead of matrix-friendly. </item>
    /// </list>
    /// </remarks>
    internal class PrepareVMIndexersRewriter : AbstractTreeRewriter {

        protected override Node? VisitIndexExpression(IndexExpression node) {
            var index = node.Index;

            node = (IndexExpression)base.VisitIndexExpression(node)!;

            // Only affect matrices, not vectors.
            var matrixType = Model.GetExpressionType(node.Expression);
            if (matrixType.TryGetVectorSize(out var _))
                return node;

            if (matrixType.TryGetMatrixSize(out var size)) {
                var indices = index.Entries.Count;

                Expression row, col;
                if (indices == 1) {
                    row = new BinaryExpression(
                        node.Index.Entries[0],
                        BinaryOp.Div,
                        new LiteralExpression(size.cols)
                    );
                    col = new BinaryExpression(
                        node.Index.Entries[0],
                        BinaryOp.Mod,
                        new LiteralExpression(size.cols)
                    );
                } else {
                    row = node.Index.Entries[0];
                    col = node.Index.Entries[1];
                }

                int stride = size == (2, 2) ? 2 : 4;

                return node.WithIndex(
                    new MatrixExpression(
                        new[] {
                            new BinaryExpression(
                                new BinaryExpression(
                                    new InvocationExpression(
                                        new IdentifierName("floor"),
                                        new[] { row }
                                    ),
                                    BinaryOp.Mul,
                                    new LiteralExpression(stride)
                                ),
                                BinaryOp.Add,
                                col
                            )
                        }, 1, 1
                    )
                );
            }

            throw new InvalidOperationException("Impossible branch");
        }
    }
}
