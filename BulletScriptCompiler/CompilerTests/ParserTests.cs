using Atrufulgium.BulletScript.Compiler.Helpers;
using Atrufulgium.BulletScript.Compiler.Parsing;
using CompilerTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Tests {

    [TestClass]
    public class ParserTests {

        static void TestCompile(string code, string tree) {
            string actualTree = Compile(code);
            Assert.AreEqual("\n" + tree.Trim() + "\n", "\n" + actualTree.Trim() + "\n");
        }
        static string Compile(string code) => new Parser().ToTree(new Lexer().ToTokens(code).Item1).Item1.ToString();

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