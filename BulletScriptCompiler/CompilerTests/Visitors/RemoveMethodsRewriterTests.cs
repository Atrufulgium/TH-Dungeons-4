using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class RemoveMethodsRewriterTests {

        [TestMethod]
        public void TestSimple() => TestHelpers.AssertGeneratesTree(@"
float a;
function void A() { a = 3; C(); }
function void B() { A(); C(); }
function void C() { a = 5; }
", @"
[root]
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
            [none]
    [local declaration noinit]
    identifier:
        [identifier name]
        name:
            A()#entry
    type:
        float
    [local declaration noinit]
    identifier:
        [identifier name]
        name:
            B()#entry
    type:
        float
    [local declaration noinit]
    identifier:
        [identifier name]
        name:
            C()#entry
    type:
        float
    [label]
    name:
        A()
    [expression statement]
    statement:
        [assignment]
        lhs:
            [identifier name]
            name:
                a
        op:
            =
        rhs:
            [literal float]
            value:
                3
    [expression statement]
    statement:
        [assignment]
        lhs:
            [identifier name]
            name:
                C()#entry
        op:
            =
        rhs:
            [literal float]
            value:
                0
    [goto]
    target:
        [label]
        name:
            C()
    [label]
    name:
        C()#return-to-entry#0
    [label]
    name:
        A()#return
    [local declaration noinit]
    identifier:
        [identifier name]
        name:
            global#returntest
    type:
        float
    [expression statement]
    statement:
        [assignment]
        lhs:
            [identifier name]
            name:
                global#returntest
        op:
            =
        rhs:
            [binop]
            lhs:
                [identifier name]
                name:
                    A()#entry
            op:
                ==
            rhs:
                [literal float]
                value:
                    0
    [conditional goto]
    condition:
        [identifier name]
        name:
            global#returntest
    target:
        [goto]
        target:
            [label]
            name:
                A()#return-to-entry#0
    [label]
    name:
        B()
    [expression statement]
    statement:
        [assignment]
        lhs:
            [identifier name]
            name:
                A()#entry
        op:
            =
        rhs:
            [literal float]
            value:
                0
    [goto]
    target:
        [label]
        name:
            A()
    [label]
    name:
        A()#return-to-entry#0
    [expression statement]
    statement:
        [assignment]
        lhs:
            [identifier name]
            name:
                C()#entry
        op:
            =
        rhs:
            [literal float]
            value:
                1
    [goto]
    target:
        [label]
        name:
            C()
    [label]
    name:
        C()#return-to-entry#1
    [label]
    name:
        B()#return
    [local declaration noinit]
    identifier:
        [identifier name]
        name:
            global#returntest
    type:
        float
    [label]
    name:
        C()
    [expression statement]
    statement:
        [assignment]
        lhs:
            [identifier name]
            name:
                a
        op:
            =
        rhs:
            [literal float]
            value:
                5
    [label]
    name:
        C()#return
    [local declaration noinit]
    identifier:
        [identifier name]
        name:
            global#returntest
    type:
        float
    [expression statement]
    statement:
        [assignment]
        lhs:
            [identifier name]
            name:
                global#returntest
        op:
            =
        rhs:
            [binop]
            lhs:
                [identifier name]
                name:
                    C()#entry
            op:
                ==
            rhs:
                [literal float]
                value:
                    0
    [conditional goto]
    condition:
        [identifier name]
        name:
            global#returntest
    target:
        [goto]
        target:
            [label]
            name:
                C()#return-to-entry#0
    [expression statement]
    statement:
        [assignment]
        lhs:
            [identifier name]
            name:
                global#returntest
        op:
            =
        rhs:
            [binop]
            lhs:
                [identifier name]
                name:
                    C()#entry
            op:
                ==
            rhs:
                [literal float]
                value:
                    1
    [conditional goto]
    condition:
        [identifier name]
        name:
            global#returntest
    target:
        [goto]
        target:
            [label]
            name:
                C()#return-to-entry#1
", new RemoveMethodsRewriter());
    }
}