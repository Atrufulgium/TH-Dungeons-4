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

        readonly Dictionary<Node, string> node2fqn;
        readonly Dictionary<string, ISymbol> fqn2symbol;
        readonly Dictionary<ISymbol, string> symbol2fqn;
        readonly HashSet<Node> treeNodes;

        // Private with a factory to discourage creating this.
        private SymbolTable(Root root, Dictionary<Node, string> node2fqn, Dictionary<string, ISymbol> fqn2symbol) {
            this.node2fqn = node2fqn;
            this.fqn2symbol = fqn2symbol;
            var reverseKvs = fqn2symbol.Select(kv => new KeyValuePair<ISymbol, string>(kv.Value, kv.Key));
            symbol2fqn = new(reverseKvs);

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
        public static SymbolTable Create(Root root, Dictionary<Node, string> node2fqn, Dictionary<string, ISymbol> fqn2symbol)
            => new(root, node2fqn, fqn2symbol);

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
            if (node2fqn.ContainsKey(node))
                return fqn2symbol[node2fqn[node]];
            return null;
        }

        readonly Dictionary<ISymbol, int> refCount = new();
        /// <summary>
        /// Returns how often this symbol is referenced in code.
        /// These references may include references in inaccessible code.
        /// <br/>
        /// This is <i>excluding</i> the definition itself.
        /// <br/>
        /// This is <i>including</i> indirect references of the VM:
        /// <list type="bullet">
        /// <item>
        /// Bullet variables such as <c>autoclear</c> are referenced +1 for
        /// each <c>spawn()</c>-call.
        /// </item>
        /// <item>
        /// Special methods <c>main()</c>, <c>on_message()</c>, <c>on_health&lt;..&gt;</c>
        /// and <c>on_time&lt;..&gt;</c> automatically get +1.
        /// </item>
        /// </list>
        /// </summary>
        public int GetRefCount(ISymbol symbol) {
            if (refCount.TryGetValue(symbol, out int count))
                return count;
            var fqn = symbol2fqn[symbol];

            count = 0;
            foreach (var node in node2fqn.Keys) {
                if (node is Declaration)
                    continue;
                if (fqn == node2fqn[node])
                    count++;
            }

            // special cases
            if (symbol is MethodSymbol m && m.IsSpecialMethod)
                count++;
            if (symbol is VariableSymbol v && v.IsBulletVariable)
                count += GetRefCount(fqn2symbol["spawn()"]);

            refCount[symbol] = count;
            return count;
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

        public override string ToString()
            => ToString(true);

        public string ToString(bool includeCompilerSymbols) {
            if (fqn2symbol.Count == 0)
                return "(Empty table.)";

            int fqnWidth = 32;
            foreach (var symbol in this)
                if (includeCompilerSymbols || !symbol.IsCompilerIntroduced())
                    fqnWidth = Math.Max(fqnWidth, symbol.FullyQualifiedName.Length);

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
                if (!includeCompilerSymbols && symbol.IsCompilerIntroduced())
                    continue;

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
                    comment = string.Join($"\n{empty.PadRight(fqnWidth)} | {empty,-20} | {empty,-20} | {empty,-9} | {empty,-4} | ", comments);

                } else throw new InvalidOperationException("bleh");
                var fqn = symbol.FullyQualifiedName;
                var decLoc = symbol.OriginalDefinition?.Location.ToString() ?? "[n/a]";
                var metLoc = symbol.ContainingSymbol?.OriginalDefinition?.Location.ToString() ?? "[n/a]";
                var type = symbol.Type;
                var refCount = GetRefCount(symbol);
                AddLine(
                    symbol.OriginalDefinition?.Location ?? Location.CompilerIntroduced,
                    $"{fqn.PadRight(fqnWidth)} | {decLoc,-20} | {metLoc,-20} | {type,-9} | {refCount,4} | {comment}"
                );
            }

            string[] header = new[] { "Fully qualified name", "Declaration location", "Container location", "Type", "Refs", "Notes" };
            return string.Join('\n', lines.Select(p => p.line).Prepend(
                $"\n{header[0].PadRight(fqnWidth)} | {header[1], -20} | {header[2], -20} | {header[3], -9} | {header[4],-4} | {header[5]}"
                +
                $"\n{new('-', fqnWidth)}-+-{new('-',20)}-+-{new('-',20)}-+-{new('-',9)}-+-{new('-',4)}-+----------"
            )) + "\n";
        }

        public IEnumerator<ISymbol> GetEnumerator() {
            foreach (var symbol in fqn2symbol.Values)
                yield return symbol;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// This checks for whether there is recursion or whether `wait()` is
        /// called by any VM event method.
        /// </summary>
        public static List<Diagnostic> TestIllegalCallChains(SymbolTable table) {
            List<List<MethodSymbol>> cycles = new();
            SortedSet<MethodSymbol> todo = new(
                Comparer<MethodSymbol>.Create(
                    (m1, m2) => m1.FullyQualifiedName.CompareTo(m2.FullyQualifiedName)
                )
            );
            foreach (var s in table.symbol2fqn.Keys) {
                if (s is MethodSymbol m)
                    todo.Add(m);
            }

            while (todo.Min != null) {
                RecursionVisit(todo.Min, new() { todo.Min });
            }

            List<List<MethodSymbol>> waitReaches = new();
            HashSet<MethodSymbol> seen = new();
            if (table.fqn2symbol.TryGetValue("wait(float)", out var waitSymbol))
                WaitVisit((MethodSymbol)waitSymbol, new());

            return cycles.Select(l => GetCycleError(l))
                .Concat(waitReaches.Select(l => GetWaitCallError(l)))
                .ToList();


            
            // This implementation is lame but eh.
            // The call graph won't be complex enough for it to matter.
            void RecursionVisit(MethodSymbol node, List<MethodSymbol> pathSoFar) {
                todo.Remove(node);
                foreach (var call in node.Calls) {
                    List<MethodSymbol> pathWithNode = new(pathSoFar) { call };
                    if (todo.Contains(call))
                        RecursionVisit(call, pathWithNode);
                    else {
                        // Seen this node already -- in this branch?
                        int i = pathSoFar.IndexOf(call);
                        if (i >= 0) {
                            pathWithNode = pathWithNode.GetRange(i, pathWithNode.Count - i);
                            cycles.Add(pathWithNode);
                        }
                    }
                }
            }

            // As opposed to the previous one, this one goes backwards.
            void WaitVisit(MethodSymbol node, List<MethodSymbol> pathSoFar) {
                seen.Add(node);
                var pathWithNode = new List<MethodSymbol>(pathSoFar) {
                    node
                };
                if (node.IsSpecialMethod && node.FullyQualifiedName != "main(float)") {
                    waitReaches.Add(pathWithNode);
                    return;
                }
                foreach (var called in node.CalledBy)
                    if (!seen.Contains(called))
                        WaitVisit(called, pathWithNode);
            }

            Diagnostic GetCycleError(List<MethodSymbol> cycle) {
                // Grab a random location, idc
                Location loc = Location.CompilerIntroduced;
                foreach (var symbol in cycle) {
                    if (symbol.TryGetLocation(out loc) && loc.line > 0)
                        break;
                }
                return DiagnosticRules.RecursiveCall(loc, cycle);
            }

            Diagnostic GetWaitCallError(List<MethodSymbol> waitPath) {
                // Note that this path is backwards at this point.
                if (!waitPath[1].TryGetLocation(out var loc))
                    throw new InvalidOperationException("The wait method is not called from user code, is that even allowed?");
                waitPath.Reverse();
                return DiagnosticRules.IllegalWaitCall(loc, waitPath);
            }
        }
    }
}
