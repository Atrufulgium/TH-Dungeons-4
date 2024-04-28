using Atrufulgium.BulletScript.Compiler.Parsing;
using CompilerTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Tests {

    [TestClass]
    public class LexerTests {
        static void TestLexer(string code, List<TokenKind> expectedTokens)
            => TestHelpers.AssertCollectionsEqual(
                expectedTokens,
                new Lexer().ToTokens(code).Item1.Select(t => t.Kind).ToList()
            );

        static void TestLexerDiagnostics(string code, List<string> expectedIDs)
            => TestHelpers.AssertCollectionsEqual(
                expectedIDs,
                new Lexer().ToTokens(code).Item2.Select(d => d.ID).ToList()
            );

        [TestMethod]
        public void LexerTest1() => TestLexer(
@"",
new List<TokenKind>() { }
        );

        [TestMethod]
        public void LexerTest2() => TestLexer(
@"function void empty(float arg) {}",
new List<TokenKind>() {
    TokenKind.FunctionKeyword,
    TokenKind.VoidKeyword,
    TokenKind.Identifier,
    TokenKind.ParensStart,
    TokenKind.FloatKeyword,
    TokenKind.Identifier,
    TokenKind.ParensEnd,
    TokenKind.BlockStart,
    TokenKind.BlockEnd
}
        );

        [TestMethod]
        public void LexerTest3() => TestLexer(
@"m mat matrix matrix1x1 matrix4x4 matrix5x5 matrix1x5 matrix5x1",
new List<TokenKind>() {
    TokenKind.Identifier,
    TokenKind.Identifier,
    TokenKind.MatrixKeyword,
    TokenKind.MatrixKeyword,
    TokenKind.MatrixKeyword,
    TokenKind.Identifier,
    TokenKind.Identifier,
    TokenKind.Identifier
}
        );

        [TestMethod]
        public void LexerTest4() => TestLexer(
@"1 1.0 1.0e1.0 1.0e+9 1.0e-9 3f f10e+10",
new List<TokenKind>() {
    TokenKind.Number,
    TokenKind.Number,
    TokenKind.Number,
    TokenKind.Number,
    TokenKind.Number,
    TokenKind.Number,
    TokenKind.Identifier,
    TokenKind.Plus,
    TokenKind.Number
}
        );

        [TestMethod]
        public void LexerTest5() => TestLexer(
@"string ""a"" a ""a 3 a"" 3",
new List<TokenKind>() {
    TokenKind.StringKeyword,
    TokenKind.String,
    TokenKind.Identifier,
    TokenKind.String,
    TokenKind.Number
}
        );

        [TestMethod]
        public void LexerTest6() => TestLexer(
@"; {} { } [] [ ] (), ( ) , float matrix string 230 ""string"" identifier if else while for
repeat break continue function void ! ^ * / % + - < > = & | return",
new List<TokenKind>() {
    TokenKind.Semicolon,
    TokenKind.BlockStart,
    TokenKind.BlockEnd,
    TokenKind.BlockStart,
    TokenKind.BlockEnd,
    TokenKind.BracketStart,
    TokenKind.BracketEnd,
    TokenKind.BracketStart,
    TokenKind.BracketEnd,
    TokenKind.ParensStart,
    TokenKind.ParensEnd,
    TokenKind.Comma,
    TokenKind.ParensStart,
    TokenKind.ParensEnd,
    TokenKind.Comma,
    TokenKind.FloatKeyword,
    TokenKind.MatrixKeyword,
    TokenKind.StringKeyword,
    TokenKind.Number,
    TokenKind.String,
    TokenKind.Identifier,
    TokenKind.IfKeyword,
    TokenKind.ElseKeyword,
    TokenKind.WhileKeyword,
    TokenKind.ForKeyword,
    TokenKind.RepeatKeyword,
    TokenKind.BreakKeyword,
    TokenKind.ContinueKeyword,
    TokenKind.FunctionKeyword,
    TokenKind.VoidKeyword,
    TokenKind.ExclamationMark,
    TokenKind.Power,
    TokenKind.Mul,
    TokenKind.Div,
    TokenKind.Mod,
    TokenKind.Plus,
    TokenKind.Minus,
    TokenKind.LessThan,
    TokenKind.MoreThan,
    TokenKind.Equals,
    TokenKind.And,
    TokenKind.Or,
    TokenKind.ReturnKeyword
}
        );

        [TestMethod]
        public void LexerTest7() => TestLexerDiagnostics("\"unmatched :(", new() { "BS0001" });

        [TestMethod]
        public void LexerTest8() => TestLexerDiagnostics("function void _() {}", new() { "BS0002" });

        [TestMethod]
        public void LexerTest9() => TestLexerDiagnostics("_ \"unmatched :(", new() { "BS0002", "BS0001" });

        // ...i just forgot about the existence of _
        [TestMethod]
        public void LexerTest10() => TestLexer(
@"a b a_b",
new List<TokenKind>() {
    TokenKind.Identifier,
    TokenKind.Identifier,
    TokenKind.Identifier
}
        );
        
        // ...empty lines gave an out of range, whoops
        [TestMethod]
        public void LexerTest11() => TestLexer(
@"


",
new List<TokenKind>() { }
        );
    }
}