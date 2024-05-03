using Atrufulgium.BulletScript.Compiler.Parsing;
using CompilerTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Tests {

    [TestClass]
    public class ParserTests {

        static void TestCompile(string code, string tree) {
            TestHelpers.AssertTrimmedStringsEqual(Compile(code), tree);
        }
        static string Compile(string code) {
            var (tokens, diags) = new Lexer().ToTokens(code);
            TestHelpers.AssertNoErrorDiagnostics(diags);
            var (root, diags2) = new Parser().ToTree(tokens);
            TestHelpers.AssertNoErrorDiagnostics(diags2);
            if (root == null)
                Assert.Fail("Unexpected null tree, without any diagnostics.");
            var diags3 = root.ValidateTree();
            TestHelpers.AssertNoErrorDiagnostics(diags3);
            return root.ToString();
        }
        static void TestFail(string code, string expectedErrorID, int expectedLine) {
            var (tokens, diags) = new Lexer().ToTokens(code);
            TestHelpers.AssertNoErrorDiagnostics(diags);
            var (root, diags2) = new Parser().ToTree(tokens);
            if (root != null)
                diags2 = diags2.Union(root.ValidateTree()).ToList();
            foreach (var diag in diags2) {
                if (diag.ID == expectedErrorID && diag.Location.line == expectedLine + 1)
                    return;
            }
            string msg = $"Did not encounter error \"{expectedErrorID}\" at line {expectedLine + 1}.";
            if (diags2.Any()) {
                msg += " Errors:\n";
                foreach (var diag in diags2) {
                    msg += $"{diag}\n";
                }
            } else {
                msg += " No errors at all.";
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
            [none]
    [method declaration]
    identifier:
        [identifier name]
        name:
            on_health<0.50>
    type:
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
repeat {
    continue;
    break;
}
", @"
[root]
statements:
    [return]
    value:
        [none]
    [repeat loop]
    count:
        [none]
    body:
        [block]
        statements:
            [continue]
            [break]
");

        [TestMethod]
        public void CompileTestExpressionlessStatements2() => TestCompile(@"
function void Main(float value) {
    return;
    repeat {
        continue;
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
            [return]
            value:
                [none]
            [repeat loop]
            count:
                [none]
            body:
                [block]
                statements:
                    [continue]
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
            matrix
        initializer:
            [matrix1x3]
            entries:
                [binop]
                lhs:
                    [literal float]
                    value:
                        1
                op:
                    +
                rhs:
                    [binop]
                    lhs:
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
                    [literal float]
                    value:
                        4
                op:
                    +
                rhs:
                    [binop]
                    lhs:
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
                    [literal float]
                    value:
                        7
                op:
                    +
                rhs:
                    [binop]
                    lhs:
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
            float
        initializer:
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
                        [binop]
                        lhs:
                            [literal float]
                            value:
                                2
                        op:
                            -
                        rhs:
                            [binop]
                            lhs:
                                [literal float]
                                value:
                                    3
                            op:
                                *
                            rhs:
                                [binop]
                                lhs:
                                    [literal float]
                                    value:
                                        4
                                op:
                                    /
                                rhs:
                                    [binop]
                                    lhs:
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

        [TestMethod]
        public void CompileTestIndex() => TestCompile(@"
i = m[2];
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
            =
        rhs:
            [index]
            expression:
                [identifier name]
                name:
                    m
            index:
                [matrix1x1]
                entries:
                    [literal float]
                    value:
                        2
");

        [TestMethod]
        public void ErrorTestStatementInDeclarationMode() => TestFail(@"
function void hoi() {}
i += 3;
", "BS0004", 2);

        [TestMethod]
        public void ErrorTestMethodDeclMissingReturntype() => TestFail(@"
function hoi() {}
", "BS0006", 1);

        [TestMethod]
        public void ErrorTestMethodDeclNonexistentReturntype() => TestFail(@"
function my_type hoi() {}
", "BS0006", 1);

        [TestMethod]
        public void ErrorTestMethodDeclMissingParens1() => TestFail(@"
function void hoi {}
", "BS0022", 1);

        [TestMethod]
        public void ErrorTestMethodDeclMissingParens2() => TestFail(@"
function void hoi( {}
", "BS0021", 1);

        [TestMethod]
        public void ErrorTestMethodDeclMissingBlock() => TestFail(@"
function void hoi()
float ensure_no_eof;
", "BS0013", 2);

        [TestMethod]
        public void ErrorTestMethodDeclArgMissingType() => TestFail(@"
function void hoi(value) {}
", "BS0016", 1);

        [TestMethod]
        public void ErrorTestMethodDeclArgMissingName() => TestFail(@"
function void hoi(float) {}
", "BS0017", 1);

        [TestMethod]
        public void ErrorTestMethodDeclArgMissing() => TestFail(@"
function void hoi(float i,) {}
", "BS0016", 1);

        [TestMethod]
        public void ErrorTestMethodDeclArgWeirdName() => TestFail(@"
function void hoi(float float) {}
", "BS0017", 1);

        [TestMethod]
        public void ErrorTestVariableDeclWeirdName() => TestFail(@"
float float = 3;
function void ensure_toplevel_form() {}
", "BS0017", 1);

        [TestMethod]
        public void ErrorTestVariableDeclWeirdOp() => TestFail(@"
float i += 3;
function void ensure_toplevel_form() {}
", "BS0034", 1);

        [TestMethod]
        public void ErrorTestVariableDeclMissingSemicolon1() => TestFail(@"
float i
function void ensure_toplevel_form() {}
", "BS0018", 2);

        [TestMethod]
        public void ErrorTestVariableDeclMissingSemicolon2() => TestFail(@"
float i
function void ensure_toplevel_form() {}
", "BS0018", 2);

        [TestMethod]
        public void ErrorTestBlockMissingClose1() => TestFail(@"
function void a() {
function void b() {}
", "BS0019", 2);

        [TestMethod]
        public void ErrorTestBlockMissingClose2() => TestFail(@"
function void a() {
    return;
", "BS0014", 4);

        [TestMethod]
        public void ErrorTestStatementMissingSemicolon() => TestFail(@"
float i = 3
float j = 4;
", "BS0018", 2);

        [TestMethod]
        public void ErrorTestForMissingPart1() => TestFail(@"
for () {}
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestForMissingPart2() => TestFail(@"
for (float i = 0) {}
", "BS0018", 1);

        [TestMethod]
        public void ErrorTestForMissingPart3() => TestFail(@"
for (float i = 0;;) {}
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestForMissingParens1() => TestFail(@"
for float i = 0; true; i++) {}
", "BS0022", 1);

        [TestMethod]
        public void ErrorTestForMissingParens2() => TestFail(@"
for (float i = 0; true; i++ {}
", "BS0021", 1);

        [TestMethod]
        public void ErrorTestForMissingBlock() => TestFail(@"
for (float i = 0; true; i++)
    i--;
", "BS0013", 2);

        [TestMethod]
        public void ErrorTestForInvalidInit() => TestFail(@"
for (return; true; i++) {}
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestForTooMuch() => TestFail(@"
for (float i = 0; true; i++; i++) {}
", "BS0021", 1);

        [TestMethod]
        public void ErrorTestIfMissingParens1() => TestFail(@"
if (true {}
", "BS0021", 1);

        [TestMethod]
        public void ErrorTestIfMissingParens2() => TestFail(@"
if true) {}
", "BS0022", 1);

        [TestMethod]
        public void ErrorTestIfMissingBlock() => TestFail(@"
if (true)
    i = 3;
", "BS0013", 2);

        [TestMethod]
        public void ErrorTestIfElseMissingBlock() => TestFail(@"
if (true) {}
else
    i = 3;
", "BS0013", 3);

        [TestMethod]
        public void ErrorTestIfWeirdCondition1() => TestFail(@"
if () {}
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestIfWeirdCondition2() => TestFail(@"
if (return) {}
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestRepeatMissingBlock1() => TestFail(@"
repeat
    i++;
", "BS0013", 2);

        [TestMethod]
        public void ErrorTestRepeatMissingBlock2() => TestFail(@"
repeat (10)
    i++;
", "BS0013", 2);

        [TestMethod]
        public void ErrorTestRepeatMissingParens1() => TestFail(@"
repeat (10 {}
", "BS0021", 1);

        // Pretty disagreeable, but getting a better error message seems like
        // a lot of work for little reward.
        [TestMethod]
        public void ErrorTestRepeatMissingParens2() => TestFail(@"
repeat 10) {}
", "BS0013", 1);

        [TestMethod]
        public void ErrorTestRepeatWeirdCondition1() => TestFail(@"
repeat () {}
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestRepeatWeirdCondition2() => TestFail(@"
repeat (return) {}
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestWhileMissingParens1() => TestFail(@"
while (true {}
", "BS0021", 1);

        [TestMethod]
        public void ErrorTestWhileMissingParens2() => TestFail(@"
while true) {}
", "BS0022", 1);

        [TestMethod]
        public void ErrorTestWhileMissingCondition() => TestFail(@"
while {}
", "BS0022", 1);

        [TestMethod]
        public void ErrorTestWhileMissingBlock() => TestFail(@"
while (true) i++;
", "BS0013", 1);

        [TestMethod]
        public void ErrorTestWhileWeirdCondition1() => TestFail(@"
while () {}
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestWhileWeirdCondition2() => TestFail(@"
while (return) {}
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestWeirdOp() => TestFail(@"
i =+ 1;
", "BS0024", 1);

        // If adding any op ∘ that does not have a ∘= variant, test here.

        [TestMethod]
        public void ErrorTestWeirdParens1() => TestFail(@"
i = (((1));
", "BS0021", 1);

        [TestMethod]
        public void ErrorTestWeirdParens2() => TestFail(@"
i = ((1)));
", "BS0018", 1);

        [TestMethod]
        public void ErrorTestInvocationMissingParens1() => TestFail(@"
my_method(3;
", "BS0021", 1);

        // Also pretty suboptimal, but oh well.
        [TestMethod]
        public void ErrorTestInvocationMissingParens2() => TestFail(@"
my_method3);
", "BS0018", 1);

        [TestMethod]
        public void ErrorTestInvocationWeirdArg() => TestFail(@"
my_method(1,2,3,return,5,6);
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestMatrix1() => TestFail(@"
matrix m = [];
", "BS0027", 1);

        [TestMethod]
        public void ErrorTestMatrix2() => TestFail(@"
matrix m = [1; 1 2];
", "BS0028", 1);

        [TestMethod]
        public void ErrorTestMatrix3() => TestFail(@"
matrix m = [1 2 3 4 5];
", "BS0029", 1);

        [TestMethod]
        public void ErrorTestMatrix4() => TestFail(@"
matrix m = [1; 2; 3; 4; 5];
", "BS0029", 1);

        [TestMethod]
        public void ErrorTestMatrix5() => TestFail(@"
matrix m = [1 2 : 3; 4 5 6];
", "BS0030", 1);

        [TestMethod]
        public void ErrorTestMatrix6() => TestFail(@"
matrix m = [1 : 2 : 3];
", "BS0030", 1);

        [TestMethod]
        public void ErrorTestMatrix7() => TestFail(@"
matrix m = [;1];
", "BS0027", 1);

        [TestMethod]
        public void ErrorTestMatrix8() => TestFail(@"
matrix m = [1;];
", "BS0027", 1);

        [TestMethod]
        public void ErrorTestMatrix9() => TestFail(@"
matrix m = [:1 2];
", "BS0030", 1);

        [TestMethod]
        public void ErrorTestMatrix10() => TestFail(@"
matrix m = [1 2:];
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestMatrix11() => TestFail(@"
matrix m = [1:;];
", "BS0024", 1);

        [TestMethod]
        public void ErrorTestEmptyStatement() => TestFail(@"
;
", "BS0019", 1);

        [TestMethod]
        public void ErrorTestBreakNotInLoop() => TestFail(@"
function void main() {
    break;
}
", "BS0035", 2);

        [TestMethod]
        public void ErrorTestContinueNotInLoop() => TestFail(@"
function void main() {
    continue;
}
", "BS0036", 2);

        [TestMethod]
        public void ErrorTestInvalidForInitializer() => TestFail(@"
for (1 + 1; true;) {}
", "BS0037", 1);

        [TestMethod]
        public void ErrorTestAssignmentOnlyAsStatement() => TestFail(@"
i = j = 3;
", "BS0039", 1);

        [TestMethod]
        public void ErrorTestIndexWrong1() => TestFail(@"
i = m[];
", "BS0027", 1);

        [TestMethod]
        public void ErrorTestIndexWrong2() => TestFail(@"
i = m[1 2 3];
", "BS0040", 1);

        [TestMethod]
        public void ErrorTestIndexWrong3() => TestFail(@"
i = m[1; 2; 3];
", "BS0040", 1);

        [TestMethod]
        public void ErrorTestIndexWrong4() => TestFail(@"
i = m[1 : 2];
", "BS0023", 1);

        [TestMethod]
        public void ErrorTestInitInMethodDecl1() => TestFail(@"
function void my_method(float val = 3) {}
", "BS0041", 1);

        [TestMethod]
        public void ErrorTestInitInMethodDecl2() => TestFail(@"
function void my_method(matrix val) {}
", "BS0046", 1);

        [TestMethod]
        public void ErrorTestIncrDecrOnNonIdentifiers() => TestFail(@"
float i = (1+1)++;
", "BS0024", 1);
    }
}