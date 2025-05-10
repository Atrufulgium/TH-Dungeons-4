using Atrufulgium.BulletScript.Compiler.Syntax;
using System;
using System.Collections.Generic;

namespace Atrufulgium.BulletScript.Compiler.Semantics {
    internal partial class PartialSymbolTable {
        /// <summary>
        /// Converts this partial table to a fully-fledged symbol table.
        /// <br/>
        /// Requires all data to be present and complete. This is not verified.
        /// </summary>
        public SymbolTable ToSymbolTable(Root root) {
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

                    List<VariableSymbol> hackyIntrinsics = new();

                    foreach (var arg in arguments[fqn]) {
                        var argFqn = GetFullyQualifiedName(arg, method);
                        if (fqnResult.TryGetValue(argFqn, out var varSymbol)) {
                            argSymbols.Add((VariableSymbol)varSymbol);
                        } else {
                            // For intrinsic methods, the fqnResult does not contain
                            // the arguments yet. So we need to create that variable
                            // symbol this branch.
                            varSymbol = new VariableSymbol(
                                argFqn,
                                arg,
                                null,
                                arg.Type,
                                true,
                                false
                            );
                            fqnResult[argFqn] = varSymbol;
                            argSymbols.Add((VariableSymbol)varSymbol);
                            hackyIntrinsics.Add((VariableSymbol)varSymbol);
                        }
                    }

                    var methodSymbol = new MethodSymbol(
                        fqn,
                        method,
                        types[fqn],
                        argSymbols,
                        calledBy,
                        calls,
                        isIntrinsic: intrinsics.Contains(fqn)
                    );
                    fqnResult[fqn] = methodSymbol;

                    symbolCalledBys[fqn] = calledBy;
                    symbolCalls[fqn] = calls;
                    
                    // The intrinsics do not get updated below, so might as well
                    // do it now.
                    // Why did I think it was a good idea to have special nodes
                    // not in the tree, when I *know* that breaks everything...
                    // (Note that if this did get modified in both, the
                    //  `AddContainingSymbol` in the second case would throw,
                    //  so we know this is really necessary.)
                    foreach (var intrinsicArg in hackyIntrinsics) {
                        VariableSymbol.AddContainingSymbol(intrinsicArg, methodSymbol);
                        VariableSymbol.Seal(intrinsicArg);
                    }
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

            return SymbolTable.Create(root, new Dictionary<Node, string>(symbolNameMap), fqnResult);
        }
    }
}
