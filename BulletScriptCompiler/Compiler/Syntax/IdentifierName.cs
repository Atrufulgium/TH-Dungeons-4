using Atrufulgium.BulletScript.Compiler.Parsing;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents a name, such as a type, variable, or function name.
    /// </summary>
    internal class IdentifierName : Expression {
        public string Name { get; private set; }

        public IdentifierName(string name, Location location = default) : base(location) {
            Name = name;
        }

        public IdentifierName(Token token) : base(token.Location) {
            Name = token.Value;
        }

        public override string ToString()
            => $"[identifier name]\nname:\n{Indent(Name)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => new List<Diagnostic>();

        public IdentifierName WithName(string name)
            => new(name, Location);
    }
}
