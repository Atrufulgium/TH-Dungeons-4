using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class ExtractMethodArgsRewriterTests {

        [TestMethod]
        public void TestRegular() => TestHelpers.AssertGeneratesTree(@"
function void A(float a) { a = a; }
function void B(float b) { A(b); }
", @"
[root]
declarations:
    [variable declaration]
    identifier:
        [identifier name]
        name:
            A(float)#a
    type:
        float
    initializer:
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
                [assignment]
                lhs:
                    [identifier name]
                    name:
                        A(float)#a
                op:
                    =
                rhs:
                    [identifier name]
                    name:
                        A(float)#a
    [variable declaration]
    identifier:
        [identifier name]
        name:
            B(float)#b
    type:
        float
    initializer:
        [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            B
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
                        A(float)#a
                op:
                    =
                rhs:
                    [identifier name]
                    name:
                        B(float)#b
            [expression statement]
            statement:
                [invocation]
                target:
                    [identifier name]
                    name:
                        A
                args:
                    [none]
", new ExtractMethodArgsRewriter());

        [TestMethod]
        public void TestArithmetic() => TestHelpers.AssertGeneratesTree(@"
function float A(float a) { return a; }
function void B(float b) { b = 1 + A(b); }
", @"
[root]
declarations:
    [variable declaration]
    identifier:
        [identifier name]
        name:
            A(float)#a
    type:
        float
    initializer:
        [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            A
    type:
        float
    arguments:
        [none]
    block:
        [block]
        statements:
            [return]
            value:
                [identifier name]
                name:
                    A(float)#a
    [variable declaration]
    identifier:
        [identifier name]
        name:
            B(float)#b
    type:
        float
    initializer:
        [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            B
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
                        A(float)#a
                op:
                    =
                rhs:
                    [identifier name]
                    name:
                        B(float)#b
            [expression statement]
            statement:
                [assignment]
                lhs:
                    [identifier name]
                    name:
                        B(float)#b
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
                        [invocation]
                        target:
                            [identifier name]
                            name:
                                A
                        args:
                            [none]
", new ExtractMethodArgsRewriter());

        [TestMethod]
        public void TestIntrinsic() => TestHelpers.AssertGeneratesTree(@"
function void A(float a) { a = sin(a); }
", @"
[root]
declarations:
    [variable declaration]
    identifier:
        [identifier name]
        name:
            A(float)#a
    type:
        float
    initializer:
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
                [assignment]
                lhs:
                    [identifier name]
                    name:
                        A(float)#a
                op:
                    =
                rhs:
                    [invocation]
                    target:
                        [identifier name]
                        name:
                            sin
                    args:
                        [identifier name]
                        name:
                            A(float)#a
", new ExtractMethodArgsRewriter());

        [TestMethod]
        public void TestSpecial() => TestHelpers.AssertGeneratesTree(@"
function void A(float a) { main(a); on_message(a); }
function void main(float value) { }
function void on_message(float value) { }
", @"
[root]
declarations:
    [variable declaration]
    identifier:
        [identifier name]
        name:
            A(float)#a
    type:
        float
    initializer:
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
                [assignment]
                lhs:
                    [identifier name]
                    name:
                        main(float)#value
                op:
                    =
                rhs:
                    [identifier name]
                    name:
                        A(float)#a
            [expression statement]
            statement:
                [invocation]
                target:
                    [identifier name]
                    name:
                        main
                args:
                    [literal float]
                    value:
                        0
            [expression statement]
            statement:
                [assignment]
                lhs:
                    [identifier name]
                    name:
                        on_message(float)#value
                op:
                    =
                rhs:
                    [identifier name]
                    name:
                        A(float)#a
            [expression statement]
            statement:
                [invocation]
                target:
                    [identifier name]
                    name:
                        on_message
                args:
                    [literal float]
                    value:
                        0
    [variable declaration]
    identifier:
        [identifier name]
        name:
            main(float)#value
    type:
        float
    initializer:
        [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            main
    type:
        void
    arguments:
        [local declaration]
        declaration:
            [variable declaration]
            identifier:
                [identifier name]
                name:
                    main(float)#value
            type:
                float
            initializer:
                [none]
    block:
        [block]
        statements:
            [none]
    [variable declaration]
    identifier:
        [identifier name]
        name:
            on_message(float)#value
    type:
        float
    initializer:
        [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            on_message
    type:
        void
    arguments:
        [local declaration]
        declaration:
            [variable declaration]
            identifier:
                [identifier name]
                name:
                    on_message(float)#value
            type:
                float
            initializer:
                [none]
    block:
        [block]
        statements:
            [none]
", new ExtractMethodArgsRewriter());
    }
}