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

            eofLocation = tokens[tokens.Count - 1].Location;
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
                    tokens = tokens[1..];
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
                    tokens = tokens[1..];
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
            if (tokens[0].IsTypeName) {
                var decl = ParseVariableDeclaration(ref tokens);
                ParseSemicolon(ref tokens);
                return decl;
            }
            
            Panic(ExpectedDeclaration(location));
            throw new UnreachablePathException();
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

                ParseComma(ref tokens);
                // With the current format, (type1 arg1, type2 arg2,) with the
                // extra comma is valid. Don't allow that.
                if (tokens.Count > 0 && tokens[0].Kind == TokenKind.ParensEnd)
                    Panic(UnknownType(tokens[0]));
            }

            // By the break conditions we are guaranteed a parens end here.
            ParseParensClose(ref tokens);

            var block = ParseBlock(ref tokens);

            return new MethodDeclaration(
                new(methodName, methodNameLoc),
                Syntax.Type.FromString(type.Value),
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

        // (Parses WITHOUT any `;`)
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
                if (tokens[2].IsOp)
                    Panic(VarDeclNoOps(tokens[2]));
                tokens = tokens[2..];
                return new VariableDeclaration(new(name), Syntax.Type.FromString(type.Value), location);
            }
            
            tokens = tokens[3..];
            var expr = ParseExpression(ref tokens);
            return new VariableDeclaration(new(name), Syntax.Type.FromString(type.Value), location, expr);
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
                    tokens = tokens[1..];
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
            // always start with an Identifier, as the only two valid ones are
            // AssignmentExpression and InvocationExpression.
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
            // Three options: a `;` for an empty init, a declaration statement,
            // or an expression.
            Statement? init = null;
            if (tokens[0].Kind != TokenKind.Semicolon) {
                if (tokens[0].IsTypeName) {
                    var decl = ParseVariableDeclaration(ref tokens);
                    init = new LocalDeclarationStatement(decl, decl.Location);
                } else {
                    var expr = ParseExpression(ref tokens);
                    init = new ExpressionStatement(expr, expr.Location);
                }
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

            if (!tokens[0].IsTypeName)
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
            tokens = tokens[1..];

            if (tokens[0].Kind == TokenKind.Semicolon) {
                ParseSemicolon(ref tokens);
                return new ReturnStatement(location);
            }

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
                Panic(ExpectedOpeningBrace(tokens[0]));

            var body = ParseBlock(ref tokens);

            return new WhileStatement(condition, body, location);

        }

        Expression ParseExpression(ref ListView<Token> tokens) {
            var lhs = ParseTerm(ref tokens);
            return ParseRemainingExpression(ref tokens, 0, lhs);
        }

        /// <summary>
        /// Parses a single term in a complex expression.
        /// </summary>
        Expression ParseTerm(ref ListView<Token> tokens) {
            // A single term is defined to be one of the following,
            // in order of parsing:
            // - A parenthesised expression
            // - Invocations `identifier(..`
            // - Postfix `identifier++` or `identifier--`
            // - Accesses `identifier[..`
            // - Regular identifiers `identifier`
            // - Literals `number` or `string`
            // - Matrix-likes `[ .. ]` including the polar variant
            // - Prefix `-` and `!` operators
            if (tokens.Count < 2) EoFPanic();

            if (tokens[0].Kind == TokenKind.ParensStart) {
                // Usually parens are a full single term and we can return.
                // However, indexing is allowed on parenthesised expressions,
                // in which case this is not yet a full term.
                var expression = ParseParenthesisedExpression(ref tokens);
                if (tokens.Count == 0 || tokens[0].Kind != TokenKind.BracketStart)
                    return expression;
                // We're a ( .. )[ .. ] construction.
                var index = ParseMatrixLike(ref tokens);
                if (index is MatrixExpression mat)
                    return new IndexExpression(expression, mat, expression.Location);
                Panic(PolarIsntAnIndex(index.Location));
            }

            if (tokens[0].Kind == TokenKind.Identifier) {
                if (tokens[1].Kind == TokenKind.ParensStart
                    || (tokens.Count >= 5 && tokens[1].Kind == TokenKind.LessThan
                    && tokens[3].Kind == TokenKind.MoreThan && tokens[4].Kind == TokenKind.ParensStart))
                    return ParseInvocation(ref tokens);

                var id = new IdentifierName(tokens[0]);
                tokens = tokens[1..];

                if (tokens.Count >= 2 && tokens[0].Kind == tokens[1].Kind
                    && tokens[0].Kind is TokenKind.Plus or TokenKind.Minus) {
                    PostfixUnaryOp op;
                    if (tokens[0].Kind == TokenKind.Plus)
                        op = PostfixUnaryOp.FromString("++");
                    else
                        op = PostfixUnaryOp.FromString("--");
                    var pf = new PostfixUnaryExpression(
                        id,
                        op,
                        id.Location
                    );
                    tokens = tokens[2..];
                    return pf;
                }

                if (tokens[0].Kind == TokenKind.BracketStart) {
                    var matlike = ParseMatrixLike(ref tokens);
                    if (matlike is MatrixExpression mat)
                        return new IndexExpression(id, mat, id.Location);
                    Panic(PolarIsntAnIndex(matlike.Location));
                }

                return id;
            }

            if (tokens[0].Kind is TokenKind.Number or TokenKind.String)
                return ParseLiteralExpression(ref tokens);

            if (tokens[0].Kind is TokenKind.BracketStart)
                return ParseMatrixLike(ref tokens);

            if (tokens[0].Kind is TokenKind.Minus or TokenKind.ExclamationMark)
                return ParsePrefixUnary(ref tokens);

            Panic(UnexpectedTerm(tokens[0]));
            throw new UnreachablePathException();
        }

        LiteralExpression ParseLiteralExpression(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();

            if (tokens[0].Kind == TokenKind.String) {
                var lit = new LiteralExpression(tokens[0].Value, tokens[0].Location);
                tokens = tokens[1..];
                return lit;
            }

            if (tokens[0].Kind == TokenKind.Number) {
                var lit = new LiteralExpression(ParseNumber(tokens[0]), tokens[0].Location);
                tokens = tokens[1..];
                return lit;
            }

            Panic(UnsupportedLiteral(tokens[0]));
            throw new UnreachablePathException();
        }

        /// <summary>
        /// After a term <paramref name="lhs"/> has been parsed, this parses
        /// the remaining expression.
        /// </summary>
        Expression ParseRemainingExpression(ref ListView<Token> tokens, int prevPrecedence, Expression lhs) {
            // New things to handle:
            // - Assignment `[∘]=`
            // - Binop `∘`
            // Depending on the precedence of the operation so far, the
            // precedence of the next op, and the precedence of the RHS, things
            // bind differently, which gives some annoying code.
            while (true) {
                var (op, precedence, isAssignment, consumedTokens) = ParseOp(tokens);

                if (precedence == -999)
                    return lhs;
                // Nothing to bind to on the right in this case.
                if (precedence < prevPrecedence)
                    return lhs;
                // (Don't consume the token when not binding. Leave that for
                //  its own iteration.)
                tokens = tokens[consumedTokens..];

                var rhs = ParseTerm(ref tokens);
                int nextPrecedence = ParseOp(tokens).precedence;
                // We have to bind to the right *first* in this case.
                // Equality also for the = case that works strictly right to left.
                if (precedence <= nextPrecedence)
                    rhs = ParseRemainingExpression(ref tokens, nextPrecedence, rhs);
                
                if (isAssignment) {
                    if (lhs is not IdentifierName id) {
                        Panic(AssignLHSMustBeIdentifier(lhs.Location));
                        throw new UnreachablePathException();
                    }
                    lhs = new AssignmentExpression(id, AssignmentOp.FromString(op), rhs, lhs.Location);
                } else {
                    lhs = new BinaryExpression(lhs, BinaryOp.FromString(op), rhs, lhs.Location);
                }
            }
        }

        static readonly Dictionary<string, (string op, int precedence, bool isAssignment, int consumedTokens)> ops = new() {
            {  "^", ( "^", 14, false, 1) }, { "^=", ( "^", 0, true, 2) },
            {  "*", ( "*", 12, false, 1) }, { "*=", ( "*", 0, true, 2) },
            {  "/", ( "/", 12, false, 1) }, { "/=", ( "/", 0, true, 2) },
            {  "%", ( "%", 12, false, 1) }, { "%=", ( "%", 0, true, 2) },
            {  "+", ( "+", 10, false, 1) }, { "+=", ( "+", 0, true, 2) },
            {  "-", ( "-", 10, false, 1) }, { "-=", ( "-", 0, true, 2) },
            {  "<", ( "<",  8, false, 1) },
            {  ">", ( ">",  8, false, 1) },
            { "<=", ("<=",  8, false, 2) },
            { ">=", (">=",  8, false, 2) },
            { "==", ("==",  6, false, 2) },
            { "!=", ("!=",  6, false, 2) },
            {  "&", ( "&",  4, false, 1) }, { "&=", ( "&", 0, true, 2) },
            {  "|", ( "|",  2, false, 1) }, { "|=", ( "|", 0, true, 2) },
            {  "=", ( "=",  0, true, 1) }
        };

        /// <summary>
        /// Parses the next operator or assignment, and puts relevant data in
        /// the return. This does not throw an error when it is not an op/ass,
        /// but insteads returns ("", -999, false).
        /// </summary>
        static (string op, int precedence, bool isAssignment, int consumedTokens) ParseOp(ListView<Token> tokens) {
            if (tokens.Count >= 2) {
                if (ops.TryGetValue(tokens[0].Value + tokens[1].Value, out var res)) {
                    return res;
                }
            }
            if (tokens.Count >= 1) {
                if (ops.TryGetValue(tokens[0].Value, out var res)) {
                    return res;
                }
            }
            return ("", -999, false, 0);
        }

        Expression ParseParenthesisedExpression(ref ListView<Token> tokens) {
            ParseParensOpen(ref tokens);
            var expr = ParseExpression(ref tokens);
            ParseParensClose(ref tokens);
            return expr;
        }

        InvocationExpression ParseInvocation(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var nameLoc = tokens[0].Location;
            var name = ParseMethodName(ref tokens);
            ParseParensOpen(ref tokens);
            List<Expression> args = new();
            while (tokens.Count > 0) {
                if (tokens[0].Kind == TokenKind.ParensEnd)
                    break;

                args.Add(ParseExpression(ref tokens));

                if (tokens[0].Kind == TokenKind.ParensEnd)
                    break;

                // Fine I'll handle missing semicolons separately.
                if (tokens.Count > 0 && tokens[0].Kind == TokenKind.Semicolon)
                    Panic(ForgotClosingParens(tokens[0]));

                ParseComma(ref tokens);
                // With the current format, (type1 arg1, type2 arg2,) with the
                // extra comma is valid. Don't allow that.
                if (tokens.Count > 0 && tokens[0].Kind == TokenKind.ParensEnd)
                    Panic(UnknownType(tokens[0]));
            }
            ParseParensClose(ref tokens);

            return new(new(name, nameLoc), args, nameLoc);
        }

        /// <summary>
        /// Parses either a matrix or a polar coord 2-vector, returning either
        /// a <see cref="MatrixExpression"/> or <see cref="PolarExpression"/>.
        /// </summary>
        Expression ParseMatrixLike(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            var matLoc = tokens[0].Location;

            ParseBracketOpen(ref tokens);
            List<List<Expression>> entries = new();
            bool isPolar = false;
            while(tokens.Count > 0) {
                var rowLoc = tokens[0].Location;
                // Row
                if (tokens[0].Kind == TokenKind.BracketEnd)
                    break;
                // Stupid edge-case: the rest of the code succesfully parses
                // a polar matrix of the form [:angle radius].
                if (tokens[0].Kind == TokenKind.Colon)
                    Panic(PolarFormatWrong(tokens[0].Location));

                List<Expression> row = new();
                while (tokens.Count > 0) {
                    // Single entry in row
                    if (tokens[0].Kind is TokenKind.Semicolon or TokenKind.BracketEnd)
                        break;
                    if (tokens[0].Kind == TokenKind.Colon) {
                        isPolar = true;
                        tokens = tokens[1..];
                    }
                    var expr = ParseExpression(ref tokens);
                    row.Add(expr);
                }
                // if we broke because of a ; token, consume it
                // if we broke because of a ; token, leave it to above
                if (tokens[0].Kind == TokenKind.Semicolon) {
                    ParseSemicolon(ref tokens);
                    // hacky edge-case that's not caught otherwise
                    if (tokens.Count > 0 && tokens[0].Kind == TokenKind.BracketEnd)
                        Panic(EmptyMatrix(rowLoc));
                }
                if (row.Count == 0)
                    Panic(EmptyMatrix(rowLoc));
                entries.Add(row);
            }
            ParseBracketClose(ref tokens);

            // Now ensure the matrix is actually proper
            if (entries.Count == 0)
                Panic(EmptyMatrix(matLoc));
            int rowCount = entries.Count;
            int colCount = entries[0].Count;
            if (entries.Any(l => l.Count != colCount))
                Panic(JaggedMatrix(matLoc));
            if (rowCount is < 1 or > 4 || colCount is < 1 or > 4)
                Panic(LargeMatrix(matLoc, rowCount, colCount));
            if (isPolar && (rowCount != 1 || colCount != 2))
                Panic(PolarFormatWrong(matLoc));

            List<Expression> flattenedEntries = entries.SelectMany(l => l).ToList();
            if (isPolar)
                return new PolarExpression(flattenedEntries[0], flattenedEntries[1], matLoc);
            return new MatrixExpression(flattenedEntries, rowCount, colCount, matLoc);
        }

        PrefixUnaryExpression ParsePrefixUnary(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind is not (TokenKind.ExclamationMark or TokenKind.Minus))
                Panic(NotAPrefixUnary(tokens[0]));
            var unary = tokens[0];
            tokens = tokens[1..];
            var expr = ParseTerm(ref tokens);
            return new PrefixUnaryExpression(expr, PrefixUnaryOp.FromString(unary.Value), unary.Location);
        }

        void ParseParensOpen(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.ParensStart)
                Panic(ExpectedOpeningParens(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseParensClose(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.ParensEnd)
                Panic(ForgotClosingParens(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseBlockOpen(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BlockStart)
                Panic(ExpectedOpeningBrace(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseBlockClose(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BlockEnd)
                Panic(ForgotClosingBrace(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseBracketOpen(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BracketStart)
                Panic(ExpectedOpeningBrace(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseBracketClose(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BracketEnd)
                Panic(ForgotClosingBracket(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseSemicolon(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.Semicolon)
                Panic(MissingSemicolon(tokens[0]));
            tokens = tokens[1..];
        }

        void ParseComma(ref ListView<Token> tokens) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.Comma)
                Panic(FunctionArgsCommaSep(tokens[0]));
            tokens = tokens[1..];
        }

        float ParseNumber(Token token) {
            if (token.Value == "true")
                return 1;
            if (token.Value == "false")
                return 0;

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
