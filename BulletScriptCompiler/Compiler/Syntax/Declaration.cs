namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents any declaration.
    /// </summary>
    internal abstract class Declaration : Node {
        public IdentifierName Identifier { get; private set; }
        public IdentifierName Type { get; private set; }

        public Declaration(
            IdentifierName identifier,
            IdentifierName type,
            Location location
        ) : base(location) {
            Identifier = identifier;
            Type = type;
        }
    }
}
