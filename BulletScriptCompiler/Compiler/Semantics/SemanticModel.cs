using Atrufulgium.BulletScript.Compiler.Semantics.SemanticVisitors;
using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Semantics {
    /// <summary>
    /// Represents not what is written but gives actual interpretation/meaning
    /// of nodes in a tree.
    /// </summary>
    internal class SemanticModel {

        public SemanticModel(Root root) {
            PartialSymbolTable table = new();
            AddIntrinsicVariablesRewriter pass1 = new(table);
            root = (Root)pass1.Visit(root)!;
            GetRootLevelTypeDeclarationsWalker pass2 = new(table);
            pass2.Visit(root);
        }
    }
}
