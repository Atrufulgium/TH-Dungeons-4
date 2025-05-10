using Atrufulgium.BulletScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
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

    internal static class SymbolExtensions {
        /// <summary>
        /// Whether this symbol exists in user code, or is introduced by the
        /// compiler.
        /// </summary>
        public static bool IsCompilerIntroduced(this ISymbol symbol)
            => symbol.OriginalDefinition == null || symbol.OriginalDefinition.Location.line == 0;

        /// <summary>
        /// Tries to grab the symbol belonging to a node. If this node does not
        /// have a location (not even a compiler-introduced one), this returns
        /// <c>false</c>.
        /// </summary>
        public static bool TryGetLocation(this ISymbol symbol, out Location location) {
            location = default;
            if (symbol.OriginalDefinition == null)
                return false;
            location = symbol.OriginalDefinition.Location;
            return true;
        }
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
        /// Whether this variable is read from at any point. Automatically
        /// <c>true</c> for bullet variables such as <c>autoclear</c>, even if
        /// <c>spawn()</c> is never called.
        /// </summary>
        public bool ReadFrom { get; private set; }
        /// <summary>
        /// Whether this variable is written to at any point <i>after</i>
        /// declaration.
        /// </summary>
        public bool WrittenTo { get; private set; }

        /// <summary>
        /// Whether this symbol represents an implicit bullet variable such as
        /// <c>autoclear</c>.
        /// </summary>
        public bool IsBulletVariable => FullyQualifiedName is "autoclear"
            or "bullettype" or "clearimmune" or "clearingtype" or "harmsenemies"
            or "harmsplayers" or "spawnposition" or "spawnrelative"
            or "spawnrotation" or "spawnspeed" or "usepivot";

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
            ReadFrom |= IsBulletVariable;
        }

        public override string ToString() => FullyQualifiedName;

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
        public bool IsIntrinsic { get; private set; }
        /// <summary>
        /// In order, all arguments of this method.
        /// </summary>
        public IReadOnlyList<VariableSymbol> Parameters { get; private set; }
        /// <summary>
        /// What methods this method is called by (directly).
        /// <br/>
        /// This of course does not take into account calls from the VM
        /// directly, only the bulletscript code itself.
        /// </summary>
        public IReadOnlyCollection<MethodSymbol> CalledBy { get; private set; }
        /// <summary>
        /// What methods this method calls (directly).
        /// </summary>
        public IReadOnlyCollection<MethodSymbol> Calls { get; private set; }

        /// <summary>
        /// Whether this method is a method called directly from the VM.
        /// </summary>
        public bool IsSpecialMethod => FullyQualifiedName is "main(float)" or "on_message(float)"
            || FullyQualifiedName.StartsWith("on_health<")
            || FullyQualifiedName.StartsWith("on_time<");

        public MethodSymbol(
            string fullyQualifiedName,
            Declaration? originalDefinition,
            Syntax.Type returnType,
            IList<VariableSymbol> parameters,
            IList<MethodSymbol> calledBy,
            IList<MethodSymbol> calls,
            bool isIntrinsic = false
        ) {
            FullyQualifiedName = fullyQualifiedName;
            OriginalDefinition = originalDefinition;
            Type = returnType;
            IsIntrinsic = isIntrinsic;
            Parameters = new ReadOnlyCollection<VariableSymbol>(parameters);
            CalledBy = new ReadOnlyCollection<MethodSymbol>(calledBy);
            Calls = new ReadOnlyCollection<MethodSymbol>(calls);
        }

        public override string ToString() => FullyQualifiedName;
    }
}
