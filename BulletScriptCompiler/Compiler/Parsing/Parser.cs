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

        public (Root?, List<Diagnostic>) ToTree(List<Token> tokens) {
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
            var loc = tokens[0].Location;
            if (declarationForm)
                return (ParseDeclarations(tokens.GetView(0), loc), diagnostics);
            return (ParseTopLevelStatements(tokens.GetView(0), loc), diagnostics);
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

        Root ParseTopLevelStatements(ListView<Token> tokens, Location location) {
            // Unfortunately this bunch of statements and the block
            // statements cannot be combined into one with their differing
            // end conditions.
            List<Statement> statements = new();
            while (tokens.Count > 0) {
                try {
                    statements.Add(ParseStatement(ref tokens, tokens[0].Location));
                }
                catch (MalformedCodeException) {
                    // Guess the next statement by the heuristic "after a semicolon".
                    int i = tokens.FirstIndexWhere(t => t.Kind == TokenKind.Semicolon);
                    tokens = tokens[(i + 1)..];
                }
            }
            return new(statements, location);
        }

        Root ParseDeclarations(ListView<Token> tokens, Location location) {
            List<Declaration> declarations = new();
            while (tokens.Count > 0) {
                try {
                    declarations.Add(ParseDeclaration(ref tokens, tokens[0].Location));
                } catch (MalformedCodeException) {
                    // Guess the next declaration by the "string", "matrix",
                    // "float", or "function" keywords.
                    int i = tokens.FirstIndexWhere(t => t.IsTypeName || t.Kind == TokenKind.FunctionKeyword);
                    tokens = tokens[i..];
                }
            }
            return new(declarations, location);
        }

        Declaration ParseDeclaration(ref ListView<Token> tokens, Location location) {
            if (tokens.Count == 0) EoFPanic();

            if (tokens[0].Kind == TokenKind.FunctionKeyword)
                return ParseMethodDeclaration(ref tokens, location);
            if (tokens[0].IsTypeName)
                return ParseVariableDeclaration(ref tokens, location);
            
            Panic(ExpectedDeclaration(location));
            return null;
        }

        MethodDeclaration ParseMethodDeclaration(ref ListView<Token> tokens, Location location) {
            // yeah preconditions are satisfied *now*, I'm not gonna take
            // chances for refactors
            if (tokens.Count < 3) EoFPanic();
            if (tokens[0].Kind != TokenKind.FunctionKeyword)
                Panic(FunctionRequiresFunction(location));
            if (!tokens[1].IsReturnTypeName)
                Panic(FunctionTypeWrong(tokens[1].Location));
            string type = tokens[1].Value;
            var typeLoc = tokens[1].Location;
            tokens = tokens[2..];
            var methodNameLoc = tokens[0].Location;
            string methodName = ParseMethodName(ref tokens, methodNameLoc);
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.ParensStart)
                Panic(FunctionParensWrong(tokens[0].Location));

            tokens = tokens[1..];
            // We've done `function TYPE NAME(`, now it's time for the good stuff
            List<LocalDeclarationStatement> arguments = new();
            while (true) {
                if (tokens.Count == 0) EoFPanic();
                if (tokens[0].Kind == TokenKind.BlockStart)
                    Panic(FunctionParensWrong(tokens[0].Location));
                if (tokens[0].Kind == TokenKind.ParensEnd)
                    break;
                var loc = tokens[0].Location;
                arguments.Add(new(ParseVariableDeclaration(ref tokens, loc), loc));

                if (tokens.Count == 0) EoFPanic();
                if (tokens[0].Kind == TokenKind.ParensEnd)
                    break;
                if (tokens[0].Kind != TokenKind.Comma)
                    Panic(FunctionArgsCommaSep(tokens[0]));
                tokens = tokens[1..];
            }
            // By the break conditions we are guaranteed a parens end.
            tokens = tokens[1..];
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BlockStart)
                Panic(FunctionHasNoBlock(tokens[0].Location));
            var block = ParseBlock(ref tokens, tokens[0].Location);

            return new MethodDeclaration(
                new(methodName, methodNameLoc),
                new(type, typeLoc),
                arguments,
                block,
                location
            );
        }

        string ParseMethodName(ref ListView<Token> tokens, Location location) {
            if (tokens.Count == 0) EoFPanic();
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

        VariableDeclaration ParseVariableDeclaration(ref ListView<Token> tokens, Location location) {
            if (tokens.Count < 3) EoFPanic();
            if (!tokens[0].IsTypeName) Panic(UnknownType(tokens[0]));
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
            if (tokens.Count == 0) EoFPanic();
            var expr = ParseExpression(ref tokens, tokens[0].Location);
            return new VariableDeclaration(new(name), new(type), location, expr);
        }

        Block ParseBlock(ref ListView<Token> tokens, Location location) {
            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BlockStart)
                Panic(BlockNeedsOpenBrace(location));
            tokens = tokens[1..];

            // Unfortunately this bunch of statements and the top level
            // statements cannot be combined into one with their differing
            // end conditions.
            List<Statement> statements = new();
            while (true) {
                if (tokens.Count == 0) EoFPanic();
                if (tokens[0].Kind == TokenKind.BlockEnd)
                    break;

                try {
                    statements.Add(ParseStatement(ref tokens, tokens[0].Location));
                }
                catch (MalformedCodeException) {
                    // Guess the next statement by the heuristic "after a semicolon".
                    int i = tokens.FirstIndexWhere(t => t.Kind == TokenKind.Semicolon);
                    tokens = tokens[(i + 1)..];
                }
            }

            if (tokens.Count == 0) EoFPanic();
            if (tokens[0].Kind != TokenKind.BlockEnd)
                Panic(BlocksNeedsCloseBrace(tokens[0]));
            tokens = tokens[1..];
            return new(statements, location);
        }

        Statement ParseStatement(ref ListView<Token> tokens, Location location) {
            // temp
            return new ReturnStatement(location);
        }

        Expression ParseExpression(ref ListView<Token> tokens, Location location) {
            // temp
            return new LiteralExpression("", location);
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
