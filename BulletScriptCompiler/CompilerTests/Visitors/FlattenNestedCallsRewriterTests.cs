using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class FlattenNestedCallsRewriterTests {

        [TestMethod]
        public void Test() => TestHelpers.AssertGeneratesTree(@"
function float A(float value) { return value; }
function float B(float value) { return value; }
function float C(float value) { return value; }
function float D(float value) { return value; }
function float E(float value) { return value; }

function void main(float value) {
    float i = E(length([D(4) D(C(3) + B(A(1))) 5]));
}
", @"
[root]
declarations:
    [method declaration]
    identifier:
        [identifier name]
        name:
            A
    type:
        float
    arguments:
        [local declaration]
        declaration:
            [variable declaration]
            identifier:
                [identifier name]
                name:
                    value
            type:
                float
            initializer:
                [none]
    block:
        [block]
        statements:
            [return]
            value:
                [identifier name]
                name:
                    value
    [method declaration]
    identifier:
        [identifier name]
        name:
            B
    type:
        float
    arguments:
        [local declaration]
        declaration:
            [variable declaration]
            identifier:
                [identifier name]
                name:
                    value
            type:
                float
            initializer:
                [none]
    block:
        [block]
        statements:
            [return]
            value:
                [identifier name]
                name:
                    value
    [method declaration]
    identifier:
        [identifier name]
        name:
            C
    type:
        float
    arguments:
        [local declaration]
        declaration:
            [variable declaration]
            identifier:
                [identifier name]
                name:
                    value
            type:
                float
            initializer:
                [none]
    block:
        [block]
        statements:
            [return]
            value:
                [identifier name]
                name:
                    value
    [method declaration]
    identifier:
        [identifier name]
        name:
            D
    type:
        float
    arguments:
        [local declaration]
        declaration:
            [variable declaration]
            identifier:
                [identifier name]
                name:
                    value
            type:
                float
            initializer:
                [none]
    block:
        [block]
        statements:
            [return]
            value:
                [identifier name]
                name:
                    value
    [method declaration]
    identifier:
        [identifier name]
        name:
            E
    type:
        float
    arguments:
        [local declaration]
        declaration:
            [variable declaration]
            identifier:
                [identifier name]
                name:
                    value
            type:
                float
            initializer:
                [none]
    block:
        [block]
        statements:
            [return]
            value:
                [identifier name]
                name:
                    value
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
                    value
            type:
                float
            initializer:
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
                        invocation#result#0
                type:
                    float
                initializer:
                    [invocation]
                    target:
                        [identifier name]
                        name:
                            D
                    args:
                        [literal float]
                        value:
                            4
            [local declaration]
            declaration:
                [variable declaration]
                identifier:
                    [identifier name]
                    name:
                        invocation#result#1
                type:
                    float
                initializer:
                    [invocation]
                    target:
                        [identifier name]
                        name:
                            C
                    args:
                        [literal float]
                        value:
                            3
            [local declaration]
            declaration:
                [variable declaration]
                identifier:
                    [identifier name]
                    name:
                        invocation#result#2
                type:
                    float
                initializer:
                    [invocation]
                    target:
                        [identifier name]
                        name:
                            A
                    args:
                        [literal float]
                        value:
                            1
            [local declaration]
            declaration:
                [variable declaration]
                identifier:
                    [identifier name]
                    name:
                        invocation#result#3
                type:
                    float
                initializer:
                    [invocation]
                    target:
                        [identifier name]
                        name:
                            B
                    args:
                        [identifier name]
                        name:
                            invocation#result#2
            [local declaration]
            declaration:
                [variable declaration]
                identifier:
                    [identifier name]
                    name:
                        invocation#result#4
                type:
                    float
                initializer:
                    [invocation]
                    target:
                        [identifier name]
                        name:
                            D
                    args:
                        [binop]
                        lhs:
                            [identifier name]
                            name:
                                invocation#result#1
                        op:
                            +
                        rhs:
                            [identifier name]
                            name:
                                invocation#result#3
            [local declaration]
            declaration:
                [variable declaration]
                identifier:
                    [identifier name]
                    name:
                        invocation#result#5
                type:
                    float
                initializer:
                    [invocation]
                    target:
                        [identifier name]
                        name:
                            length
                    args:
                        [matrix1x3]
                        entries:
                            [identifier name]
                            name:
                                invocation#result#0
                            [identifier name]
                            name:
                                invocation#result#4
                            [literal float]
                            value:
                                5
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
                    [invocation]
                    target:
                        [identifier name]
                        name:
                            E
                    args:
                        [identifier name]
                        name:
                            invocation#result#5
", new FlattenNestedCallsRewriter());

    }
}