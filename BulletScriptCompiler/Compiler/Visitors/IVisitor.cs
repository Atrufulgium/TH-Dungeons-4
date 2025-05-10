using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Syntax;
using System.Collections.Generic;

namespace Atrufulgium.BulletScript.Compiler.Visitors {

    /// <summary>
    /// An interface to unify the read-only <see cref="AbstractTreeWalker"/>
    /// and the read-write <see cref="AbstractTreeRewriter"/>.
    /// </summary>
    internal interface IVisitor {
        /// <summary>
        /// Visit the tree from a node down. This updates <see cref="VisitResult"/>
        /// to contain what <paramref name="node"/> looks like after the
        /// rewrites, if any.
        /// </summary>
        public void Visit(Node node);

        /// <summary>
        /// Whether this visitor only reads, or does both reading and writing
        /// operations.
        /// </summary>
        public bool IsReadOnly { get; }

        /// <summary>
        /// What <see cref="Visit(Node)"/>'s node looks like after execution.
        /// This is <c>null</c> before any walks.
        /// </summary>
        public Node? VisitResult { get; }

        /// <summary>
        /// All diagnostics this <see cref="Visit(Node)"/> introduced.
        /// </summary>
        public IReadOnlyCollection<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Add a diagnostic to <see cref="Diagnostics"/>.
        /// </summary>
        public void AddDiagnostic(Diagnostic diagnostic);

        /// <summary>
        /// Sets the semantic model the tree has access to.
        /// </summary>
        public SemanticModel Model { set; }
    }
}
