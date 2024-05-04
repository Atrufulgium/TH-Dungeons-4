using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class FlattenInitializationsRewriterTests {

        [TestMethod]
        public void TestWithoutInit() => TestHelpers.AssertGeneratesTree(@"
float i;
", @"
[root]
statements:
    [local declaration noinit]
    identifier:
        [identifier name]
        name:
            i
    type:
        float
", new FlattenInitializationsRewriter());

        [TestMethod]
        public void TestWithInit() => TestHelpers.AssertGeneratesTree(@"
float i = 1 + 2;
", @"
[root]
statements:
    [local declaration noinit]
    identifier:
        [identifier name]
        name:
            i
    type:
        float
    [expression statement]
    statement:
        [assignment]
        lhs:
            [identifier name]
            name:
                i
        op:
            =
        rhs:
            [binop]
            lhs:
                [literal float]
                value:
                    1
            op:
                +
            rhs:
                [literal float]
                value:
                    2
", new FlattenInitializationsRewriter());

    }
}