using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// The base node all AST nodes derive from.
    /// </summary>
    internal abstract class Node {
        public Location Location { get; private set; }

        public Node(Location location) {
            Location = location;
        }

        private const int indent = 4;
        protected static string Indent(string? str) {
            string indentStr = new(' ', indent);

            if (str == null || str == "")
                return indentStr + "[none]";

            return indentStr + Regex.Replace(str, @"\r\n?|\n", Environment.NewLine)
                .Replace(Environment.NewLine, Environment.NewLine + indentStr);
        }
        protected static string Indent(Node? node)
            => Indent(node?.ToString());
        protected static string Indent<T>(IReadOnlyCollection<T>? nodes) where T : Node
            => nodes == null ? Indent((string?)null) : Indent(string.Join('\n', nodes));

        protected static string CompactIndent(string? str) => Indent(str);
        protected static string CompactIndent(Node? node) => CompactIndent(node?.ToCompactString());
        protected static string CompactIndent<T>(IReadOnlyCollection<T>? nodes) where T : Node
            => nodes == null ? CompactIndent((string?)null) : CompactIndent(string.Join('\n', nodes.Select(n => n.ToCompactString())));

        /// <summary>
        /// Check whether there are any obvious syntactic problems with the tree,
        /// with a given path of parent nodes. This must include "obvious"
        /// things, as well as things already checked by the <see cref="Parsing.Parser"/>.
        /// <br/>
        /// Be sure to call the base method.
        /// </summary>
        /// <param name="path">
        /// The path used to reach this node.
        /// </param>
        public virtual IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            if (this is ITransientNode)
                throw new InvalidOperationException("Transient nodes may not be part of the final tree.");
            return Array.Empty<Diagnostic>();
        }
        /// <summary>
        /// Checks whether there are any obvious syntactic problems with the tree.
        /// </summary>
        public IEnumerable<Diagnostic> ValidateTree()
            => ValidateTree(new List<Node>() { this } );

        /// <summary>
        /// A more compact representation of the syntax tree in which only
        /// statements take up newlines.
        /// <br/>
        /// Non-emittable statements should use [] for their type, and emittable
        /// should use &lt;&gt; for their type.
        /// </summary>
        /// <returns></returns>
        public abstract string ToCompactString();
    }
}
