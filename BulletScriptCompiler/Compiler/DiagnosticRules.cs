using Atrufulgium.BulletScript.Compiler.Parsing;

namespace Atrufulgium.BulletScript.Compiler {
    internal static class DiagnosticRules {

        static Diagnostic Error(Location location, string id, string msg)
            => new(location, DiagnosticLevel.Error, id, msg);

        static Diagnostic Error(Token token, string id, string msg)
            => Error(token.Location, id, msg);

        public static Diagnostic UnterminatedString(Location location, string token)
            => Error(location, "BS0001",
                $"Encountered unterminated string:\n  {token}\nAll strings must be enclosed with \" on both sides."
            );

        public static Diagnostic UnknownToken(Location location, string token)
            => Error(location, "BS0002",
                $"Encountered unexpected token:\n  {token}\n(If you started a name with a \"_\", that is not allowed.)"
            );

        public static Diagnostic EmptyProgram()
            => new(new(0, 0), DiagnosticLevel.Warning, "BS0003", "You compiled an empty program. This is probably not intended.");

        public static Diagnostic ExpectedDeclaration(Location location)
            => Error(location, "BS0004", "Expected a declaration such as `float number` or `function void method()` designated by a `float`, `function`, `method`, or `string` keyword..");

        public static Diagnostic FunctionRequiresFunction(Location location)
            => Error(location, "BS0005", "A function declaration is of the form `function type name()`, but the `function` keyword is missing.");

        public static Diagnostic FunctionTypeWrong(Location location)
            => Error(location, "BS0006", "Expected a valid return type (`void`, `float`, `matrix`, or `string`) after `function` keyword.");

        public static Diagnostic FunctionNameWrong(Location location)
            => Error(location, "BS0007", "Expected a valid identifier name for the function. Note that this may not be a predefined keyword (such as `matrix` or `function`).");

        public static Diagnostic NumberUnparsable(Location location, string number)
            => Error(location, "BS0008", $"Expected a number, but `{number}` is not a number!");

        public static Diagnostic FunctionArgsCommaSep(Token location)
            => Error(location, "BS0011", "Function arguments should be separated with a comma.");

        public static Diagnostic ExpectedOpeningBrace(Location location)
            => Error(location, "BS0013", "Expected a block `{ .. }`, but did not encounter opening `{`. Note that single statements are not a replacement for blocks.");

        public static Diagnostic UnexpectedEoF(Location location)
            => Error(new Location(location.line + 1, 1), "BS0014", "Unexpected end of file. Don't forget to end your blocks with `}` and your statements with `;`.");

        public static Diagnostic UnknownType(Token location)
            => Error(location, "BS0016", "Expected a valid variable type (`float`, `matrix`, or `string`).");

        public static Diagnostic VariableNameWrong(Token location)
            => Error(location, "BS0017", "Expected a valid identifier name for the variable. NOte that this may not be a predefined keyword (such as `matrix` or `function`).");

        public static Diagnostic MissingSemicolon(Token location)
            => Error(location, "BS0018", "Expected a semicolon `;`.");

        public static Diagnostic NotAStatement(Token location)
            => Error(location, "BS0019", "Expected a statement (such as `break;`, `lhs = rhs`, `call()`, etc.)");

        public static Diagnostic Silly(Location location, string reason)
            => Error(location, "BS0020", $"(Something went wrong internally, and a precondition that should always be met was not met: {reason})");

        public static Diagnostic ForgotClosingParens(Token location)
            => Error(location, "BS0021", "Expected a closing parenthesis `)`.");

        public static Diagnostic ExpectedOpeningParens(Token location)
            => Error(location, "BS0022", "Expected an opening parenthesis `(`.");

    }
}
