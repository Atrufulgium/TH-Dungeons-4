using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class LoopsToEternalRepeatRewriterTests {

        [TestMethod]
        public void TestEternalRepeat() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
repeat {
    i = 4;
    break;
}
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
    [repeat loop]
    count:
        [none]
    body:
        [block]
        statements:
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
            [break]
", new LoopsToEternalRepeatRewriter());

        [TestMethod]
        public void TestRepeat() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
repeat (i) {
    i = 4;
    break;
}
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
                looptemp#0
        type:
            float
        initializer:
            [identifier name]
            name:
                i
    [repeat loop]
    count:
        [none]
    body:
        [block]
        statements:
            [expression statement]
            statement:
                [postfix]
                op:
                    --
                expression:
                    [identifier name]
                    name:
                        looptemp#0
            [if]
            condition:
                [binop]
                lhs:
                    [identifier name]
                    name:
                        looptemp#0
                op:
                    <
                rhs:
                    [literal float]
                    value:
                        0
            true:
                [block]
                statements:
                    [break]
            false:
                [none]
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
            [break]
", new LoopsToEternalRepeatRewriter());

        [TestMethod]
        public void TestWhile() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
while (i == 3) {
    i = 4;
    break;
}
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
    [repeat loop]
    count:
        [none]
    body:
        [block]
        statements:
            [if]
            condition:
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
                            3
            true:
                [block]
                statements:
                    [break]
            false:
                [none]
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
            [break]
", new LoopsToEternalRepeatRewriter());

        [TestMethod]
        public void TestFor() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
for (float j = 3; i == j; j++) {
    i = 4;
    break;
}
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
                j
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
                looptemp#0
        type:
            float
        initializer:
            [literal float]
            value:
                1
    [repeat loop]
    count:
        [none]
    body:
        [block]
        statements:
            [if]
            condition:
                [identifier name]
                name:
                    looptemp#0
            true:
                [block]
                statements:
                    [expression statement]
                    statement:
                        [assignment]
                        lhs:
                            [identifier name]
                            name:
                                looptemp#0
                        op:
                            =
                        rhs:
                            [literal float]
                            value:
                                0
            false:
                [block]
                statements:
                    [expression statement]
                    statement:
                        [postfix]
                        op:
                            ++
                        expression:
                            [identifier name]
                            name:
                                j
            [if]
            condition:
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
                        [identifier name]
                        name:
                            j
            true:
                [block]
                statements:
                    [break]
            false:
                [none]
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
            [break]
", new LoopsToEternalRepeatRewriter());

        [TestMethod]
        public void TestNested() => TestHelpers.AssertGeneratesTree(@"
repeat (10) {
    for (float i = 0; i < 9; i++) {
        while (i < 8) {
            repeat (i - 7) {
                i = 6;
            }
        }
    }
}
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                looptemp#2
        type:
            float
        initializer:
            [literal float]
            value:
                10
    [repeat loop]
    count:
        [none]
    body:
        [block]
        statements:
            [expression statement]
            statement:
                [postfix]
                op:
                    --
                expression:
                    [identifier name]
                    name:
                        looptemp#2
            [if]
            condition:
                [binop]
                lhs:
                    [identifier name]
                    name:
                        looptemp#2
                op:
                    <
                rhs:
                    [literal float]
                    value:
                        0
            true:
                [block]
                statements:
                    [break]
            false:
                [none]
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
            [local declaration]
            declaration:
                [variable declaration]
                identifier:
                    [identifier name]
                    name:
                        looptemp#1
                type:
                    float
                initializer:
                    [literal float]
                    value:
                        1
            [repeat loop]
            count:
                [none]
            body:
                [block]
                statements:
                    [if]
                    condition:
                        [identifier name]
                        name:
                            looptemp#1
                    true:
                        [block]
                        statements:
                            [expression statement]
                            statement:
                                [assignment]
                                lhs:
                                    [identifier name]
                                    name:
                                        looptemp#1
                                op:
                                    =
                                rhs:
                                    [literal float]
                                    value:
                                        0
                    false:
                        [block]
                        statements:
                            [expression statement]
                            statement:
                                [postfix]
                                op:
                                    ++
                                expression:
                                    [identifier name]
                                    name:
                                        i
                    [if]
                    condition:
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
                                <
                            rhs:
                                [literal float]
                                value:
                                    9
                    true:
                        [block]
                        statements:
                            [break]
                    false:
                        [none]
                    [repeat loop]
                    count:
                        [none]
                    body:
                        [block]
                        statements:
                            [if]
                            condition:
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
                                        <
                                    rhs:
                                        [literal float]
                                        value:
                                            8
                            true:
                                [block]
                                statements:
                                    [break]
                            false:
                                [none]
                            [local declaration]
                            declaration:
                                [variable declaration]
                                identifier:
                                    [identifier name]
                                    name:
                                        looptemp#0
                                type:
                                    float
                                initializer:
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
                                            7
                            [repeat loop]
                            count:
                                [none]
                            body:
                                [block]
                                statements:
                                    [expression statement]
                                    statement:
                                        [postfix]
                                        op:
                                            --
                                        expression:
                                            [identifier name]
                                            name:
                                                looptemp#0
                                    [if]
                                    condition:
                                        [binop]
                                        lhs:
                                            [identifier name]
                                            name:
                                                looptemp#0
                                        op:
                                            <
                                        rhs:
                                            [literal float]
                                            value:
                                                0
                                    true:
                                        [block]
                                        statements:
                                            [break]
                                    false:
                                        [none]
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
", new LoopsToEternalRepeatRewriter());
    }
}