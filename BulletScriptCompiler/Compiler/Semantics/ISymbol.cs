using Atrufulgium.BulletScript.Compiler.Syntax;
using System.Collections.ObjectModel;

namespace Atrufulgium.BulletScript.Compiler.Semantics {
    /// <summary>
    /// An interface representing the meaning of a node.
    /// </summary>
    internal interface ISymbol {
        /// <summary>
        /// The symbol's fully qualified name.
        /// For instance, a method local gets that prefixed.
        /// </summary>
        public string FullyQualifiedName { get; }

        /// <summary>
        /// Where the original definition of this symbol is:
        /// <list type="bullet">
        /// <item>
        /// For variables, this is the <i>first</i> <see cref="VariableDeclaration"/>
        /// that names this symbol. May be null if introduced by the compiler.
        /// </item>
        /// <item>
        /// For methods, this is the <see cref="MethodDeclaration"/> that names
        /// this symbol. Is null for intrinsics.
        /// </item>
        /// <item>
        /// For types, this is null.
        /// </item>
        /// </list>
        /// </summary>
        public Declaration? OriginalDefinition { get; }

        /// <summary>
        /// If this symbol is contained in another, gives the other symbol.
        /// Otherwise, it is null.
        /// </summary>
        public MethodSymbol? ContainingSymbol { get; }

        /// <summary>
        /// The type of this symbol.
        /// </summary>
        public Syntax.Type Type { get; }
    }

    /// <summary>
    /// A symbol representing the meaning of a variable.
    /// </summary>
    internal class VariableSymbol : ISymbol {
        public string FullyQualifiedName { get; private set; }
        public Declaration? OriginalDefinition { get; private set; }
        public MethodSymbol? ContainingSymbol { get; private set; }
        public Syntax.Type Type { get; private set; }
        /// <summary>
        /// Whether this variable is read from at any point.
        /// </summary>
        public bool ReadFrom { get; private set; }
        /// <summary>
        /// Whether this variable is written to at any point <i>after</i>
        /// declaration.
        /// </summary>
        public bool WrittenTo { get; private set; }

        bool seal = false;

        public VariableSymbol(
            string fullyQualifiedName,
            Declaration? originalDefinition,
            MethodSymbol? containingSymbol,
            Syntax.Type type,
            bool readFrom,
            bool writtenTo
        ) {
            FullyQualifiedName = fullyQualifiedName;
            OriginalDefinition = originalDefinition;
            ContainingSymbol = containingSymbol;
            Type = type;
            ReadFrom = readFrom;
            WrittenTo = writtenTo;
        }

        // A hack because there's a cyclic dependency between method symbols
        // having parameters and variables having a containing symbol.
        public static void AddContainingSymbol(VariableSymbol symbol, MethodSymbol? containingSymbol) {
            if (symbol.ContainingSymbol != null)
                throw new ArgumentException("May only add a containing symbol when it's null.");
            if (symbol.seal)
                throw new ArgumentException("This symbol has been sealed and can no longer be modified.");
            symbol.ContainingSymbol = containingSymbol;
        }

        public static void Seal(VariableSymbol symbol) => symbol.seal = true;
    }

    /// <summary>
    /// A symbol representing the meaning of a method.
    /// </summary>
    internal class MethodSymbol : ISymbol {
        public string FullyQualifiedName { get; private set; }
        public Declaration? OriginalDefinition { get; private set; }
        public MethodSymbol? ContainingSymbol => null;
        public Syntax.Type Type { get; private set; }

        /// <summary>
        /// Whether this is an intrinsic method that does not have a definition
        /// in bulletscript code.
        /// </summary>
        public bool Intrinsic => OriginalDefinition == null;
        /// <summary>
        /// In order, all arguments of this method.
        /// </summary>
        public IReadOnlyCollection<VariableSymbol> Parameters { get; private set; }
        /// <summary>
        /// What methods this method is called by (directly).
        /// </summary>
        public IReadOnlyCollection<MethodSymbol> CalledBy { get; private set; }
        /// <summary>
        /// What methods this method calls (directly).
        /// </summary>
        public IReadOnlyCollection<MethodSymbol> Calls { get; private set; }

        public MethodSymbol(
            string fullyQualifiedName,
            Declaration? originalDefinition,
            Syntax.Type returnType,
            IList<VariableSymbol> parameters,
            IList<MethodSymbol> calledBy,
            IList<MethodSymbol> calls
        ) {
            FullyQualifiedName = fullyQualifiedName;
            OriginalDefinition = originalDefinition;
            Type = returnType;
            Parameters = new ReadOnlyCollection<VariableSymbol>(parameters);
            CalledBy = new ReadOnlyCollection<MethodSymbol>(calledBy);
            Calls = new ReadOnlyCollection<MethodSymbol>(calls);
        }
    }
}
