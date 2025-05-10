using Atrufulgium.BulletScript.Compiler.Syntax;
using Atrufulgium.BulletScript.Compiler.Visitors;
using System.Linq;

namespace Atrufulgium.BulletScript.Compiler.Semantics.SemanticVisitors {
    /// <summary>
    /// Symbol analysis is easiest in a few passes:
    /// <list type="number">
    /// <item> Add intrinsic information. </item>
    /// <item> Get all top-level function and variable info. </item>
    /// <item> Go through all function bodies line by line. </item>
    /// <item> Collate this partial data into a proper symbol table. </item>
    /// </list>
    /// This represents the first pass.
    /// </summary>
    internal class AddIntrinsicVariablesRewriter : AbstractTreeRewriter {

        public AddIntrinsicVariablesRewriter(PartialSymbolTable table) {
            IntrinsicData.ApplyIntrinsicMethods(table);
        }

        protected override Node? VisitRoot(Root node) {
            if (node.Declarations.Count > 0)
                return node.WithDeclarations(
                    IntrinsicData.GetIntrinsicVariables().Concat(node.Declarations).ToList()
                );
            var intrinsicStatements = IntrinsicData.GetIntrinsicVariables().Select(
                v => new LocalDeclarationStatement(v, v.Location)
            );
            return node.WithRootLevelStatements(
                intrinsicStatements.Concat(node.RootLevelStatements).ToList()
            );
            // (no base visit because we don't care)
        }
    }
}
