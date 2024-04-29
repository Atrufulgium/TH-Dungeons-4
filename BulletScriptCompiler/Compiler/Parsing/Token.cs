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
        Comma,
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
        BreakKeyword,
        ContinueKeyword,
        FunctionKeyword,
        VoidKeyword,
        ReturnKeyword,
        Colon,
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
        public readonly TokenKind Kind;
        /// <summary>
        /// The text corresponding to this token in the text.
        /// </summary>
        public readonly string Value;
        /// <summary>
        /// Where in the text this token was found.
        /// </summary>
        public readonly Location Location;
        /// <summary>
        /// The first index in the text beyond this token.
        /// (This may not necessarily exist in the text in cases of e.g.
        ///  newlines.)
        /// </summary>
        public Location EndLocation => new(Location.line, Location.col + Value.Length);

        public Token(TokenKind syntaxToken, string value, Location location) {
            Kind = syntaxToken;
            Value = value;
            Location = location;
        }

        public override string ToString()
            => $"{Kind} ({Value})";

        /// <summary>
        /// Whether this token specifies a proper type.
        /// </summary>
        public bool IsTypeName
            => Kind is TokenKind.FloatKeyword or TokenKind.MatrixKeyword or TokenKind.StringKeyword;

        /// <summary>
        /// Whether this token specifies a proper type, or `void`.
        /// </summary>
        public bool IsReturnTypeName
            => IsTypeName || Kind == TokenKind.VoidKeyword;

        /// <summary>
        /// Whether this token specifies a non-assignment operator: one of
        /// ^ * / % + - &lt; &gt; &amp; |.
        /// </summary>
        public bool IsOp
            => Kind is TokenKind.Power or TokenKind.Mul or TokenKind.Div or TokenKind.Mod
            or TokenKind.Plus or TokenKind.Minus or TokenKind.LessThan or TokenKind.MoreThan
            or TokenKind.And or TokenKind.Or;
    }
}
