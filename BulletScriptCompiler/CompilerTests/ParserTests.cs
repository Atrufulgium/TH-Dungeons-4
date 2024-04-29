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
        public void CompileTestDeclarationsWithoutStatements() => TestCompile(@"
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

        [TestMethod]
        public void CompileTestExpressionlessStatements1() => TestCompile(@"
return;
continue;
repeat {
    break;
}
", @"
[root]
statements:
    [return]
    value:
        [none]
    [continue]
    [repeat loop]
    count:
        [none]
    body:
        [block]
        statements:
            [break]
");

        [TestMethod]
        public void CompileTestExpressionlessStatements2() => TestCompile(@"
function void Main(float value) {
    return;
    continue;
    repeat {
        break;
    }
}
", @"
[root]
declarations:
    [method declaration]
    identifier:
        [identifier name]
        name:
            Main
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
            [return]
            value:
                [none]
            [continue]
            [repeat loop]
            count:
                [none]
            body:
                [block]
                statements:
                    [break]
");

        [TestMethod]
        public void CompileTestVariableDeclaration1() => TestCompile(@"
float i = 0.2e+9;
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
            [identifier name]
            name:
                float
        initializer:
            [literal float]
            value:
                200000000
");

        [TestMethod]
        public void CompileTestVariableDeclaration2() => TestCompile(@"
string s = ""hoi"";
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                s
        type:
            [identifier name]
            name:
                string
        initializer:
            [literal string]
            value:
                ""hoi""
");

        [TestMethod]
        public void CompileTestVariableDeclaration3() => TestCompile(@"
matrix m = [1 2 3; 4 5 6];
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                m
        type:
            [identifier name]
            name:
                matrix
        initializer:
            [matrix2x3]
            entries:
                [literal float]
                value:
                    1
                [literal float]
                value:
                    2
                [literal float]
                value:
                    3
                [literal float]
                value:
                    4
                [literal float]
                value:
                    5
                [literal float]
                value:
                    6
");

        [TestMethod]
        public void CompileTestVariableDeclaration4() => TestCompile(@"
matrix p = [7:8];
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                p
        type:
            [identifier name]
            name:
                matrix
        initializer:
            [polar]
            angle:
                [literal float]
                value:
                    7
            radius:
                [literal float]
                value:
                    8
");

        [TestMethod]
        public void CompileTestFor1() => TestCompile(@"
for (;true;) {
    break;
}
", @"
[root]
statements:
    [for loop]
    initializer:
        [none]
    condition:
        [literal float]
        value:
            1
    increment:
        [none]
    body:
        [block]
        statements:
            [break]
");

        [TestMethod]
        public void CompileTestFor2() => TestCompile(@"
for (float i = 0; i < 10;) {
    break;
}
", @"
[root]
statements:
    [for loop]
    initializer:
        [local declaration]
        declaration:
            [variable declaration]
            identifier:
                [identifier name]
                name:
                    i
            type:
                [identifier name]
                name:
                    float
            initializer:
                [literal float]
                value:
                    0
    condition:
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
                10
    increment:
        [none]
    body:
        [block]
        statements:
            [break]
");

        [TestMethod]
        public void CompileTestFor3() => TestCompile(@"
for (i = 0; i < 10; i++) {
    break;
}
", @"
[root]
statements:
    [for loop]
    initializer:
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
                    0
    condition:
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
                10
    increment:
        [postfix]
        op:
            ++
        expression:
            [identifier name]
            name:
                i
    body:
        [block]
        statements:
            [break]
");

        [TestMethod]
        public void CompileTestFor4() => TestCompile(@"
for (my_method(); true;) {
    break;
}
", @"
[root]
statements:
    [for loop]
    initializer:
        [expression statement]
        statement:
            [invocation]
            target:
                [identifier name]
                name:
                    my_method
            args:
                [none]
    condition:
        [literal float]
        value:
            1
    increment:
        [none]
    body:
        [block]
        statements:
            [break]
");

        [TestMethod]
        public void CompileTestIf1() => TestCompile(@"
if (true) {
    i++;
}
", @"
[root]
statements:
    [if]
    condition:
        [literal float]
        value:
            1
    true:
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
    false:
        [none]
");

        [TestMethod]
        public void CompileTestIf2() => TestCompile(@"
if (true) {
    i++;
} else {
    j++;
}
", @"
[root]
statements:
    [if]
    condition:
        [literal float]
        value:
            1
    true:
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
");

        [TestMethod]
        public void CompileTestIf3() => TestCompile(@"
if (true) {
    i++;
} else if (false) {
    j++;
}
", @"
[root]
statements:
    [if]
    condition:
        [literal float]
        value:
            1
    true:
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
    false:
        [block]
        statements:
            [if]
            condition:
                [literal float]
                value:
                    0
            true:
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
            false:
                [none]
");

        [TestMethod]
        public void CompileTestIf4() => TestCompile(@"
if (true) {
    i++;
} else if (false) {
    j++;
} else if (-1) {
    k++;
}
", @"
[root]
statements:
    [if]
    condition:
        [literal float]
        value:
            1
    true:
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
    false:
        [block]
        statements:
            [if]
            condition:
                [literal float]
                value:
                    0
            true:
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
            false:
                [block]
                statements:
                    [if]
                    condition:
                        [prefix]
                        op:
                            -
                        expression:
                            [literal float]
                            value:
                                1
                    true:
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
                                        k
                    false:
                        [none]
");

        [TestMethod]
        public void CompileTestRepeat1() => TestCompile(@"
repeat {
    break;
}
", @"
[root]
statements:
    [repeat loop]
    count:
        [none]
    body:
        [block]
        statements:
            [break]
");

        [TestMethod]
        public void CompileTestRepeat2() => TestCompile(@"
repeat (10) {
    break;
}
", @"
[root]
statements:
    [repeat loop]
    count:
        [literal float]
        value:
            10
    body:
        [block]
        statements:
            [break]
");

        [TestMethod]
        public void CompileTestReturn1() => TestCompile(@"
return;
", @"
[root]
statements:
    [return]
    value:
        [none]
");

        [TestMethod]
        public void CompileTestReturn2() => TestCompile(@"
return [1 2; 3 4];
", @"
[root]
statements:
    [return]
    value:
        [matrix2x2]
        entries:
            [literal float]
            value:
                1
            [literal float]
            value:
                2
            [literal float]
            value:
                3
            [literal float]
            value:
                4
");

        [TestMethod]
        public void CompileTestWhile1() => TestCompile(@"
while (true) {
    break;
}
", @"
[root]
statements:
    [while loop]
    condition:
        [literal float]
        value:
            1
    body:
        [block]
        statements:
            [break]
");

        [TestMethod]
        public void CompileTestParentheses1() => TestCompile(@"
float i = (((((1)))));
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
            [identifier name]
            name:
                float
        initializer:
            [literal float]
            value:
                1
");

        [TestMethod]
        public void CompileTestInvocation1() => TestCompile(@"
my_method();
", @"
[root]
statements:
    [expression statement]
    statement:
        [invocation]
        target:
            [identifier name]
            name:
                my_method
        args:
            [none]
");

        [TestMethod]
        public void CompileTestInvocation2() => TestCompile(@"
my_method<230>();
", @"
[root]
statements:
    [expression statement]
    statement:
        [invocation]
        target:
            [identifier name]
            name:
                my_method<230.00>
        args:
            [none]
");

        [TestMethod]
        public void CompileTestInvocation3() => TestCompile(@"
my_method(a, 2, my_method(a));
", @"
[root]
statements:
    [expression statement]
    statement:
        [invocation]
        target:
            [identifier name]
            name:
                my_method
        args:
            [identifier name]
            name:
                a
            [literal float]
            value:
                2
            [invocation]
            target:
                [identifier name]
                name:
                    my_method
            args:
                [identifier name]
                name:
                    a
");

        [TestMethod]
        public void CompileTestInvocation4() => TestCompile(@"
float a = my_method(a);
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
            [identifier name]
            name:
                float
        initializer:
            [invocation]
            target:
                [identifier name]
                name:
                    my_method
            args:
                [identifier name]
                name:
                    a
");

        [TestMethod]
        public void CompileTestMatrixLike1() => TestCompile(@"
matrix2x2 m = [1 2; 3 4];
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                m
        type:
            [identifier name]
            name:
                matrix2x2
        initializer:
            [matrix2x2]
            entries:
                [literal float]
                value:
                    1
                [literal float]
                value:
                    2
                [literal float]
                value:
                    3
                [literal float]
                value:
                    4
");

        [TestMethod]
        public void CompileTestMatrixLike2() => TestCompile(@"
matrix m = [1 2 3 4; 5 6 7 8; 9 10 11 12; 13 14 15 16];
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                m
        type:
            [identifier name]
            name:
                matrix
        initializer:
            [matrix4x4]
            entries:
                [literal float]
                value:
                    1
                [literal float]
                value:
                    2
                [literal float]
                value:
                    3
                [literal float]
                value:
                    4
                [literal float]
                value:
                    5
                [literal float]
                value:
                    6
                [literal float]
                value:
                    7
                [literal float]
                value:
                    8
                [literal float]
                value:
                    9
                [literal float]
                value:
                    10
                [literal float]
                value:
                    11
                [literal float]
                value:
                    12
                [literal float]
                value:
                    13
                [literal float]
                value:
                    14
                [literal float]
                value:
                    15
                [literal float]
                value:
                    16
");

        [TestMethod]
        public void CompileTestMatrixLike3() => TestCompile(@"
matrix m = [1 2 3 4];
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                m
        type:
            [identifier name]
            name:
                matrix
        initializer:
            [matrix1x4]
            entries:
                [literal float]
                value:
                    1
                [literal float]
                value:
                    2
                [literal float]
                value:
                    3
                [literal float]
                value:
                    4
");

        [TestMethod]
        public void CompileTestMatrixLike4() => TestCompile(@"
matrix m = [1; 2; 3; 4];
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                m
        type:
            [identifier name]
            name:
                matrix
        initializer:
            [matrix4x1]
            entries:
                [literal float]
                value:
                    1
                [literal float]
                value:
                    2
                [literal float]
                value:
                    3
                [literal float]
                value:
                    4
");

        [TestMethod]
        public void CompileTestMatrixLike5() => TestCompile(@"
matrix m = [1];
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                m
        type:
            [identifier name]
            name:
                matrix
        initializer:
            [matrix1x1]
            entries:
                [literal float]
                value:
                    1
");

        [TestMethod]
        public void CompileTestMatrixLike6() => TestCompile(@"
matrix m = [1 : 2];
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                m
        type:
            [identifier name]
            name:
                matrix
        initializer:
            [polar]
            angle:
                [literal float]
                value:
                    1
            radius:
                [literal float]
                value:
                    2
");

        [TestMethod]
        public void CompileTestMatrixLike7() => TestCompile(@"
matrix m = [1 + 2 + 3 4 + 5 + 6 (7 + 8 + 9)];
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                m
        type:
            [identifier name]
            name:
                matrix
        initializer:
            [matrix1x3]
            entries:
                [binop]
                lhs:
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
                op:
                    +
                rhs:
                    [literal float]
                    value:
                        3
                [binop]
                lhs:
                    [binop]
                    lhs:
                        [literal float]
                        value:
                            4
                    op:
                        +
                    rhs:
                        [literal float]
                        value:
                            5
                op:
                    +
                rhs:
                    [literal float]
                    value:
                        6
                [binop]
                lhs:
                    [binop]
                    lhs:
                        [literal float]
                        value:
                            7
                    op:
                        +
                    rhs:
                        [literal float]
                        value:
                            8
                op:
                    +
                rhs:
                    [literal float]
                    value:
                        9
");

        [TestMethod]
        public void CompileTestArithmetic1() => TestCompile(@"
float i = 1 + 2 - 3 * 4 / 5 % 6 ^ 7 & 8 | 9 > 0;
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
            [identifier name]
            name:
                float
        initializer:
            [binop]
            lhs:
                [binop]
                lhs:
                    [binop]
                    lhs:
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
                    op:
                        -
                    rhs:
                        [binop]
                        lhs:
                            [binop]
                            lhs:
                                [binop]
                                lhs:
                                    [literal float]
                                    value:
                                        3
                                op:
                                    *
                                rhs:
                                    [literal float]
                                    value:
                                        4
                            op:
                                /
                            rhs:
                                [literal float]
                                value:
                                    5
                        op:
                            %
                        rhs:
                            [binop]
                            lhs:
                                [literal float]
                                value:
                                    6
                            op:
                                ^
                            rhs:
                                [literal float]
                                value:
                                    7
                op:
                    &
                rhs:
                    [literal float]
                    value:
                        8
            op:
                |
            rhs:
                [binop]
                lhs:
                    [literal float]
                    value:
                        9
                op:
                    >
                rhs:
                    [literal float]
                    value:
                        0
");

        [TestMethod]
        public void CompileTestArithmetic2() => TestCompile(@"
float i = 1 - - ! ! - ! - 2;
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
            [identifier name]
            name:
                float
        initializer:
            [binop]
            lhs:
                [literal float]
                value:
                    1
            op:
                -
            rhs:
                [prefix]
                op:
                    -
                expression:
                    [prefix]
                    op:
                        !
                    expression:
                        [prefix]
                        op:
                            !
                        expression:
                            [prefix]
                            op:
                                -
                            expression:
                                [prefix]
                                op:
                                    !
                                expression:
                                    [prefix]
                                    op:
                                        -
                                    expression:
                                        [literal float]
                                        value:
                                            2
");

        [TestMethod]
        public void CompileTestArithmetic3() => TestCompile(@"
i ^= 1 + 1;
", @"
[root]
statements:
    [expression statement]
    statement:
        [assignment]
        lhs:
            [identifier name]
            name:
                i
        op:
            ^
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
                    1
");

    }
}