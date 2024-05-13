using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class ExtractReturnRewriterTests {

        [TestMethod]
        public void TestRegular() => TestHelpers.AssertGeneratesTree(@"
function float my_float() { return 3; }
function void A() { float a = my_float(); }
", @"
[root]
declarations:
    [variable declaration]
    identifier:
        [identifier name]
        name:
            my_float()#return
    type:
        float
    initializer:
        [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            my_float
    type:
        void
    arguments:
        [none]
    block:
        [block]
        statements:
            [expression statement]
            statement:
                [assignment]
                lhs:
                    [identifier name]
                    name:
                        my_float()#return
                op:
                    =
                rhs:
                    [literal float]
                    value:
                        3
            [return]
            value:
                [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            A
    type:
        void
    arguments:
        [none]
    block:
        [block]
        statements:
            [expression statement]
            statement:
                [invocation]
                target:
                    [identifier name]
                    name:
                        my_float
                args:
                    [none]
            [local declaration]
            declaration:
                [variable declaration]
                identifier:
                    [identifier name]
                    name:
                        a
                type:
                    float
                initializer:
                    [identifier name]
                    name:
                        my_float()#return
", new ExtractReturnRewriter());

        [TestMethod]
        public void TestArithmetic() => TestHelpers.AssertGeneratesTree(@"
function float my_float() { return 3; }
function void A() { float a = 1 + my_float(); }
", @"
[root]
declarations:
    [variable declaration]
    identifier:
        [identifier name]
        name:
            my_float()#return
    type:
        float
    initializer:
        [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            my_float
    type:
        void
    arguments:
        [none]
    block:
        [block]
        statements:
            [expression statement]
            statement:
                [assignment]
                lhs:
                    [identifier name]
                    name:
                        my_float()#return
                op:
                    =
                rhs:
                    [literal float]
                    value:
                        3
            [return]
            value:
                [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            A
    type:
        void
    arguments:
        [none]
    block:
        [block]
        statements:
            [expression statement]
            statement:
                [invocation]
                target:
                    [identifier name]
                    name:
                        my_float
                args:
                    [none]
            [local declaration]
            declaration:
                [variable declaration]
                identifier:
                    [identifier name]
                    name:
                        a
                type:
                    float
                initializer:
                    [binop]
                    lhs:
                        [literal float]
                        value:
                            1
                    op:
                        +
                    rhs:
                        [identifier name]
                        name:
                            my_float()#return
", new ExtractReturnRewriter());

        [TestMethod]
        public void TestIntrinsic() => TestHelpers.AssertGeneratesTree(@"
function void A() { float a = sin(230); }
", @"
[root]
declarations:
    [method declaration]
    identifier:
        [identifier name]
        name:
            A
    type:
        void
    arguments:
        [none]
    block:
        [block]
        statements:
            [local declaration]
            declaration:
                [variable declaration]
                identifier:
                    [identifier name]
                    name:
                        a
                type:
                    float
                initializer:
                    [invocation]
                    target:
                        [identifier name]
                        name:
                            sin
                    args:
                        [literal float]
                        value:
                            230
", new ExtractReturnRewriter());

        // I did an oopsie and this introduced `Identifier;` statements, which
        // are not allowed.
        [TestMethod]
        public void TestVoidedNonvoid() => TestHelpers.AssertGeneratesTree(@"
function float A() { return 3; }
function void B() { A(); }
", @"
[root]
    <variable declaration>   float A()#return
    [method declaration]     void A()
            [expression]             A()#return = 3
            [return]                 [none]
    [method declaration]     void B()
            [expression]             A()
", compactTree: true, new ExtractReturnRewriter());
    }
}