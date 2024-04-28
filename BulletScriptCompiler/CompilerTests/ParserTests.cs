using Atrufulgium.BulletScript.Compiler.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Tests {

    [TestClass]
    public class ParserTests {

        static void TestCompile(string code, string tree) {
            string actualTree = Compile(code);
            tree = tree.ReplaceLineEndings().Trim();
            actualTree = actualTree.ReplaceLineEndings().Trim();
            // (newlines for nicer test output)
            Assert.AreEqual("\n" + tree + "\n", "\n" + actualTree + "\n");
        }
        static string Compile(string code) {
            var (tokens, diags) = new Lexer().ToTokens(code);
            AssertNoErrorDiagnostics(diags);
            var (root, diags2) = new Parser().ToTree(tokens);
            AssertNoErrorDiagnostics(diags2);
            if (root == null)
                Assert.Fail("Unexpected null tree, without any diagnostics.");
            return root.ToString();
        }

        static void AssertNoErrorDiagnostics(IEnumerable<Diagnostic> diagnostics) {
            var errorDiags = diagnostics.Where(d => d.DiagnosticLevel == DiagnosticLevel.Error);
            if (!errorDiags.Any())
                return;
            string msg = "";
            foreach (var err in errorDiags) {
                msg += "\n" + err.ToString();
            }
            Assert.Fail(msg);
        }

        [TestMethod]
        public void CompileTest1() => TestCompile(@"
function void main(float value) {}
function void on_health<0.5>() {}
", @"
[root]
declarations:
    [method declaration]
    identifier:
        [identifier name]
        name:
            main
    type:
        [identifier name]
        name:
            void
    arguments:
        [local declaration]
        declaration:
            [variable declaration]
            identifier:
                [identifier name]
                name:
                    value
            type:
                [identifier name]
                name:
                    float
            initializer:
                [none]
    block:
        [block]
        statements:
            [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            on_health<0.50>
    type:
        [identifier name]
        name:
            void
    arguments:
        [none]
    block:
        [block]
        statements:
            [none]
");

    }
}