using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Semantics {
    /// <summary>
    /// Keeps track of semantic information (so far) when parsing the meaning
    /// of the syntax tree.
    /// </summary>
    internal partial class PartialSymbolTable {
        readonly Dictionary<string, Declaration?> originalDeclarations = new();
        readonly Dictionary<string, MethodDeclaration?> containingMethods = new();
        readonly Dictionary<string, Syntax.Type> types = new();
        // When a method
        readonly Dictionary<string, List<VariableDeclaration>> arguments = new();

        // Data I decided to add later
        readonly List<(string source, string target)> callGraph = new();
        readonly HashSet<string> readVariables = new();
        readonly HashSet<string> writtenVariables = new();
        readonly Dictionary<Node, string> symbolNameMap = new();

        /// <summary>
        /// Sets some data that is easier to set in batch format than update
        /// on the fly. Each list must contain the fully qualified names.
        /// </summary>
        /// <param name="callGraph">
        /// This collection should contain every instance of `target` being
        /// called, and include the calling method `source`.
        /// </param>
        /// <param name="readVariables">
        /// This collection should contain all variables being read from at
        /// some point.
        /// </param>
        /// <param name="writtenVariables">
        /// This collection should contain all variables being written to at
        /// some point.
        /// </param>
        /// <param name="symbolNameMap">
        /// For each node that has some semantic meaning, the name of the thing
        /// it means.
        /// </param>
        public void SetExtraData(
            IEnumerable<(string source, string target)> callGraph,
            IEnumerable<string> readVariables,
            IEnumerable<string> writtenVariables,
            Dictionary<Node, string> symbolNameMap
        ) {
            this.callGraph.Clear();
            this.callGraph.AddRange(callGraph);
            this.readVariables.Clear();
            this.readVariables.UnionWith(readVariables);
            this.writtenVariables.Clear();
            this.writtenVariables.UnionWith(writtenVariables);
            this.symbolNameMap.Clear();
            foreach (var kv in symbolNameMap)
                this.symbolNameMap.Add(kv.Key, kv.Value);
        }

        /// <summary>
        /// Tries to update a symbol in the table with the given information.
        /// <br/>
        /// If the update fails -- i.e. the given name already exists as a
        /// different symbol kind, or with incompatible type -- this returns
        /// some diagnostic. Otherwise, <c>null</c>.
        /// </summary>
        /// <param name="declaration"> The node to add to the table. </param>
        /// <param name="containingMethod"> If this is inside a method, what method it is in. </param>
        /// <param name="overridenType"> If you want to disregard the type of the declaration, set this. </param>
        public Diagnostic? TryUpdate(
            Declaration declaration,
            MethodDeclaration? containingMethod = null,
            Syntax.Type? overridenType = null
        ) {
            string key = GetFullyQualifiedName(declaration, containingMethod);
            bool existing = originalDeclarations.ContainsKey(key);

            Syntax.Type targetType = overridenType ?? declaration.Type;
            if (existing && !Syntax.Type.TypesAreCompatible(types[key], targetType))
                return DiagnosticRules.ClashingTypeDef(declaration, types[key], targetType);
            if (existing && declaration.GetType() != originalDeclarations[key]?.GetType())
                return DiagnosticRules.ClashingKindDef(declaration);
            if (declaration is MethodDeclaration methodDecl2 && methodDecl2.Type == Syntax.Type.MatrixUnspecified)
                return DiagnosticRules.ReturnMatricesNeedSize(methodDecl2);

            originalDeclarations[key] = declaration;
            containingMethods[key] = containingMethod;
            types[key] = targetType;

            if (declaration is MethodDeclaration methodDecl)
                arguments[key] = methodDecl.Arguments.Select(l => l.Declaration).ToList();
            return null;
        }

        public Syntax.Type GetType(string fullyQualifiedName) {
            bool exists = originalDeclarations.ContainsKey(fullyQualifiedName);

            if (exists)
                return types[fullyQualifiedName];
            return Syntax.Type.Error;
        }

        /// <summary>
        /// Gets a fully qualified name of a declaration.
        /// This allows for multiple same-named variables in different scopes.
        /// This allows for multiple method overloads.
        /// </summary>
        public string GetFullyQualifiedName(Declaration declaration, MethodDeclaration? containingMethod = null) {
            // Note: declarations don't need to search up whether the name
            // exists already.
            string name;
            if (declaration is MethodDeclaration methodDecl)
                name = GetMethodName(methodDecl);
            else if (declaration is VariableDeclaration varDecl)
                name = varDecl.Identifier.Name;
            else
                throw new ArgumentException($"Unhandled case {declaration.GetType()}.");

            if (containingMethod == null)
                return name;
            return $"{GetMethodName(containingMethod)}.{name}";
        }

        string GetMethodName(MethodDeclaration method) {
            string name = method.Identifier.Name + "(";
            bool first = true;
            foreach (var arg in method.Arguments) {
                if (!first)
                    name += ",";
                else
                    first = false;

                name += arg.Declaration.Type;
            }
            name += ")";
            return name;
        }

        /// <summary>
        /// Gets a fully qualified name of a method, as determined by its name
        /// and arguments.
        /// </summary>
        public string GetFullyQualifiedMethodName(IdentifierName node, IEnumerable<Syntax.Type> argTypes) {
            string name = node.Name + "(";
            bool first = true;
            foreach (var arg in argTypes) {
                if (!first)
                    name += ",";
                else
                    first = false;
                name += arg;
            }
            name += ")";

            return name;
        }

        /// <summary>
        /// Gets a fully qualified name of a variable.
        /// </summary>
        public string GetFullyQualifiedName(IdentifierName node, MethodDeclaration? containingMethod = null) {
            string name = node.Name;
            if (containingMethod != null)
                name = $"{GetMethodName(containingMethod)}.{name}";
            bool exists = originalDeclarations.ContainsKey(name);
            if (exists)
                return name;

            // If it does not exist, try higher scopes.
            var testKey = name;
            while (true) {
                int i = testKey.IndexOf('.');
                if (i == -1)
                    break;
                testKey = testKey[(i + 1)..];
                if (originalDeclarations.ContainsKey(testKey)) {
                    name = testKey;
                    exists = true;
                    break;
                }
            }

            return name;
        }
    }
}
