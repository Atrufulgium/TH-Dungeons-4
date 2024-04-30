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

            return indentStr + str.ReplaceLineEndings()
                .ReplaceLineEndings()
                .Replace(Environment.NewLine, Environment.NewLine + indentStr);
        }
        protected static string Indent(Node? node)
            => Indent(node?.ToString());
        protected static string Indent<T>(IReadOnlyCollection<T>? nodes) where T : Node
            => nodes == null ? Indent((string?)null) : Indent(string.Join('\n', nodes));

        /// <summary>
        /// Check whether there are any obvious syntactic problems with the tree,
        /// with a given path of parent nodes. This must include "obvious"
        /// things, as well as things already checked by the <see cref="Parsing.Parser"/>.
        /// </summary>
        /// <param name="path">
        /// The path used to reach this node.
        /// </param>
        public abstract IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path);
        /// <summary>
        /// Checks whether there are any obvious syntactic problems with the tree.
        /// </summary>
        public IEnumerable<Diagnostic> ValidateTree()
            => ValidateTree(new List<Node>() { this } );
    }
}
