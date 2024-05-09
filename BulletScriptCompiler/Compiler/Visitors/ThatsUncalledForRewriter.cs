using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrite:
    /// <list type="bullet">
    /// <item>
    /// Uncalled methods (transitively)
    /// <code>
    ///     function A() { B(); }
    ///     function B() { }
    /// </code>
    /// get removed.
    /// <br/>
    /// The exception are the methods the VM calls directly, such as <c>main()</c>.
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
    internal class ThatsUncalledForRewriter : AbstractTreeRewriter {

        protected override Node? VisitRoot(Root node) {
            if (node.RootLevelStatements.Count > 0)
                return node;

            // Do passes until nothing gets removed.
            var semanticModel = Model;
            while (true) {
                IVisitor rewriter = new RemovalPassRewriter() { Model = semanticModel };
                rewriter.Visit(node);
                if (!((RemovalPassRewriter)rewriter).RemovedAnything)
                    break;
                node = (Root)rewriter.VisitResult!;
                semanticModel = new(node);
            }
            return node;
        }


        private class RemovalPassRewriter : AbstractTreeRewriter {

            public bool RemovedAnything { get; private set; } = false;

            protected override Node? VisitMethodDeclaration(MethodDeclaration node) {
                var symbol = Model.GetSymbolInfo(node);
                // MethodSymbol.CalledBy only references bulletscript-internal
                // calls. The VM calls are disregarded, so that is an extra check.
                if (symbol.IsSpecialMethod)
                    return node;
                if (symbol.CalledBy.Count == 0) {
                    RemovedAnything = true;
                    return null;
                }
                return node;
            }

            // Other stuff doesn't need to be handled so quick return w/o children
            protected override Node? VisitVariableDeclaration(VariableDeclaration node)
                => node;
        }
    }
}
