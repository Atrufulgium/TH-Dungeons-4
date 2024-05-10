using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrite:
    /// <list type="bullet">
    /// <item>
    /// Compound assignments
    /// <code>
    ///     a ∘= ..;
    /// </code>
    /// turn into
    /// <code>
    ///     a = a ∘ ..;
    /// </code>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> None. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> All assignments are simplly `set`. </item>
    /// </list>
    /// </remarks>
    internal class SimpleAssignmentRewriter : AbstractTreeRewriter {

        protected override Node? VisitAssignmentExpression(AssignmentExpression node) {
            node = (AssignmentExpression)base.VisitAssignmentExpression(node)!;
            if (node.OP == AssignmentOp.Set)
                return node;
            
            if (!AssignmentOp.TryGetBinop(node.OP, out var binop))
                throw new ArgumentException($"Unconvertible compound assignment `{node.OP}=`.");
            
            return node
                .WithOP(AssignmentOp.Set)
                .WithRHS(
                new BinaryExpression(
                    node.LHS,
                    binop,
                    node.RHS
                )
            );
        }

    }
}
