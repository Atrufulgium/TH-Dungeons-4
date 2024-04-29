using System.Text;

namespace Atrufulgium.BulletScript.Compiler.Parsing {
    /// <summary>
    /// A class for converting code into more meaningful <see cref="Token"/>s.
    /// This is done via <see cref="ToTokens(string)"/>.
    /// </summary>
    internal class Lexer {
        /// <summary>
        /// <para>
        /// Tokens that are so easy to parse that they can be included once
        /// the left-hand side is found, without any post-processing.
        /// </para>
        /// <para>
        /// In particular <i>not</i> (or partially) included in this list, are
        /// <see cref="TokenKind.MatrixKeyword"/>, <see cref="TokenKind.Number"/>,
        /// <see cref="TokenKind.String"/>, and <see cref="TokenKind.Identifier"/>,
        /// as those require more data than just the literal token as encountered.
        /// </para>
        /// </summary>
        static readonly Dictionary<string, TokenKind> easyParses = new() {
            { ";", TokenKind.Semicolon },
            { ":", TokenKind.Colon },
            { "{", TokenKind.BlockStart },
            { "}", TokenKind.BlockEnd },
            { "[", TokenKind.BracketStart },
            { "]", TokenKind.BracketEnd },
            { "(", TokenKind.ParensStart },
            { ",", TokenKind.Comma },
            { ")", TokenKind.ParensEnd },
            { "float", TokenKind.FloatKeyword },
            { "matrix", TokenKind.MatrixKeyword },
            { "string", TokenKind.StringKeyword },
            { "if", TokenKind.IfKeyword },
            { "else", TokenKind.ElseKeyword },
            { "while", TokenKind.WhileKeyword },
            { "for", TokenKind.ForKeyword },
            { "repeat", TokenKind.RepeatKeyword },
            { "return", TokenKind.ReturnKeyword },
            { "break", TokenKind.BreakKeyword },
            { "continue", TokenKind.ContinueKeyword },
            { "function", TokenKind.FunctionKeyword },
            { "void", TokenKind.VoidKeyword },
            { "true", TokenKind.Number},
            { "false", TokenKind.Number },
            { "!", TokenKind.ExclamationMark },
            { "^", TokenKind.Power },
            { "*", TokenKind.Mul },
            { "/", TokenKind.Div },
            { "%", TokenKind.Mod },
            { "+", TokenKind.Plus },
            { "-", TokenKind.Minus },
            { "<", TokenKind.LessThan },
            { ">", TokenKind.MoreThan },
            { "=", TokenKind.Equals },
            { "&", TokenKind.And },
            { "|", TokenKind.Or },
        };

        /// <summary>
        /// Converts <paramref name="code"/> into a list of tokens.
        /// Any information encountered is to be put in the Diagnostics list.
        /// </summary>
        public (List<Token>, List<Diagnostic>) ToTokens(string code) {
            StringReader reader = new(code);
            List<Token> tokens = new();
            List<Diagnostic> diagnostics = new();
            int lineNumber = 0;

            while (true) {
                // For each line, go through all characters.
                // If characters match with `easyParses`, add that token.
                // Otherwise, do some analysis to determine what token we are.
                lineNumber++;
                string? line = reader.ReadLine();
                if (line == null)
                    break;

                int charNumber = 0;
                while (true) {
                    if (charNumber >= line.Length)
                        break;

                    // Trim initial whitespace as otherwise the col nr is incorrect.
                    while (charNumber < line.Length && char.IsWhiteSpace(line[charNumber]))
                        charNumber++;

                    var (token, advance) = GrabToken(line.AsSpan(start: charNumber));
                    var location = new Location(lineNumber, charNumber + 1);

                    if (easyParses.TryGetValue(token, out TokenKind tokenKind)) {
                        tokens.Add(
                            new(
                                tokenKind,
                                token,
                                location
                            )
                        );
                    } else {
                        // We're, in order of specificity, either:
                        // - matrix#x# for a MatrixKeyword
                        // - somevar for an Identifier
                        // - "... for a String
                        // - 0123 for a Number
                        if (token.Length == 9 && token.StartsWith("matrix") && token[7] == 'x') {
                            if (token[6] >= '1' && token[6] <= '4' && token[8] >= '1' && token[8] <= '4') {
                                tokens.Add(new(TokenKind.MatrixKeyword, token, location));
                            } else {
                                tokens.Add(new(TokenKind.Identifier, token, location));
                            }
                        } else if (char.IsLetter(token[0])) {
                            tokens.Add(new(TokenKind.Identifier, token, location));
                        } else if (token[0] == '"') {
                            if (token[^1] != '"') {
                                diagnostics.Add(
                                    DiagnosticRules.UnterminatedString(location, token)
                                );
                            }
                            tokens.Add(new(TokenKind.String, token, location));
                        } else if (char.IsDigit(token[0])) {
                            tokens.Add(new(TokenKind.Number, token, location));
                        } else {
                            diagnostics.Add(
                                DiagnosticRules.UnknownToken(location, token)
                            );
                        }
                    }
                    charNumber += advance;
                }
            }

            return (tokens, diagnostics);
        }

        readonly StringBuilder sb = new();
        /// <summary>
        /// Grabs either:
        /// <list type="bullet">
        /// <item>The single non-alphanumeric character we start with;</item>
        /// <item>A full alphanumeric+. range if we start with a letter;</item>
        /// <item>A number if we start with a number, taking into account e+ and f notation.</item>
        /// <item>A "-delimited string, possibly excluding the last " if malformed.</item>
        /// </list>
        /// </summary>
        (string token, int advance) GrabToken(ReadOnlySpan<char> remainingLine) {
            int i;
            sb.Clear();
            // "A full alphanmueric+_ range if we start with a letter"
            if (char.IsLetter(remainingLine[0])) {
                for (i = 0; i < remainingLine.Length; i++) {
                    char c = remainingLine[i];
                    if (!char.IsLetter(c) && !char.IsNumber(c) && c != '_')
                        break;
                    sb.Append(c);
                }
                return (sb.ToString(), i);
            }

            // "A number if we start with a number, .."
            if (char.IsNumber(remainingLine[0])) {
                for (i = 0; i < remainingLine.Length; i++) {
                    char c = remainingLine[i];
                    if (char.IsLetter(c) || char.IsNumber(c) || c == '.') {
                        sb.Append(c);
                        continue;
                    }
                    if (c == '+' || c == '-') {
                        char lastChar = sb[^1];
                        if (lastChar == 'e' || lastChar == 'E') {
                            sb.Append(c);
                            continue;
                        }
                    }
                    break;
                }
                return (sb.ToString(), i);
            }

            // "A "-delimited string"
            if (remainingLine[0] == '"') {
                sb.Append('"');
                for (i = 1; i < remainingLine.Length; i++) {
                    char c = remainingLine[i];
                    sb.Append(c);
                    if (c == '"')
                        break;
                }
                return (sb.ToString(), i + 1);
            }

            // "The single non-alphanumeric character we start with"
            return (remainingLine[0].ToString(), 1);
        }
    }
}
