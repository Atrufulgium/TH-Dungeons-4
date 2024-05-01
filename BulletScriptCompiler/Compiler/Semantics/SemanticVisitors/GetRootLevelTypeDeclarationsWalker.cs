using Atrufulgium.BulletScript.Compiler.Syntax;
using Atrufulgium.BulletScript.Compiler.Visitors;

namespace Atrufulgium.BulletScript.Compiler.Semantics.SemanticVisitors {
    /// <summary>
    /// Symbol analysis is easiest in a few passes:
    /// <list type="number">
    /// <item> Add intrinsic information. </item>
    /// <item> Get all top-level function and variable info. </item>
    /// <item> Go through all function bodies line by line. </item>
    /// <item> Collate this partial data into a proper symbol table. </item>
    /// </list>
    /// This represents the second pass.
    /// </summary>
    internal class GetRootLevelTypeDeclarationsWalker : AbstractTreeWalker {

        readonly PartialSymbolTable table;

        public GetRootLevelTypeDeclarationsWalker(PartialSymbolTable table) => this.table = table;

        protected override void VisitRoot(Root node) {
            // No function or method declarations at all in this case.
            if (node.RootLevelStatements.Count > 0)
                return;
            base.VisitRoot(node);
        }

        protected override void VisitDeclaration(Declaration node) {
            var res = table.TryUpdate(node);
            if (res != null)
                AddDiagnostic(res);
            // No base visit as we don't care about method bodies or
            // initializers or whatnot.
        }
    }
}
