using Atrufulgium.BulletScript.Compiler.Parsing;
using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler {
    internal static class DiagnosticRules {

        static Diagnostic Error(Location location, string id, string msg)
            => new(location, DiagnosticLevel.Error, id, msg);

        static Diagnostic Error(Token token, string id, string msg)
            => Error(token.Location, id, msg);

        static Diagnostic Error(Node node, string id, string msg)
            => Error(node.Location, id, msg);

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
            => Error(location, "BS0004", "Expected a declaration such as `float number` or `function void method()` designated by a `float`, `function`, `method`, or `string` keyword.");

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

        public static Diagnostic ForgotClosingBrace(Token location)
            => Error(location, "BS0012", "Expected a block `{ .. }` to end with a closing `}`.");

        public static Diagnostic ExpectedOpeningBrace(Token location)
            => Error(location, "BS0013", "Expected a block `{ .. }`, but did not encounter opening `{`. Note that single statements are not a replacement for blocks.");

        public static Diagnostic UnexpectedEoF(Location location)
            => Error(new Location(location.line + 1, 1), "BS0014", "Unexpected end of file. Don't forget to end your blocks with `}` and your statements with `;`.");

        public static Diagnostic UnknownType(Location location)
            => Error(location, "BS0016", "Expected a valid variable type (`float`, `matrix`, or `string`).");
        public static Diagnostic UnknownType(Token location) => UnknownType(location.Location);

        public static Diagnostic VariableNameWrong(Token location)
            => Error(location, "BS0017", "Expected a valid identifier name for the variable. Note that this may not be a predefined keyword (such as `matrix` or `function`).");

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

        public static Diagnostic PolarIsntAnIndex(Location location)
            => Error(location, "BS0023", "You cannot access a matrix with a polar expression [a:b], only with a regular matrix.");

        public static Diagnostic UnexpectedTerm(Location location, string weird)
            => Error(location, "BS0024", $"Unexpected `{weird}`.");
        public static Diagnostic UnexpectedTerm(Token location)
            => UnexpectedTerm(location.Location, location.Value);

        public static Diagnostic AssignLHSMustBeIdentifier(Location location)
            => Error(location, "BS0025", "The left-hand side of an assignment must be an identifier name.");

        public static Diagnostic NotAPrefixUnary(Location location)
            => Error(location, "BS0026", "Expected a unary operator `!` or `-`.");
        public static Diagnostic NotAPrefixUnary(Token location) => NotAPostfixUnary(location.Location);

        public static Diagnostic EmptyMatrix(Location location)
            => Error(location, "BS0027", "Matrix is empty/has an empty row. Matrices must be between 1x1 and 4x4.");

        public static Diagnostic JaggedMatrix(Location location)
            => Error(location, "BS0028", "Matrix is not square.");

        public static Diagnostic LargeMatrix(Location location, int rows, int cols)
            => Error(location, "BS0029", $"Matrix is {rows}x{cols}. Matrices must be between 1x1 and 4x4.");

        public static Diagnostic PolarFormatWrong(Location location)
            => Error(location, "BS0030", "Polar matrices must be of the form `[angle:radius]`.");

        public static Diagnostic UnsupportedLiteral(Token location)
            => Error(location, "BS0031", "Expected a literal string `\"hi\"` or number `230`.");

        public static Diagnostic ExpectedOpeningBracket(Token location)
            => Error(location, "BS0032", "Expected opening brace `[`.");

        public static Diagnostic ForgotClosingBracket(Token location)
            => Error(location, "BS0033", "Expected closing brace `]`.");

        public static Diagnostic VarDeclNoOps(Token location)
            => Error(location, "BS0034", "Variable declarations with initializers must be of the form `type a = val`. There may not be a `+=` etc.");

        public static Diagnostic BreakNotInLoop(Node location)
            => Error(location, "BS0035", "Break statement not inside a `for`, `repeat`, or `while` loop.");

        public static Diagnostic ContinueNotInLoop(Node location)
            => Error(location, "BS0036", "Continue statement not inside a `for`, `repeat`, or `while` loop.");

        public static Diagnostic InvalidForInitializer(Node location)
            => Error(location, "BS0037", "For initializers may only be `type name = val` declarations, `my_method()` calls, or assignments.");

        // "But why are assignments an expression then?"
        // Because I may change my mind later and allow `a = b = c`.
        public static Diagnostic AssignmentOnlyAsStatement(Node location)
            => Error(location, "BS0039", "Assignments may only be a statement, and not further down in arithmetic/calls/etc.");

        public static Diagnostic IndexMatrixWrongSize(Node location)
            => Error(location, "BS0040", "Indexing matrices with `[ .. ]` is only sensible with `[a]`, `[a b]`, or `[a; b]`.");

        public static Diagnostic MethodDeclarationInitializer(Node location)
            => Error(location, "BS0041", "Method arguments may not have initializers. For instance, `function void my_method(float val = 3)` is disallowed.");

        public static Diagnostic InvalidAssignment(Node location)
            => Error(location, "BS0042", "The only valid assignment operators are = + - * / % ^ & |");

        public static Diagnostic InvalidBinop(Node location)
            => Error(location, "BS0043", "The only valid binary operators are + - * / % ^ & | != == >= <= > <");

        public static Diagnostic InvalidExpressionStatement(Node location)
            => Error(location, "BS0044", "Standalone expressions may only be arithmetic `a ∘= ..`, function calls `my_method(..`, or increment/decrementors `a++`/`a--`.");

        public static Diagnostic NotAPostfixUnary(Location location)
            => Error(location, "BS0045", "Expected a unary operator `++` or `--`.");

        public static Diagnostic ParamMatricesNeedSize(Node location)
            => Error(location, "BS0046", "Matrix arguments in methods must have their size specified -- `matrix` as type is forbidden.");

        public static Diagnostic ClashingTypeDef(Node location, Syntax.Type from, Syntax.Type to)
            => Error(location, "BS0047", $"Identifier already exists as type `{from}`, but also gets redefined as incompatible type `{to}`.");

        public static Diagnostic ClashingKindDef(Node location)
            => Error(location, "BS0048", "The same identifier cannot be used for both a function and a variable.");

        public static Diagnostic ClashingAssignment(Node location, Syntax.Type definition, Syntax.Type actual)
            => Error(location, "BS0049", $"The variable assigned to is of type {definition}, but you are trying to assign it an incompatible type {actual}.");

        public static Diagnostic MatricesMustBeFloats(Node location)
            => Error(location, "BS0050", "Matrices may only contain float entries.");

        public static Diagnostic PolarMustBeFloats(Node location)
            => Error(location, "BS0051", "The angle and radius in polar notation must be floats.");

        public static Diagnostic IncrementDecrementMustBeFloat(Node location)
            => Error(location, "BS0052", "The increment and decrement operators `++` and `--` are only valid on floats.");

        public static Diagnostic NegateNotMustBeNumeric(Node location)
            => Error(location, "BS0053", "The unary negate and not operators `-` and `!` are only valid on floats or matrices.");

        public static Diagnostic CanOnlyIndexMatrices(Node location)
            => Error(location, "BS0054", "The index operation [] can only be applied to matrices.");

        public static Diagnostic MatrixMulSizeMismatch(Node location, (int, int) size1, (int, int) size2)
            => Error(location, "BS0055", $"Misformed matrix multiplication sizes {size1} and {size2}. These must either be {size1} and ({size1.Item2}, *) for regular matrix multiplication, or {size1} and {size1} for entrywise multiplication.");

        public static Diagnostic IncompatibleBinop(Node location, Syntax.Type lhs, Syntax.Type rhs, string op)
            => Error(location, "BS0056", $"Cannot compute expression of form `{lhs}{op}{rhs}`. In most cases, the types must be the same. Strings are also very rigid and only allow `=`, `==` and `!=`.");

        public static Diagnostic ReturnMatricesNeedSize(Node location)
            => Error(location, "BS0057", "A function may not have `matrix` as return type, the size must be explicitly `matrixAxB`.");

        public static Diagnostic UnknownTypeAtThisPoint(IdentifierName location)
            => Error(location, "BS0058", $"Using variable `{location.Name}` before its type is well-defined.");

        public static Diagnostic UndefinedMethodOrOverload(Node location)
            => Error(location, "BS0059", "Unknown method or overload called.");

        public static Diagnostic ConditionMustBeFloat(Node location)
            => Error(location, "BS0060", "Condition must evaluate to a float.");

        public static Diagnostic RepeatCountMustBeFloat(Node location)
            => Error(location, "BS0061", "Repeat count must evaluate to a float.");

        public static Diagnostic MismatchingReturnType(Node location, Syntax.Type expected, Syntax.Type actual)
            => Error(location, "BS0062", $"Expected method to return a {expected}, but instead returned a {actual}.");

        public static Diagnostic VoidMayNotReturnExpression(Node location)
            => Error(location, "BS0063", "Void-typed methods may only use `return;`, without passing a value.");

        public static Diagnostic MainMethodWrong(Node location)
            => Error(location, "BS0064", "Main method must have signature `function void main(float)`.");

        public static Diagnostic OnMessageMethodWrong(Node location)
            => Error(location, "BS0065", "OnMessage method must have signature `function void on_message(float)`.");

        public static Diagnostic OnHealthMethodWrong(Node location)
            => Error(location, "BS0066", "OnHealth method must have signature `function void on_health<value>()`.");

        public static Diagnostic OnTimeMethodWrong(Node location)
            => Error(location, "BS0067", "OnTime method must have signature `function void on_time<value>()`.");

        public static Diagnostic OnHealthWithoutArg(Node location)
            => new(location.Location, DiagnosticLevel.Warning, "BS0068", "Found method `on_health`; did you mean `on_health<value>`?");

        public static Diagnostic OnTimeWithoutArg(Node location)
            => new(location.Location, DiagnosticLevel.Warning, "BS0069", "Found method `on_time`; did you mean `on_time<value>`?");

        public static Diagnostic RecursiveCall(Location location, List<MethodSymbol> problemPath)
            => Error(location, "BS0070", $"Recursion is not allowed. Recursion: {string.Join(" -> ", problemPath.Select(m => m.FullyQualifiedName))}.");

        public static Diagnostic IllegalWaitCall(Location location, List<MethodSymbol> problemPath)
            => Error(location, "BS0071", $"The special on_X methods may not call `wait`. Path: {string.Join(" -> ", problemPath.Select(m => m.FullyQualifiedName))}");

        public static Diagnostic InternalMalformedConditionalGoto(Node node)
            => Error(node.Location, "BS1000", "A ConditionalGotoStatement may only have a block with a single goto statement. The condition must be a `identifier`.");
    }
}
