using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Semantics {
    /// <summary>
    /// Keeps track of semantic information (so far) when parsing the meaning
    /// of the syntax tree.
    /// </summary>
    internal class PartialSymbolTable {
        readonly Dictionary<string, Declaration?> originalDeclarations = new();
        readonly Dictionary<string, MethodDeclaration?> containingMethods = new();
        readonly Dictionary<string, Syntax.Type> types = new();
        // When a method
        readonly Dictionary<string, List<VariableDeclaration>> arguments = new();

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
                return DiagnosticRules.ClashingTypes(declaration, types[key], targetType);
            if (existing && declaration.GetType() != originalDeclarations[key]?.GetType())
                return DiagnosticRules.ClashingKinds(declaration);

            originalDeclarations[key] = declaration;
            containingMethods[key] = containingMethod;
            types[key] = targetType;

            if (declaration is MethodDeclaration methodDecl)
                arguments[key] = methodDecl.Arguments.Select(l => l.Declaration).ToList();
            return null;
        }

        /// <summary>
        /// Gets a fully qualified name of a declaration.
        /// This allows for multiple same-named variables in different scopes.
        /// This allows for multiple method overloads.
        /// </summary>
        static string GetFullyQualifiedName(Declaration declaration, MethodDeclaration? containingMethod) {
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

        static string GetMethodName(MethodDeclaration method) {
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
    }
}
