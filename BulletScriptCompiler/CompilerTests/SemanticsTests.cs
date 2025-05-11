using Atrufulgium.BulletScript.Compiler.Parsing;
using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Tests {

    [TestClass]
    public class SemanticsTests {

        static void TestTable(string code, string symbolTable, bool includeIntrinsics = false) {
            TestHelpers.AssertTrimmedStringsEqual(symbolTable, Compile(code, includeIntrinsics));
        }
        static void TestFail(string code, string errorID, int errorLine) {
            var (tokens, diags) = new Lexer().ToTokens(code);
            TestHelpers.AssertNoErrorDiagnostics(diags);
            var (root, diags2) = new Parser().ToTree(tokens);
            TestHelpers.AssertNoErrorDiagnostics(diags2);
            if (root == null)
                Assert.Fail("Unexpected null tree, without any diagnostics.");
            var diags3 = root.ValidateTree();
            TestHelpers.AssertNoErrorDiagnostics(diags3);
            var semanticModel = new SemanticModel(root);
            TestHelpers.AssertContainsDiagnostic(semanticModel.Diagnostics, errorID, errorLine);
        }

        static string Compile(string code, bool includeIntrinsics = false) {
            var (tokens, diags) = new Lexer().ToTokens(code);
            TestHelpers.AssertNoErrorDiagnostics(diags);
            var (root, diags2) = new Parser().ToTree(tokens);
            TestHelpers.AssertNoErrorDiagnostics(diags2);
            if (root == null)
                Assert.Fail("Unexpected null tree, without any diagnostics.");
            var diags3 = root.ValidateTree();
            TestHelpers.AssertNoErrorDiagnostics(diags3);
            var semanticModel = new SemanticModel(root);
            TestHelpers.AssertNoErrorDiagnostics(semanticModel.Diagnostics);
            return semanticModel.ToString(includeIntrinsics);
        }

        [TestMethod]
        public void TableTest1() => TestTable(@"
function void main(float value) {
    float value2;
    matrix mat = [value value2];
    string str = ""hi"";
}
function void on_message(float value) {
    value += value;
}
", @"
Fully qualified name             | Declaration location | Container location   | Type      | Refs | Notes
---------------------------------+----------------------+----------------------+-----------+------+----------
main(float)                      | Line 2, col 1        | [n/a]                | void      |    1 | 
main(float).value                | Line 2, col 20       | Line 2, col 1        | float     |    1 | Read
main(float).value2               | Line 3, col 5        | Line 2, col 1        | float     |    1 | Read
main(float).mat                  | Line 4, col 5        | Line 2, col 1        | matrix1x2 |    0 | 
main(float).str                  | Line 5, col 5        | Line 2, col 1        | string    |    0 | 
on_message(float)                | Line 7, col 1        | [n/a]                | void      |    1 | 
on_message(float).value          | Line 7, col 26       | Line 7, col 1        | float     |    2 | Read, Written
");

        // The global-scoped `value` is hidden by the local scoped ones.
        [TestMethod]
        public void TableTest2() => TestTable(@"
float value;
function void main(float value) {
    float value2;
    matrix mat = [value value2];
    string str = ""hi"";
}
function void on_message(float value) {
    value += value;
}
", @"
Fully qualified name             | Declaration location | Container location   | Type      | Refs | Notes
---------------------------------+----------------------+----------------------+-----------+------+----------
value                            | Line 2, col 1        | [n/a]                | float     |    0 | 
main(float)                      | Line 3, col 1        | [n/a]                | void      |    1 | 
main(float).value                | Line 3, col 20       | Line 3, col 1        | float     |    1 | Read
main(float).value2               | Line 4, col 5        | Line 3, col 1        | float     |    1 | Read
main(float).mat                  | Line 5, col 5        | Line 3, col 1        | matrix1x2 |    0 | 
main(float).str                  | Line 6, col 5        | Line 3, col 1        | string    |    0 | 
on_message(float)                | Line 8, col 1        | [n/a]                | void      |    1 | 
on_message(float).value          | Line 8, col 26       | Line 8, col 1        | float     |    2 | Read, Written
");

        // Whether all expressions properly count the refcount of a.
        // `a` decl: +0. b decl: +1. b set: +1. b arith: +1. for: +3.
        // if: +1. matrix: +1. polar: +1. ++: +1. --: +1. repeat: +1.
        // while: +1.
        [TestMethod]
        public void TableTest3() => TestTable(@"
float a = 0;
float b = a;
b = a;
b = a + b;
for (a = 0; a < 1; a++) {}
if (a < 1) {}
matrix c = [a 1];
c = [a:1];
a++;
a--;
repeat(a) {}
while(a < 1) {}
", @"
Fully qualified name             | Declaration location | Container location   | Type      | Refs | Notes
---------------------------------+----------------------+----------------------+-----------+------+----------
a                                | Line 2, col 1        | [n/a]                | float     |   13 | Read, Written
b                                | Line 3, col 1        | [n/a]                | float     |    3 | Read, Written
c                                | Line 8, col 1        | [n/a]                | matrix1x2 |    1 | Written
");

        // Whether in/decrement counts as both a read and write.
        // Idem +=.
        [TestMethod]
        public void TableTest4() => TestTable(@"
float a = 0;
float b = a++;
float c = b--;
c += 1;
", @"
Fully qualified name             | Declaration location | Container location   | Type      | Refs | Notes
---------------------------------+----------------------+----------------------+-----------+------+----------
a                                | Line 2, col 1        | [n/a]                | float     |    1 | Read, Written
b                                | Line 3, col 1        | [n/a]                | float     |    1 | Read, Written
c                                | Line 4, col 1        | [n/a]                | float     |    1 | Read, Written
");

        // Some calling testing.
        [TestMethod]
        public void TableTest5() => TestTable(@"
function void A() { sin(230); B(); C(); D(); E(); }
function void B() { C(); D(); E(); }
function void C() { sin(230); D(); E(); }
function void D() { E(); }
function void E() { sin(230); }
", @"
Fully qualified name             | Declaration location | Container location   | Type      | Refs | Notes
---------------------------------+----------------------+----------------------+-----------+------+----------
A()                              | Line 2, col 1        | [n/a]                | void      |    0 | Calls B()
                                 |                      |                      |           |      | Calls C()
                                 |                      |                      |           |      | Calls D()
                                 |                      |                      |           |      | Calls E()
                                 |                      |                      |           |      | Calls sin(float)
B()                              | Line 3, col 1        | [n/a]                | void      |    1 | Called by A()
                                 |                      |                      |           |      | Calls C()
                                 |                      |                      |           |      | Calls D()
                                 |                      |                      |           |      | Calls E()
C()                              | Line 4, col 1        | [n/a]                | void      |    2 | Called by A()
                                 |                      |                      |           |      | Called by B()
                                 |                      |                      |           |      | Calls D()
                                 |                      |                      |           |      | Calls E()
                                 |                      |                      |           |      | Calls sin(float)
D()                              | Line 5, col 1        | [n/a]                | void      |    3 | Called by A()
                                 |                      |                      |           |      | Called by B()
                                 |                      |                      |           |      | Called by C()
                                 |                      |                      |           |      | Calls E()
E()                              | Line 6, col 1        | [n/a]                | void      |    4 | Called by A()
                                 |                      |                      |           |      | Called by B()
                                 |                      |                      |           |      | Called by C()
                                 |                      |                      |           |      | Called by D()
                                 |                      |                      |           |      | Calls sin(float)
");

        // Intrinsics. This one is prone to updating => breaking a lot.
        // Includes two "spawn"s to check whether the refcounts go as they should.
        // (okay yeah _maybe_ i should be putting this stuff in another file.)
        [TestMethod]
        public void TableTest6() => TestTable(@"
spawn();
spawn();
", @"
Fully qualified name                       | Declaration location | Container location   | Type      | Refs | Notes
-------------------------------------------+----------------------+----------------------+-----------+------+----------
abs(float)                                 | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
abs(float).value                           | (Not user code)      | (Not user code)      | float     |    0 | Read
abs(matrix1x2)                             | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
abs(matrix1x2).value                       | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
abs(matrix1x3)                             | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
abs(matrix1x3).value                       | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
abs(matrix1x4)                             | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
abs(matrix1x4).value                       | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
acos(float)                                | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
acos(float).value                          | (Not user code)      | (Not user code)      | float     |    0 | Read
acos(matrix1x2)                            | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
acos(matrix1x2).value                      | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
acos(matrix1x3)                            | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
acos(matrix1x3).value                      | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
acos(matrix1x4)                            | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
acos(matrix1x4).value                      | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
addscript()                                | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
addscript(string,float)                    | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
addscript(string,float).passed_value       | (Not user code)      | (Not user code)      | float     |    0 | Read
addscript(string,float).script_id          | (Not user code)      | (Not user code)      | string    |    0 | Read
addscript(string)                          | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
addscript(string).script_id                | (Not user code)      | (Not user code)      | string    |    0 | Read
addspeed(float)                            | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
addspeed(float).amount                     | (Not user code)      | (Not user code)      | float     |    0 | Read
asin(float)                                | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
asin(float).value                          | (Not user code)      | (Not user code)      | float     |    0 | Read
asin(matrix1x2)                            | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
asin(matrix1x2).value                      | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
asin(matrix1x3)                            | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
asin(matrix1x3).value                      | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
asin(matrix1x4)                            | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
asin(matrix1x4).value                      | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
atan(float)                                | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
atan(float).value                          | (Not user code)      | (Not user code)      | float     |    0 | Read
atan(matrix1x2)                            | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
atan(matrix1x2).value                      | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
atan(matrix1x3)                            | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
atan(matrix1x3).value                      | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
atan(matrix1x4)                            | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
atan(matrix1x4).value                      | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
atan2(float,float)                         | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
atan2(float,float).x                       | (Not user code)      | (Not user code)      | float     |    0 | Read
atan2(float,float).y                       | (Not user code)      | (Not user code)      | float     |    0 | Read
atan2(matrix1x2,matrix1x2)                 | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
atan2(matrix1x2,matrix1x2).x               | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
atan2(matrix1x2,matrix1x2).y               | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
atan2(matrix1x3,matrix1x3)                 | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
atan2(matrix1x3,matrix1x3).x               | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
atan2(matrix1x3,matrix1x3).y               | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
atan2(matrix1x4,matrix1x4)                 | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
atan2(matrix1x4,matrix1x4).x               | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
atan2(matrix1x4,matrix1x4).y               | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
autoclear                                  | (Not user code)      | [n/a]                | float     |    2 | Read
bullettype                                 | (Not user code)      | [n/a]                | string    |    2 | Read
ceil(float)                                | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
ceil(float).value                          | (Not user code)      | (Not user code)      | float     |    0 | Read
ceil(matrix1x2)                            | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
ceil(matrix1x2).value                      | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
ceil(matrix1x3)                            | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
ceil(matrix1x3).value                      | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
ceil(matrix1x4)                            | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
ceil(matrix1x4).value                      | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
clearimmune                                | (Not user code)      | [n/a]                | float     |    2 | Read
clearingtype                               | (Not user code)      | [n/a]                | float     |    2 | Read
cos(float)                                 | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
cos(float).value                           | (Not user code)      | (Not user code)      | float     |    0 | Read
cos(matrix1x2)                             | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
cos(matrix1x2).value                       | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
cos(matrix1x3)                             | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
cos(matrix1x3).value                       | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
cos(matrix1x4)                             | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
cos(matrix1x4).value                       | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
depivot()                                  | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
destroy()                                  | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
distance(float,float)                      | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
distance(float,float).a                    | (Not user code)      | (Not user code)      | float     |    0 | Read
distance(float,float).b                    | (Not user code)      | (Not user code)      | float     |    0 | Read
distance(matrix1x2,matrix1x2)              | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
distance(matrix1x2,matrix1x2).a            | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
distance(matrix1x2,matrix1x2).b            | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
distance(matrix1x3,matrix1x3)              | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
distance(matrix1x3,matrix1x3).a            | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
distance(matrix1x3,matrix1x3).b            | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
distance(matrix1x4,matrix1x4)              | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
distance(matrix1x4,matrix1x4).a            | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
distance(matrix1x4,matrix1x4).b            | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
faceplayer()                               | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
floor(float)                               | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
floor(float).value                         | (Not user code)      | (Not user code)      | float     |    0 | Read
floor(matrix1x2)                           | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
floor(matrix1x2).value                     | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
floor(matrix1x3)                           | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
floor(matrix1x3).value                     | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
floor(matrix1x4)                           | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
floor(matrix1x4).value                     | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
gimmick(string,float,float)                | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
gimmick(string,float,float).gimmick        | (Not user code)      | (Not user code)      | string    |    0 | Read
gimmick(string,float,float).param1         | (Not user code)      | (Not user code)      | float     |    0 | Read
gimmick(string,float,float).param2         | (Not user code)      | (Not user code)      | float     |    0 | Read
gimmick(string,float)                      | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
gimmick(string,float).gimmick              | (Not user code)      | (Not user code)      | string    |    0 | Read
gimmick(string,float).param                | (Not user code)      | (Not user code)      | float     |    0 | Read
gimmick(string)                            | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
gimmick(string).gimmick                    | (Not user code)      | (Not user code)      | string    |    0 | Read
harmsenemies                               | (Not user code)      | [n/a]                | float     |    2 | Read
harmsplayers                               | (Not user code)      | [n/a]                | float     |    2 | Read
length(float)                              | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
length(float).vector                       | (Not user code)      | (Not user code)      | float     |    0 | Read
length(matrix1x2)                          | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
length(matrix1x2).vector                   | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
length(matrix1x3)                          | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
length(matrix1x3).vector                   | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
length(matrix1x4)                          | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
length(matrix1x4).vector                   | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
loadbackground(string)                     | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
loadbackground(string).background_id       | (Not user code)      | (Not user code)      | string    |    0 | Read
mcols(matrix)                              | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
mcols(matrix).m                            | (Not user code)      | (Not user code)      | matrix    |    0 | Read
message(float)                             | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
message(float).value                       | (Not user code)      | (Not user code)      | float     |    0 | Read
mrows(matrix)                              | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
mrows(matrix).m                            | (Not user code)      | (Not user code)      | matrix    |    0 | Read
print(float)                               | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
print(float).value                         | (Not user code)      | (Not user code)      | float     |    0 | Read
print(matrix)                              | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
print(matrix).value                        | (Not user code)      | (Not user code)      | matrix    |    0 | Read
print(string)                              | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
print(string).value                        | (Not user code)      | (Not user code)      | string    |    0 | Read
rad2turns(float)                           | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
rad2turns(float).radians                   | (Not user code)      | (Not user code)      | float     |    0 | Read
rad2turns(matrix1x2)                       | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
rad2turns(matrix1x2).radians               | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
rad2turns(matrix1x3)                       | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
rad2turns(matrix1x3).radians               | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
rad2turns(matrix1x4)                       | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
rad2turns(matrix1x4).radians               | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
random(float,float)                        | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
random(float,float).lower                  | (Not user code)      | (Not user code)      | float     |    0 | Read
random(float,float).upper                  | (Not user code)      | (Not user code)      | float     |    0 | Read
random(matrix1x2,matrix1x2)                | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
random(matrix1x2,matrix1x2).lower          | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
random(matrix1x2,matrix1x2).upper          | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
random(matrix1x3,matrix1x3)                | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
random(matrix1x3,matrix1x3).lower          | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
random(matrix1x3,matrix1x3).upper          | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
random(matrix1x4,matrix1x4)                | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
random(matrix1x4,matrix1x4).lower          | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
random(matrix1x4,matrix1x4).upper          | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
rotate(float)                              | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
rotate(float).amount                       | (Not user code)      | (Not user code)      | float     |    0 | Read
round(float)                               | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
round(float).value                         | (Not user code)      | (Not user code)      | float     |    0 | Read
round(matrix1x2)                           | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
round(matrix1x2).value                     | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
round(matrix1x3)                           | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
round(matrix1x3).value                     | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
round(matrix1x4)                           | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
round(matrix1x4).value                     | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
setrotation(float)                         | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
setrotation(float).value                   | (Not user code)      | (Not user code)      | float     |    0 | Read
setspeed(float)                            | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
setspeed(float).value                      | (Not user code)      | (Not user code)      | float     |    0 | Read
sin(float)                                 | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
sin(float).value                           | (Not user code)      | (Not user code)      | float     |    0 | Read
sin(matrix1x2)                             | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
sin(matrix1x2).value                       | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
sin(matrix1x3)                             | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
sin(matrix1x3).value                       | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
sin(matrix1x4)                             | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
sin(matrix1x4).value                       | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
spawn()                                    | (Not user code)      | [n/a]                | void      |    2 | Intrinsic method
spawnposition                              | (Not user code)      | [n/a]                | matrix1x2 |    2 | Read
spawnrelative                              | (Not user code)      | [n/a]                | float     |    2 | Read
spawnrotation                              | (Not user code)      | [n/a]                | float     |    2 | Read
spawnspeed                                 | (Not user code)      | [n/a]                | float     |    2 | Read
startscript(string,float)                  | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
startscript(string,float).passed_value     | (Not user code)      | (Not user code)      | float     |    0 | Read
startscript(string,float).script_id        | (Not user code)      | (Not user code)      | string    |    0 | Read
startscript(string)                        | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
startscript(string).script_id              | (Not user code)      | (Not user code)      | string    |    0 | Read
startscriptmany(string,float)              | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
startscriptmany(string,float).passed_value | (Not user code)      | (Not user code)      | float     |    0 | Read
startscriptmany(string,float).script_id    | (Not user code)      | (Not user code)      | string    |    0 | Read
startscriptmany(string)                    | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
startscriptmany(string).script_id          | (Not user code)      | (Not user code)      | string    |    0 | Read
tan(float)                                 | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
tan(float).value                           | (Not user code)      | (Not user code)      | float     |    0 | Read
tan(matrix1x2)                             | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
tan(matrix1x2).value                       | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
tan(matrix1x3)                             | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
tan(matrix1x3).value                       | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
tan(matrix1x4)                             | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
tan(matrix1x4).value                       | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
turns2rad(float)                           | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
turns2rad(float).turns                     | (Not user code)      | (Not user code)      | float     |    0 | Read
turns2rad(matrix1x2)                       | (Not user code)      | [n/a]                | matrix1x2 |    0 | Intrinsic method
turns2rad(matrix1x2).turns                 | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
turns2rad(matrix1x3)                       | (Not user code)      | [n/a]                | matrix1x3 |    0 | Intrinsic method
turns2rad(matrix1x3).turns                 | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
turns2rad(matrix1x4)                       | (Not user code)      | [n/a]                | matrix1x4 |    0 | Intrinsic method
turns2rad(matrix1x4).turns                 | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
turnstoplayer()                            | (Not user code)      | [n/a]                | float     |    0 | Intrinsic method
usepivot                                   | (Not user code)      | [n/a]                | float     |    2 | Read
wait(float)                                | (Not user code)      | [n/a]                | void      |    0 | Intrinsic method
wait(float).seconds                        | (Not user code)      | (Not user code)      | float     |    0 | Read
", includeIntrinsics: true);

        [TestMethod]
        public void ErrorTestTypes1() => TestFail(@"
float x;
string x;
", "BS0047", 2);

        [TestMethod]
        public void ErrorTestTypes2() => TestFail(@"
float x;
x = ""hi"";
", "BS0049", 2);

        [TestMethod]
        public void ErrorTestTypes3() => TestFail(@"
matrix x = [[1 2] 3];
", "BS0050", 1);

        [TestMethod]
        public void ErrorTestTypes4() => TestFail(@"
matrix x = [[1 2] : 3];
", "BS0051", 1);

        [TestMethod]
        public void ErrorTestTypes5() => TestFail(@"
matrix x = [1];
x++;
", "BS0052", 2);

        [TestMethod]
        public void ErrorTestTypes6() => TestFail(@"
string s = ""hi"";
s = -s;
", "BS0053", 2);

        [TestMethod]
        public void ErrorTestTypes7() => TestFail(@"
string s = ""hi"";
float f = s[0];
", "BS0054", 2);

        [TestMethod]
        public void ErrorTestTypes8() => TestFail(@"
matrix a = [1 2];
matrix b = [1 2 3; 4 5 6; 7 8 9; 10 11 12];
matrix c = a * b;
", "BS0055", 3);

        [TestMethod]
        public void ErrorTestTypes9() => TestFail(@"
float a = 3;
string b = ""hi"";
float c = a * b;
", "BS0056", 3);

        [TestMethod]
        public void ErrorTestConditionTypes1() => TestFail(@"
for (; ""hi"";) {}
", "BS0060", 1);

        [TestMethod]
        public void ErrorTestConditionTypes2() => TestFail(@"
repeat ([1 2 3]) {}
", "BS0061", 1);

        [TestMethod]
        public void ErrorTestMethodTypes1() => TestFail(@"
function matrix identity() {
    return [1 0; 0 1];
}
", "BS0057", 1);

        [TestMethod]
        public void ErrorTestMethodTypes2() => TestFail(@"
function matrix2x2 identity(float i) {
    if (i == 2) {
        return [1 0; 0 1];
    }
    return [1 0 0; 0 1 0; 0 0 1];
}
", "BS0062", 5);

        [TestMethod]
        public void ErrorTestMethodTypes3() => TestFail(@"
function matrix2x2 identity(float i) {
    if (i == 2) {
        return [1 0; 0 1];
    }
    return;
}
", "BS0062", 5);

        [TestMethod]
        public void ErrorTestMethodTypes4() => TestFail(@"
function void identity(float i) {
    return [1 0 0; 0 1 0; 0 0 1];
}
", "BS0063", 2);

        [TestMethod]
        public void ErrorTestIlldefined1() => TestFail(@"
matrix a;
matrix b = a + [1 0];
", "BS0058", 2);

        [TestMethod]
        public void ErrorTestIlldefined1point5() => TestFail(@"
matrix b = a + [1 0];
", "BS0058", 1);

        [TestMethod]
        public void ErrorTestIlldefined2() => TestFail(@"
my_unknown_method(230);
", "BS0059", 1);

        [TestMethod]
        public void ErrorTestsSpecialMethod1() => TestFail(@"
function float main(float value) {
    return 3;
}
", "BS0064", 1);

        [TestMethod]
        public void ErrorTestsSpecialMethod2() => TestFail(@"
function void main(float value, float value2) {
    return;
}
", "BS0064", 1);

        [TestMethod]
        public void ErrorTestsSpecialMethod3() => TestFail(@"
function void main() {
    return;
}
", "BS0064", 1);

        [TestMethod]
        public void ErrorTestsSpecialMethod4() => TestFail(@"
function void on_message() { }
", "BS0065", 1);

        [TestMethod]
        public void ErrorTestsSpecialMethod5() => TestFail(@"
function void on_health<0.5>(float value) { }
", "BS0066", 1);

        [TestMethod]
        public void ErrorTestsSpecialMethod6() => TestFail(@"
function float on_time<0.5>() { return 3; }
", "BS0067", 1);

        [TestMethod]
        public void ErrorTestsSpecialMethod7() => TestFail(@"
function void on_health() {}
", "BS0068", 1);

        [TestMethod]
        public void ErrorTestsSpecialMethod8() => TestFail(@"
function void on_time() {}
", "BS0069", 1);

        // These chained method tests don't particularly care about location
        // But the system requires it anyway...
        [TestMethod]
        public void ErrorTestsRecursion1() => TestFail(@"
function void A1() { A1(); }
", "BS0070", 1);

        [TestMethod]
        public void ErrorTestsRecursion2() => TestFail(@"
function void A1() { A2(); }
function void A2() { A1(); }
", "BS0070", 1);

        [TestMethod]
        public void ErrorTestsRecursion3() => TestFail(@"
function void A1() { A2(); }
function void A2() { A3(); }
function void A3() { A4(); }
function void A4() { A1(); }
", "BS0070", 1);

        [TestMethod]
        public void ErrorTestsRecursion4() => TestFail(@"
function void A() { A3(); }
function void A1() { A2(); }
function void A2() { A3(); }
function void A3() { A4(); }
function void A4() { A1(); }
", "BS0070", 4);

        [TestMethod]
        public void ErrorTestIllegalWait() => TestFail(@"
function void on_message(float value) { A1(); }
function void A1() { wait(1); }
", "BS0071", 2);

        [TestMethod]
        public void ErrorNotAllBranchesReturn() => TestFail(@"
function float my_method(float value) {
    if (value == 3) {
        if (value == 3) {
            return 3;
        }
    } else {
        if (value == 3) {
            return 3;
        } else {
            return 3;
        }
    }
}
", "BS0072", 1);

        [TestMethod]
        public void WarningUnreachableCode() => TestFail(@"
function float my_method(float value) {
    if (value == 3) {
        if (value == 3) {
            return 3;
        } else {
            return 3;
            value = 4;
        }
    } else {
        if (value == 3) {
            return 3;
        } else {
            return 3;
        }
    }
}
", "BS0073", 7);

    }
}