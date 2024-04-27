namespace Atrufulgium.BulletScript.Compiler.Parsing {
    /// <summary>
    /// All tokens that can appear in a valid BulletScript file.
    /// </summary>
    internal enum TokenKind {
        Semicolon,
        BlockStart,
        BlockEnd,
        BracketStart,
        BracketEnd,
        ParensStart,
        ParensEnd,
        FloatKeyword,
        MatrixKeyword,
        StringKeyword,
        Number,
        String,
        Identifier,
        IfKeyword,
        ElseKeyword,
        WhileKeyword,
        ForKeyword,
        RepeatKeyword,
        LoopKeyword,
        BreakKeyword,
        ContinueKeyword,
        FunctionKeyword,
        VoidKeyword,
        ExclamationMark,
        Power,
        Mul,
        Div,
        Mod,
        Plus,
        Minus,
        LessThan,
        MoreThan,
        Equals,
        And,
        Or
    }

    /// <summary>
    /// A full token including its metadata.
    /// </summary>
    internal readonly struct Token {
        /// <summary>
        /// The kind of token this is.
        /// </summary>
        public readonly TokenKind SyntaxToken;
        /// <summary>
        /// The text corresponding to this token in the text.
        /// </summary>
        public readonly string Value;
        /// <summary>
        /// Where in the text this token was found.
        /// </summary>
        public readonly Location Location;

        public Token(TokenKind syntaxToken, string value, Location location) {
            SyntaxToken = syntaxToken;
            Value = value;
            Location = location;
        }

        public override string ToString()
            => $"{SyntaxToken} ({Value})";
    }
}
