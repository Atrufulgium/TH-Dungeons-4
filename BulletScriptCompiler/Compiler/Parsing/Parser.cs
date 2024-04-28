using Atrufulgium.BulletScript.Compiler.Helpers;
using Atrufulgium.BulletScript.Compiler.Syntax;
using System.Diagnostics.CodeAnalysis;
using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Parsing {
    /// <summary>
    /// A class for converting a sequence of <see cref="Token"/>s into a
    /// proper AST, consisting of <see cref="Node"/>s.
    /// This is done via <see cref="ToTree(List{Token})"/>.
    /// </summary>
    internal class Parser {

        readonly List<Diagnostic> diagnostics = new();
        Location eofLocation;

        public (Root?, List<Diagnostic>) ToTree(IReadOnlyList<Token> tokens) {
            diagnostics.Clear();

            if (tokens.Count == 0) {
                diagnostics.Add(EmptyProgram());
                return (new(Array.Empty<Declaration>(), new(0,0)), diagnostics);
            }

            eofLocation = tokens.Last().Location;
            eofLocation = new(eofLocation.line + 1, 1);

            // We're in "top level statements" form if there is no `function` token.
            // We're in "declarations" form is there is.
            bool declarationForm = false;
            foreach (var token in tokens) {
                if (token.Kind == TokenKind.FunctionKeyword) {
                    declarationForm = true;
                    break;
                }
            }
            if (declarationForm)
                return (ParseDeclarations(tokens.GetView(0)), diagnostics);
            return (ParseTopLevelStatements(tokens.GetView(0)), diagnostics);
        }

        // Note: For all these methods with a "ref", the listview is to be
        //       updated to after succesful parsing.
        // Note II: In many cases shit can go wrong when the user types in
        //       something whack. Time for some exception-based control-flow
        //       that tries to recover at the next token that *seems* fine.
        //       :(
        //       (The alternative is stopping compilation at this stage once
        //       a single error has been found, and that's even worse.)

        /// <summary>
        /// Adds a diagnostic to the diagnostics list, and throws a
        /// <see cref="MalformedCodeException"/> to escape a failed parse.
        /// </summary>
        /// <exception cref="MalformedCodeException"> Always thrown. </exception>
        [DoesNotReturn]
        void Panic(Diagnostic diagnostic) {
            diagnostics.Add(diagnostic);
            throw new MalformedCodeException();
        }

        /// <summary>
        /// Adds a diagnostic indicating that we reached the EoF unexpectedly.
        /// </summary>
        /// <exception cref="MalformedCodeException"></exception>
        [DoesNotReturn]
        void EoFPanic() {
            diagnostics.Add(UnexpectedEoF(eofLocation));
            throw new MalformedCodeException();
        }

        Root ParseTopLevelStatements(ListView<Token> tokens) {
            if (tokens.Count == 0)
                return new(Array.Empty<Statement>(), new(1, 1));
            var location = tokens[0].Location;

            // Unfortunately this bunch of statements and the block
            // statements cannot be combined into one with their differing
            // end conditions.
            List<Statement> statements = new();
            while (tokens.Count > 0) {
                try {
                    statements.Add(ParseStatement(ref tokens));
                }
                catch (MalformedCodeException) {
                    // Guess the next statement by the heuristic "after a semicolon".
                    int i = tokens.FirstIndexWhere(t => t.Kind == TokenKind.Semicolon);
                    tokens = tokens[(i + 1)..];
                }
            }
            return new(statements, location);
        }

        Root ParseDeclarations(ListView<Token> tokens) {
            if (tokens.Count == 0)
                return new(Array.Empty<Declaration>(), new(1, 1));
            var location = tokens[0].Location;

            List<Declaration> declarations = new();
            while (tokens.Count > 0) {
                try {
                    declarations.Add(ParseDeclaration(ref tokens));
                } catch (MalformedCodeException) {
                    // Guess the next declaration by the "string", "matrix",
                    // "float", or "function" keywords.
                    int i = tokens.FirstIndexWhere(t => t.IsTypeName || t.Kind == TokenKind.FunctionKeyword);
                    tokens = tokens[i..];
                }
            }
            return new(declarations, location);
        }

        Declaration ParseDeclaration(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].Kind == TokenKind.FunctionKeyword)
                return ParseMethodDeclaration(ref tokens);
            if (tokens[0].IsTypeName)
                return ParseVariableDeclaration(ref tokens);
            
            Panic(ExpectedDeclaration(location));
            return null;
        }

        MethodDeclaration ParseMethodDeclaration(ref ListView<Token> tokens) {
            // yeah preconditions are satisfied *now*, I'm not gonna take
            // chances for refactors
            if (tokens.Count < 3) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].Kind != TokenKind.FunctionKeyword)
                Panic(FunctionRequiresFunction(location));
            if (!tokens[1].IsReturnTypeName)
                Panic(FunctionTypeWrong(tokens[1].Location));
            var type = tokens[1];
            tokens = tokens[2..];

            var methodNameLoc = tokens[0].Location;
            string methodName = ParseMethodName(ref tokens);

            ParseParensOpen(ref tokens);

            // We've done `function TYPE NAME(`, now it's time for the good stuff
            List<LocalDeclarationStatement> arguments = new();
            while (true) {
                if (tokens.Count == 0) EoFPanic();
                if (tokens[0].Kind == TokenKind.BlockStart)
                    Panic(ForgotClosingParens(tokens[0]));
                if (tokens[0].Kind == TokenKind.ParensEnd)
                    break;
                var loc = tokens[0].Location;
                arguments.Add(new(ParseVariableDeclaration(ref tokens), loc));

                if (tokens.Count == 0) EoFPanic();
                if (tokens[0].Kind == TokenKind.ParensEnd)
                    break;
                if (tokens[0].Kind != TokenKind.Comma)
                    Panic(FunctionArgsCommaSep(tokens[0]));
                tokens = tokens[1..];
            }

            // By the break conditions we are guaranteed a parens end here.
            ParseParensClose(ref tokens);

            var block = ParseBlock(ref tokens);

            return new MethodDeclaration(
                new(methodName, methodNameLoc),
                new(type),
                arguments,
                block,
                location
            );
        }

        string ParseMethodName(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].Kind != TokenKind.Identifier)
                Panic(FunctionNameWrong(location));
            // Two branches: a regular "identifier", or an "identifier<number>".
            string baseName = tokens[0].Value;
            if (tokens.Count >= 4 && tokens[1].Kind == TokenKind.LessThan && tokens[3].Kind == TokenKind.MoreThan) {
                // Display with hunderdths to prevent weird floaty shit.
                float value = ParseNumber(tokens[2]);
                tokens = tokens[4..];
                return $"{baseName}<{value:N2}>";
            }
            tokens = tokens[1..];
            return baseName;
        }

        VariableDeclaration ParseVariableDeclaration(ref ListView<Token> tokens) {
            if (tokens.Count < 3) EoFPanic();
            var location = tokens[0].Location;

            if (!tokens[0].IsTypeName)
                Panic(UnknownType(tokens[0]));
            var type = tokens[0];

            if (tokens[1].Kind != TokenKind.Identifier)
                Panic(VariableNameWrong(tokens[1]));
            var name = tokens[1];

            // Two branches: "type name" or "type name = init".
            if (tokens[2].Kind != TokenKind.Equals) {
                tokens = tokens[2..];
                return new VariableDeclaration(new(name), new(type), location);
            }
            
            tokens = tokens[3..];
            var expr = ParseExpression(ref tokens);
            return new VariableDeclaration(new(name), new(type), location, expr);
        }

        Block ParseBlock(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;

            ParseBlockOpen(ref tokens);

            // Unfortunately this bunch of statements and the top level
            // statements cannot be combined into one with their differing
            // end conditions.
            List<Statement> statements = new();
            while (true) {
                if (tokens.Count == 0) EoFPanic();
                if (tokens[0].Kind == TokenKind.BlockEnd)
                    break;

                try {
                    statements.Add(ParseStatement(ref tokens));
                }
                catch (MalformedCodeException) {
                    // Guess the next statement by the heuristic "after a semicolon".
                    int i = tokens.FirstIndexWhere(t => t.Kind == TokenKind.Semicolon);
                    tokens = tokens[(i + 1)..];
                }
            }

            ParseBlockClose(ref tokens);

            return new(statements, location);
        }

        Statement ParseStatement(ref ListView<Token> tokens) {
            // Statement types:
            // - Break (signified by BreakKeyword)
            // - Continue (signified by ContinueKeyword)
            // - ExpressionStatement
            // - IfStatement (signified by IfKeyword)
            // - LocalDeclarationStatement (signified by <float|matrix|string>keyword)
            // - RepeatStatement (signified by RepeatKeyword)
            // - ReturnStatement (signified by ReturnKeyword)
            // - WhileStatement (signified by WhileKeyword)
            // ExpressionStatement is the odd one out, but *currently*, it will
            // always start with an Identifier.
            // Note: All of these statements only parse themselves, and not the
            // following semicolon.
            if (tokens.Count == 0) EoFPanic();

            Statement? statement = tokens[0].Kind switch {
                TokenKind.BreakKeyword => ParseBreak(ref tokens),
                TokenKind.ContinueKeyword => ParseContinue(ref tokens),
                TokenKind.ForKeyword => ParseFor(ref tokens),
                TokenKind.Identifier => ParseExpressionStatement(ref tokens),
                TokenKind.IfKeyword => ParseIf(ref tokens),
                TokenKind.FloatKeyword
                or TokenKind.MatrixKeyword
                or TokenKind.StringKeyword => ParseLocalDeclaration(ref tokens),
                TokenKind.RepeatKeyword => ParseRepeat(ref tokens),
                TokenKind.ReturnKeyword => ParseReturn(ref tokens),
                TokenKind.WhileKeyword => ParseWhile(ref tokens),
                _ => null
            };
            if (statement == null)
                Panic(NotAStatement(tokens[0]));

            return statement;
        }

        // okay some of these checks are just silly but *consistency*
        BreakStatement ParseBreak(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].Kind != TokenKind.BreakKeyword)
                Panic(Silly(location, "break"));
            tokens = tokens[1..];

            ParseSemicolon(ref tokens);

            return new BreakStatement(location);
        }

        ContinueStatement ParseContinue(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].Kind != TokenKind.ContinueKeyword)
                Panic(Silly(location, "continue"));
            tokens = tokens[1..];

            ParseSemicolon(ref tokens);

            return new ContinueStatement(location);
        }

        ExpressionStatement ParseExpressionStatement(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;

            var expr = ParseExpression(ref tokens);
            
            ParseSemicolon(ref tokens);

            return new ExpressionStatement(expr, location);
        }

        ForStatement ParseFor(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].Kind != TokenKind.ForKeyword)
                Panic(Silly(location, "for"));
            tokens = tokens[1..];

            ParseParensOpen(ref tokens);
            if (tokens.Count == 0) EoFPanic();
            // Two options: a semicolon for an empty init, or an init.
            // TODO: More than just declarations lol
            VariableDeclaration? init = null;
            if (tokens[0].Kind != TokenKind.Semicolon) {
                init = ParseVariableDeclaration(ref tokens);
            }
            ParseSemicolon(ref tokens);
            var condition = ParseExpression(ref tokens);
            ParseSemicolon(ref tokens);
            if (tokens.Count == 0) EoFPanic();
            // Again, either the increment is there or not.
            Expression? incr = null;
            if (tokens[0].Kind != TokenKind.ParensEnd) {
                incr = ParseExpression(ref tokens);
            }
            ParseParensClose(ref tokens);
            var body = ParseBlock(ref tokens);

            return new(condition, body, location, init, incr);
        }

        IfStatement ParseIf(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].Kind != TokenKind.IfKeyword)
                Panic(Silly(location, "if"));
            tokens = tokens[1..];

            ParseParensOpen(ref tokens);

            var condition = ParseExpression(ref tokens);

            ParseParensClose(ref tokens);

            var body = ParseBlock(ref tokens);

            if (tokens.Count == 0 || tokens[0].Kind != TokenKind.ElseKeyword)
                return new(condition, body, location);

            // Consume the else
            tokens = tokens[1..];
            if (tokens.Count == 0) EoFPanic();
            // Two options: another if, or a block.
            if (tokens[0].Kind == TokenKind.IfKeyword) {
                var elifLocation = tokens[0].Location;
                var elifBranch = ParseIf(ref tokens);
                return new IfStatement(
                    condition, body, location,
                    new Block(new List<Statement>() { elifBranch }, elifLocation)
                );
            }
            var elseBranch = ParseBlock(ref tokens);
            return new IfStatement(condition, body, location, elseBranch);
        }

        LocalDeclarationStatement ParseLocalDeclaration(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].IsTypeName)
                Panic(Silly(location, "localdecl"));

            var decl = ParseVariableDeclaration(ref tokens);

            ParseSemicolon(ref tokens);

            return new(decl, location);
        }

        RepeatStatement ParseRepeat(ref ListView<Token> tokens) {
            if (tokens.Count < 2) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].Kind != TokenKind.RepeatKeyword)
                Panic(Silly(location, "repeat"));

            tokens = tokens[1..];
            Expression? count = null;
            if (tokens[0].Kind == TokenKind.ParensStart) {
                ParseParensOpen(ref tokens);
                count = ParseExpression(ref tokens);
                ParseParensClose(ref tokens);
            }

            var body = ParseBlock(ref tokens);

            return new RepeatStatement(body, location, count);
        }

        ReturnStatement ParseReturn(ref ListView<Token> tokens) {
            if (tokens.Count < 2) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].Kind != TokenKind.ReturnKeyword)
                Panic(Silly(location, "return"));

            if (tokens[1].Kind == TokenKind.Semicolon)
                return new ReturnStatement(location);

            tokens = tokens[1..];
            var expr = ParseExpression(ref tokens);

            ParseSemicolon(ref tokens);

            return new(location, expr);
        }

        WhileStatement ParseWhile(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;

            if (tokens[0].Kind != TokenKind.WhileKeyword)
                Panic(Silly(location, "while"));

            if (tokens[1].Kind != TokenKind.ParensStart)
                Panic(ExpectedOpeningParens(tokens[1]));

            tokens = tokens[2..];
            if (tokens.Count == 0) EoFPanic();
            var condition = ParseExpression(ref tokens);
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.ParensEnd)
                Panic(ForgotClosingParens(tokens[0]));
            tokens = tokens[1..];

            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BlockStart)
                Panic(ExpectedOpeningBrace(tokens[0].Location));

            var body = ParseBlock(ref tokens);

            return new WhileStatement(condition, body, location);

        }

        Expression ParseExpression(ref ListView<Token> tokens) {
            // temp
            if (tokens.Count == 0) EoFPanic();
            var location = tokens[0].Location;
            return new LiteralExpression("", location);
        }

        void ParseParensOpen(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.ParensStart)
                Panic(MissingSemicolon(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseParensClose(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.ParensEnd)
                Panic(MissingSemicolon(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseBlockOpen(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BlockStart)
                Panic(MissingSemicolon(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseBlockClose(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BlockEnd)
                Panic(MissingSemicolon(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseBracketOpen(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BracketStart)
                Panic(MissingSemicolon(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseBracketClose(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BracketEnd)
                Panic(MissingSemicolon(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseSemicolon(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.Semicolon)
                Panic(MissingSemicolon(tokens[0]));
            tokens = tokens[1..];
        }

        float ParseNumber(Token token) {
            // ParseFloat can't handle the f.
            // F.
            if (token.Value.EndsWith('f'))
                token = new(token.Kind, token.Value[0..^2], token.Location);

            float res = 0;
            if (token.Kind != TokenKind.Number || !float.TryParse(token.Value, out res))
                Panic(NumberUnparsable(token.Location, token.Value));
            return res;
        }
    }
}
