using Atrufulgium.BulletScript.Compiler.Semantics.SemanticVisitors;
using Atrufulgium.BulletScript.Compiler.Syntax;
using System.Collections.ObjectModel;

namespace Atrufulgium.BulletScript.Compiler.Semantics {
    /// <summary>
    /// Represents not what is written but gives actual interpretation/meaning
    /// of nodes in a tree.
    /// </summary>
    internal class SemanticModel {

        /// <summary>
        /// Diagnostics that appeared when parsing the syntax tree into a
        /// symbol table.
        /// </summary>
        public IReadOnlyCollection<Diagnostic> Diagnostics { get; private init; }
        readonly List<Diagnostic> diagnostics;

        /// <summary>
        /// Whether the parsing of the syntax tree went without encountering
        /// error diagnostics. If <c>true</c>, this semantic model is safe to
        /// be queried.
        /// </summary>
        public bool Valid => SymbolTable != null;
        SymbolTable? SymbolTable { get; set; } = null;

        // Empty model.
        private SemanticModel() {
            diagnostics = new();
            Diagnostics = new ReadOnlyCollection<Diagnostic>(diagnostics);
            SymbolTable = null;
        }

        public SemanticModel(Root root) {
            diagnostics = new();
            Diagnostics = new ReadOnlyCollection<Diagnostic>(diagnostics);

            PartialSymbolTable table = new();
            AddIntrinsicVariablesRewriter pass1 = new(table);
            root = (Root)pass1.Visit(root)!;
            diagnostics.AddRange(pass1.Diagnostics);
            if (pass1.Diagnostics.ContainsErrors())
                return;

            GetRootLevelTypeDeclarationsWalker pass2 = new(table);
            pass2.Visit(root);
            diagnostics.AddRange(pass2.Diagnostics);
            if (pass2.Diagnostics.ContainsErrors())
                return;

            GetStatementInformationWalker pass3 = new(table);
            pass3.Visit(root);
            diagnostics.AddRange(pass3.Diagnostics);
            if (pass3.Diagnostics.ContainsErrors())
                return;

            SymbolTable = table.ToSymbolTable(root);
            var pass4diags = SymbolTable.TestIllegalCallChains(SymbolTable);
            diagnostics.AddRange(pass4diags);
            if (pass4diags.ContainsErrors()) {
                SymbolTable = null;
                return;
            }
        }

        public ISymbol? GetSymbolInfo(Node node) {
            CheckValidModel();
            return SymbolTable!.GetSymbolInfo(node);
        }

        // Only `IdentifierName` isn't simple as invocation names are also
        // identifiers and don't have corresponding info.
        // The rest is trivial and always works.
        public MethodSymbol GetSymbolInfo(InvocationExpression node)
            => (MethodSymbol)GetSymbolInfo((Node)node)!;

        public string ToString(bool includeCompilerSymbols)
            => SymbolTable?.ToString(includeCompilerSymbols) ?? "(Empty table.)";
        public override string ToString()
            => SymbolTable?.ToString() ?? "(Empty table.)";

        public static SemanticModel Empty => new();

        private void CheckValidModel() {
            if (!Valid)
                throw new InvalidOperationException("The semantic model is not valid and cannot answer any semantic questions.");
        }
    }
}
