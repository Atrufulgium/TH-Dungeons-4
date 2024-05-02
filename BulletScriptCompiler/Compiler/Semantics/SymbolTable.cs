using Atrufulgium.BulletScript.Compiler.Syntax;
using Atrufulgium.BulletScript.Compiler.Visitors;
using System.Collections;

namespace Atrufulgium.BulletScript.Compiler.Semantics {
    /// <summary>
    /// Contains all metadata relating to nodes that have an underlying meaning
    /// such as variables, and methods. It contains type information, the
    /// definition, etc.
    /// </summary>
    internal class SymbolTable : IEnumerable<ISymbol> {

        readonly Dictionary<Node, ISymbol> table;
        readonly HashSet<Node> treeNodes;

        // Private with a factory to discourage creating this.
        private SymbolTable(Root root, Dictionary<Node, ISymbol> table) {
            this.table = table;
            var walker = new AllNodesWalker();
            walker.Visit(root);
            treeNodes = walker.treeNodes;
        }
        /// <summary>
        /// Create a new symbol table from given data.
        /// <br/>
        /// There is literally only one spot in the codebase where this should
        /// be needed.
        /// </summary>
        public static SymbolTable Create(Root root, Dictionary<Node, ISymbol> table) => new(root, table);

        /// <summary>
        /// Tries to get the symbol info corresponding to a node.
        /// <br/>
        /// If the node does not have a corresponding symbol, this returns
        /// <c>null</c> instead. Currently, the following nodes have attached
        /// symbolinfo:
        /// <list type="bullet">
        /// <item><see cref="IdentifierName"/>;</item>
        /// <item><see cref="InvocationExpression"/>;</item>
        /// <item><see cref="MethodDeclaration"/>;</item>
        /// <item><see cref="VariableDeclaration"/>.</item>
        /// </list>
        /// If the node is not part of the original tree, this will throw a
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        public ISymbol? GetSymbolInfo(Node node) {
            CheckInTree(node);
            if (table.ContainsKey(node))
                return table[node];
            return null;
        }

        private void CheckInTree(Node node) {
            if (!treeNodes.Contains(node))
                throw new InvalidOperationException("Node is not in tree.");
        }

        private class AllNodesWalker : AbstractTreeWalker {
            public HashSet<Node> treeNodes = new();

            public override void Visit(Node node) {
                treeNodes.Add(node);
                base.Visit(node);
            }
        }

        public override string ToString() {
            if (table.Count == 0)
                return "(Empty table.)";

            // For neatness and readability, the table is ordered as follows:
            // - First, in appearance-in-file order.
            //   Compiler introduced stuff gets put at the bottom.
            // - Second, in alphabetical order.
            SortedSet<(int priority, string line)> lines = new();
            void AddLine(Location loc, string line) {
                if (loc.line == Location.CompilerIntroduced.line) {
                    lines.Add((int.MaxValue, line));
                } else {
                    lines.Add((loc.col + loc.line * 10000, line));
                }
            }
            
            foreach (var symbol in this) {
                string comment;
                if (symbol is VariableSymbol variable) {
                    comment = (variable.ReadFrom, variable.WrittenTo) switch {
                        (true, true)  => "Read, Written",
                        (true, false) => "Read",
                        (false, true) => "Written",
                        (false, false) => ""
                    };
                } else if (symbol is MethodSymbol method) {
                    string indent = "";
                    SortedSet<string> comments = new();
                    foreach (var methodSymbol in method.Calls)
                        comments.Add($"Calls {methodSymbol.FullyQualifiedName}");
                    foreach (var methodSymbol in method.CalledBy)
                        comments.Add($"{indent}Called by {methodSymbol.FullyQualifiedName}");

                    string empty = "";
                    comment = string.Join($"\n{empty,-32} | {empty,-20} | {empty,-20} | {empty,-9} | ", comments);

                } else throw new InvalidOperationException("bleh");
                var fqn = symbol.FullyQualifiedName;
                var decLoc = symbol.OriginalDefinition?.Location.ToString() ?? "[n/a]";
                var metLoc = symbol.ContainingSymbol?.OriginalDefinition?.Location.ToString() ?? "[n/a]";
                var type = symbol.Type;
                AddLine(
                    symbol.OriginalDefinition?.Location ?? Location.CompilerIntroduced,
                    $"{fqn,-32} | {decLoc,-20} | {metLoc,-20} | {type,-9} | {comment}"
                );
            }

            string[] header = new[] { "Fully qualified name", "Declaration location", "Container location", "Type", "Notes" };
            return string.Join('\n', lines.Select(p => p.line).Prepend(
                $"\n{header[0], -32} | {header[1], -20} | {header[2], -20} | {header[3], -9} | {header[4]}"
                +
                $"\n{new('-',32)}-+-{new('-',20)}-+-{new('-',20)}-+-{new('-',9)}-+-----------"
            )) + "\n";
        }

        public IEnumerator<ISymbol> GetEnumerator() {
            foreach (var symbol in table.Values)
                yield return symbol;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
