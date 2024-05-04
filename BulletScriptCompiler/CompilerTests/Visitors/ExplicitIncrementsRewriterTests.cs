using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class ExplicitIncrementsRewriterTests {

        [TestMethod]
        public void TestIncrement() => TestHelpers.AssertGeneratesTree(@"
float i = 0;
i++;
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                i
        type:
            float
        initializer:
            [literal float]
            value:
                0
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
                [identifier name]
                name:
                    i
            op:
                +
            rhs:
                [literal float]
                value:
                    1
", new ExplicitIncrementsRewriter());

        [TestMethod]
        public void TestDecrement() => TestHelpers.AssertGeneratesTree(@"
float i = 0;
i--;
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                i
        type:
            float
        initializer:
            [literal float]
            value:
                0
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
                [identifier name]
                name:
                    i
            op:
                -
            rhs:
                [literal float]
                value:
                    1
", new ExplicitIncrementsRewriter());

    }
}