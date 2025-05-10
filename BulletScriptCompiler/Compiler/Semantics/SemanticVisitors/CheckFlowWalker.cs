using Atrufulgium.BulletScript.Compiler.Syntax;
using Atrufulgium.BulletScript.Compiler.Visitors;
using System.Collections.Generic;

namespace Atrufulgium.BulletScript.Compiler.Semantics.SemanticVisitors {
    /// <summary>
    /// Checks the following:
    /// <list type="bullet">
    /// <item> Whether non-void methods have all branches return (otherwise, error). </item>
    /// <item> Whether there are any statements after returns (otherwise, warning). </item>
    /// </list>
    /// </summary>
    // I really wanted to warn infinite loops too, but they're annoying.
    // `wait()` loops aren't really infinite loops in the same way all others
    // are, and they may be called indirectly, making them hard to recognise.
    internal class CheckFlowWalker : AbstractTreeWalker {

        // The approach is simple:
        // - Every block keeps track whether it itself returns.
        // - Then, if blocks are dependent on other blocks (e.g. if/else),
        //   coarser blocks take into account the result of the finer blocks.
        readonly Stack<Block> blocks = new();
        readonly HashSet<Block> returningBlocks = new();
        readonly HashSet<Block> warnedUnreachableBlocks = new();

        Block CurrentBlock => blocks.Peek();

        protected override void VisitRoot(Root node) {
            if (node.RootLevelStatements.Count > 0)
                return;

            base.VisitRoot(node);
        }

        protected override void VisitMethodDeclaration(MethodDeclaration node) {
            // No reason to visit the arguments.
            // (They're statements and actually throw errors as the block stack
            //  is empty!)
            VisitBlock(node.Body);

            if (node.Type == Syntax.Type.Void)
                return;

            if (!returningBlocks.Contains(node.Body))
                AddDiagnostic(DiagnosticRules.NotAllBranchesReturn(node));
        }

        protected override void VisitBlock(Block node) {
            blocks.Push(node);
            base.VisitBlock(node);
            blocks.Pop();
        }

        protected override void VisitIfStatement(IfStatement node) {
            base.VisitIfStatement(node);
            // If this is an if-else and both branches return, the block returns.
            if (node.FalseBranch != null) {
                if (returningBlocks.Contains(node.TrueBranch)
                    && returningBlocks.Contains(node.FalseBranch)) {
                    returningBlocks.Add(CurrentBlock);
                }
            }
        }

        protected override void VisitReturnStatement(ReturnStatement node) {
            returningBlocks.Add(CurrentBlock);
        }

        protected override void VisitStatement(Statement node) {
            if (returningBlocks.Contains(CurrentBlock)
                && !warnedUnreachableBlocks.Contains(CurrentBlock)) {
                AddDiagnostic(DiagnosticRules.UnreachableCode(node));
                warnedUnreachableBlocks.Add(CurrentBlock);
            }
            base.VisitStatement(node);
        }
    }
}
