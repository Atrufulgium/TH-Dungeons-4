using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class RepeatsAndBranchesToGotoRewriterTests {

        [TestMethod]
        public void TestRepeat() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
repeat {
    i = 4;
    break;
}
i = 5;
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
                3
    [label]
    name:
        continue-label-0
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
            [literal float]
            value:
                4
    [goto]
    target:
        [label]
        name:
            break-label-1
    [goto]
    target:
        [label]
        name:
            continue-label-0
    [label]
    name:
        break-label-1
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
            [literal float]
            value:
                5
", new RepeatsAndBranchesToGotoRewriter());

        [TestMethod]
        public void TestIf() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
if (i == 4) {
    i = 5;
}
i = 7;
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
                3
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                global#if#temp
        type:
            float
        initializer:
            [prefix]
            op:
                !
            expression:
                [binop]
                lhs:
                    [identifier name]
                    name:
                        i
                op:
                    ==
                rhs:
                    [literal float]
                    value:
                        4
    [conditional goto]
    condition:
        [identifier name]
        name:
            global#if#temp
    target:
        [goto]
        target:
            [label]
            name:
                done-label-0
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
            [literal float]
            value:
                5
    [label]
    name:
        done-label-0
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
            [literal float]
            value:
                7
", new RepeatsAndBranchesToGotoRewriter());

        [TestMethod]
        public void TestIfElse() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
if (i == 4) {
    i = 5;
} else {
    i = 6;
}
i = 7;
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
                3
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                global#if#temp
        type:
            float
        initializer:
            [prefix]
            op:
                !
            expression:
                [binop]
                lhs:
                    [identifier name]
                    name:
                        i
                op:
                    ==
                rhs:
                    [literal float]
                    value:
                        4
    [conditional goto]
    condition:
        [identifier name]
        name:
            global#if#temp
    target:
        [goto]
        target:
            [label]
            name:
                false-branch-label-1
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
            [literal float]
            value:
                5
    [goto]
    target:
        [label]
        name:
            done-label-0
    [label]
    name:
        false-branch-label-1
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
            [literal float]
            value:
                6
    [label]
    name:
        done-label-0
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
            [literal float]
            value:
                7
", new RepeatsAndBranchesToGotoRewriter());
    }
}