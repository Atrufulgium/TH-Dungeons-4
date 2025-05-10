using System;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// This interface represents syntax nodes that are <b>invalid</b> in the
    /// final resulting tree. However, they are "syntax sugar" when doing
    /// rewrites with <see cref="Visitors.AbstractTreeRewriter"/> and may only
    /// be returned and only in certain contexts.
    /// <br/>
    /// Transient nodes may have <see cref="Node.ValidateTree(System.Collections.Generic.IEnumerable{Node})"/>
    /// a simple <see cref="PersistentTransientException"/>.
    /// </summary>
    internal interface ITransientNode {  }

    /// <summary>
    /// An exception that signifies that a transient node was not transient,
    /// and remained in the tree longer than this is allowed.
    /// </summary>
    internal class PersistentTransientException : Exception {
        /// <inheritdoc cref="PersistentTransientException"/>
        public PersistentTransientException(ITransientNode node)
            : base($"Transient nodes (such as {node.GetType()}) may not remain in the tree after rewriting or be visited during rewriting.") { }
    }
}
