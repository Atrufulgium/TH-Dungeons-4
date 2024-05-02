using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Semantics {
    internal partial class PartialSymbolTable {
        /// <summary>
        /// Converts this partial table to a fully-fledged symbol table.
        /// <br/>
        /// Requires all data to be present and complete. This is not verified.
        /// </summary>
        public SymbolTable ToSymbolTable(Root root) {
            Dictionary<Node, ISymbol> result = new();
            Dictionary<string, ISymbol> fqnResult = new();
            // We need these because we cannot update the symbol directly.
            Dictionary<string, IList<MethodSymbol>> symbolCalledBys = new();
            Dictionary<string, IList<MethodSymbol>> symbolCalls = new();

            // First add all variable symbols, and set everything except for
            // the containing symbol.
            foreach (var fqn in originalDeclarations.Keys) {
                var decl = originalDeclarations[fqn];
                if (decl is VariableDeclaration variable) {
                    fqnResult[fqn] = new VariableSymbol(
                        fqn,
                        originalDeclarations[fqn],
                        null,
                        types[fqn],
                        readVariables.Contains(fqn),
                        writtenVariables.Contains(fqn)
                    );
                } else if (decl != null && decl is not MethodDeclaration) {
                    throw new ArgumentException($"Unhandled case {decl.GetType()}.");
                }
            }

            // Now add all method symbols, referencing variable symbols as
            // arguments if needed.
            foreach (var fqn in originalDeclarations.Keys) {
                if (originalDeclarations[fqn] is MethodDeclaration method) {
                    List<VariableSymbol> argSymbols = new();
                    List<MethodSymbol> calledBy = new();
                    List<MethodSymbol> calls = new();

                    foreach (var arg in arguments[fqn]) {
                        // TODO: This access fails exactly for the intrinsic methods.
                        // For now, 
                        /// <see cref="IntrinsicData.ApplyIntrinsicMethods(PartialSymbolTable)"/>
                        // is commented out, but this should be fixed top priority.
                        argSymbols.Add((VariableSymbol)fqnResult[GetFullyQualifiedName(arg, method)]);
                    }

                    fqnResult[fqn] = new MethodSymbol(
                        fqn,
                        method,
                        types[fqn],
                        argSymbols,
                        calledBy,
                        calls
                    );

                    symbolCalledBys[fqn] = calledBy;
                    symbolCalls[fqn] = calls;
                }
            }

            // Now fix the fact that the variable symbols don't have their
            // containing methods written yet.
            foreach (var fqn in originalDeclarations.Keys) {
                if (originalDeclarations[fqn] is VariableDeclaration variable) {
                    VariableSymbol symbol = (VariableSymbol)fqnResult[fqn];
                    
                    var containingMethod = containingMethods[fqn];
                    if (containingMethod != null) {
                        var methodFqn = GetFullyQualifiedName(containingMethod);
                        VariableSymbol.AddContainingSymbol(
                            symbol,
                            (MethodSymbol)fqnResult[methodFqn]
                        );
                    }

                    VariableSymbol.Seal(symbol);
                }
            }
            
            // Finally, ensure the CalledBy and Calls lists are correct.
            foreach (var (source, target) in callGraph) {
                symbolCalls    [source].Add((MethodSymbol)fqnResult[target]);
                symbolCalledBys[target].Add((MethodSymbol)fqnResult[source]);
            }

            // We now have everything we need to convert `fqnResult` into `result`.
            foreach (var kv in symbolNameMap)
                result.Add(kv.Key, fqnResult[kv.Value]);

            return SymbolTable.Create(root, result);
        }
    }
}
