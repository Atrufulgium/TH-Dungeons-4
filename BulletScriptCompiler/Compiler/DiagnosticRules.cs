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

        public static Diagnostic FunctionParensWrong(Location location)
            => Error(location, "BS0009", "This function declaration has malformed parentheses.");

        public static Diagnostic FunctionParamsWrong(Location location)
            => Error(location, "BS0010", "Expected either a function parameter or a closing brace, but found neither.");

        public static Diagnostic FunctionArgsCommaSep(Token location)
            => Error(location, "BS0011", "Function arguments should be separated with a comma.");

        public static Diagnostic FunctionHasNoBlock(Location location)
            => Error(location, "BS0012", "Function does not have a block body `{ .. }`. All functions must have one.");

        public static Diagnostic BlockNeedsOpenBrace(Location location)
            => Error(location, "BS0013", "Expected a block `{ .. }`, but did not encounter opening `{`.");

        public static Diagnostic UnexpectedEoF(Location location)
            => Error(new Location(location.line + 1, 1), "BS0014", "Unexpected end of file.");

        public static Diagnostic BlocksNeedsCloseBrace(Token location)
            => Error(location, "BS0015", "Expected block `{ .. }` to close with `}`, but did not.");

        public static Diagnostic UnknownType(Token location)
            => Error(location, "BS0016", "Expected a valid variable type (`float`, `matrix`, or `string`).");

        public static Diagnostic VariableNameWrong(Token location)
            => Error(location, "BS0017", "Expected a valid identifier name for the variable. NOte that this may not be a predefined keyword (such as `matrix` or `function`).");
    }
}
