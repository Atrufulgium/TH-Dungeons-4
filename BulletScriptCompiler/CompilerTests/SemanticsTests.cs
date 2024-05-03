﻿using Atrufulgium.BulletScript.Compiler.Parsing;
using Atrufulgium.BulletScript.Compiler.Semantics;
using CompilerTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Tests {

    [TestClass]
    public class SemanticsTests {

        static void TestTable(string code, string symbolTable, bool includeIntrinsics = false) {
            TestHelpers.AssertTrimmedStringsEqual(symbolTable, Compile(code, includeIntrinsics));
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
abs(float)                                 | (Not user code)      | [n/a]                | float     |    0 | 
abs(float).value                           | (Not user code)      | (Not user code)      | float     |    0 | Read
abs(matrix1x2)                             | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
abs(matrix1x2).value                       | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
abs(matrix1x3)                             | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
abs(matrix1x3).value                       | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
abs(matrix1x4)                             | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
abs(matrix1x4).value                       | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
acos(float)                                | (Not user code)      | [n/a]                | float     |    0 | 
acos(float).value                          | (Not user code)      | (Not user code)      | float     |    0 | Read
acos(matrix1x2)                            | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
acos(matrix1x2).value                      | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
acos(matrix1x3)                            | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
acos(matrix1x3).value                      | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
acos(matrix1x4)                            | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
acos(matrix1x4).value                      | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
addscript()                                | (Not user code)      | [n/a]                | void      |    0 | 
addscript(string,float)                    | (Not user code)      | [n/a]                | void      |    0 | 
addscript(string,float).passed_value       | (Not user code)      | (Not user code)      | float     |    0 | Read
addscript(string,float).script_id          | (Not user code)      | (Not user code)      | string    |    0 | Read
addscript(string)                          | (Not user code)      | [n/a]                | void      |    0 | 
addscript(string).script_id                | (Not user code)      | (Not user code)      | string    |    0 | Read
addspeed(float)                            | (Not user code)      | [n/a]                | void      |    0 | 
addspeed(float).amount                     | (Not user code)      | (Not user code)      | float     |    0 | Read
angletoplayer()                            | (Not user code)      | [n/a]                | float     |    0 | 
asin(float)                                | (Not user code)      | [n/a]                | float     |    0 | 
asin(float).value                          | (Not user code)      | (Not user code)      | float     |    0 | Read
asin(matrix1x2)                            | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
asin(matrix1x2).value                      | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
asin(matrix1x3)                            | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
asin(matrix1x3).value                      | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
asin(matrix1x4)                            | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
asin(matrix1x4).value                      | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
atan(float)                                | (Not user code)      | [n/a]                | float     |    0 | 
atan(float).value                          | (Not user code)      | (Not user code)      | float     |    0 | Read
atan(matrix1x2)                            | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
atan(matrix1x2).value                      | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
atan(matrix1x3)                            | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
atan(matrix1x3).value                      | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
atan(matrix1x4)                            | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
atan(matrix1x4).value                      | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
atan2(float,float)                         | (Not user code)      | [n/a]                | float     |    0 | 
atan2(float,float).x                       | (Not user code)      | (Not user code)      | float     |    0 | Read
atan2(float,float).y                       | (Not user code)      | (Not user code)      | float     |    0 | Read
atan2(matrix1x2,matrix1x2)                 | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
atan2(matrix1x2,matrix1x2).x               | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
atan2(matrix1x2,matrix1x2).y               | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
atan2(matrix1x3,matrix1x3)                 | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
atan2(matrix1x3,matrix1x3).x               | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
atan2(matrix1x3,matrix1x3).y               | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
atan2(matrix1x4,matrix1x4)                 | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
atan2(matrix1x4,matrix1x4).x               | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
atan2(matrix1x4,matrix1x4).y               | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
autoclear                                  | (Not user code)      | [n/a]                | float     |    2 | Read
bullettype                                 | (Not user code)      | [n/a]                | string    |    2 | Read
ceil(float)                                | (Not user code)      | [n/a]                | float     |    0 | 
ceil(float).value                          | (Not user code)      | (Not user code)      | float     |    0 | Read
ceil(matrix1x2)                            | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
ceil(matrix1x2).value                      | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
ceil(matrix1x3)                            | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
ceil(matrix1x3).value                      | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
ceil(matrix1x4)                            | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
ceil(matrix1x4).value                      | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
clearimmune                                | (Not user code)      | [n/a]                | float     |    2 | Read
clearingtype                               | (Not user code)      | [n/a]                | float     |    2 | Read
cos(float)                                 | (Not user code)      | [n/a]                | float     |    0 | 
cos(float).value                           | (Not user code)      | (Not user code)      | float     |    0 | Read
cos(matrix1x2)                             | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
cos(matrix1x2).value                       | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
cos(matrix1x3)                             | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
cos(matrix1x3).value                       | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
cos(matrix1x4)                             | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
cos(matrix1x4).value                       | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
depivot()                                  | (Not user code)      | [n/a]                | void      |    0 | 
destroy()                                  | (Not user code)      | [n/a]                | void      |    0 | 
distance(float,float)                      | (Not user code)      | [n/a]                | float     |    0 | 
distance(float,float).a                    | (Not user code)      | (Not user code)      | float     |    0 | Read
distance(float,float).b                    | (Not user code)      | (Not user code)      | float     |    0 | Read
distance(matrix1x2,matrix1x2)              | (Not user code)      | [n/a]                | float     |    0 | 
distance(matrix1x2,matrix1x2).a            | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
distance(matrix1x2,matrix1x2).b            | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
distance(matrix1x3,matrix1x3)              | (Not user code)      | [n/a]                | float     |    0 | 
distance(matrix1x3,matrix1x3).a            | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
distance(matrix1x3,matrix1x3).b            | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
distance(matrix1x4,matrix1x4)              | (Not user code)      | [n/a]                | float     |    0 | 
distance(matrix1x4,matrix1x4).a            | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
distance(matrix1x4,matrix1x4).b            | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
faceplayer()                               | (Not user code)      | [n/a]                | void      |    0 | 
floor(float)                               | (Not user code)      | [n/a]                | float     |    0 | 
floor(float).value                         | (Not user code)      | (Not user code)      | float     |    0 | Read
floor(matrix1x2)                           | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
floor(matrix1x2).value                     | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
floor(matrix1x3)                           | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
floor(matrix1x3).value                     | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
floor(matrix1x4)                           | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
floor(matrix1x4).value                     | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
gimmick()                                  | (Not user code)      | [n/a]                | void      |    0 | 
gimmick(float,float)                       | (Not user code)      | [n/a]                | void      |    0 | 
gimmick(float,float).param1                | (Not user code)      | (Not user code)      | float     |    0 | Read
gimmick(float,float).param2                | (Not user code)      | (Not user code)      | float     |    0 | Read
gimmick(float)                             | (Not user code)      | [n/a]                | void      |    0 | 
gimmick(float).param1                      | (Not user code)      | (Not user code)      | float     |    0 | Read
harmsenemies                               | (Not user code)      | [n/a]                | float     |    2 | Read
harmsplayers                               | (Not user code)      | [n/a]                | float     |    2 | Read
length(float)                              | (Not user code)      | [n/a]                | float     |    0 | 
length(float).vector                       | (Not user code)      | (Not user code)      | float     |    0 | Read
length(matrix1x2)                          | (Not user code)      | [n/a]                | float     |    0 | 
length(matrix1x2).vector                   | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
length(matrix1x3)                          | (Not user code)      | [n/a]                | float     |    0 | 
length(matrix1x3).vector                   | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
length(matrix1x4)                          | (Not user code)      | [n/a]                | float     |    0 | 
length(matrix1x4).vector                   | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
loadbackground(string)                     | (Not user code)      | [n/a]                | void      |    0 | 
loadbackground(string).background_id       | (Not user code)      | (Not user code)      | string    |    0 | Read
message(float)                             | (Not user code)      | [n/a]                | void      |    0 | 
message(float).value                       | (Not user code)      | (Not user code)      | float     |    0 | Read
print(float)                               | (Not user code)      | [n/a]                | void      |    0 | 
print(float).value                         | (Not user code)      | (Not user code)      | float     |    0 | Read
print(string)                              | (Not user code)      | [n/a]                | void      |    0 | 
print(string).value                        | (Not user code)      | (Not user code)      | string    |    0 | Read
random(float,float)                        | (Not user code)      | [n/a]                | float     |    0 | 
random(float,float).lower                  | (Not user code)      | (Not user code)      | float     |    0 | Read
random(float,float).upper                  | (Not user code)      | (Not user code)      | float     |    0 | Read
random(matrix1x2,matrix1x2)                | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
random(matrix1x2,matrix1x2).lower          | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
random(matrix1x2,matrix1x2).upper          | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
random(matrix1x3,matrix1x3)                | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
random(matrix1x3,matrix1x3).lower          | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
random(matrix1x3,matrix1x3).upper          | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
random(matrix1x4,matrix1x4)                | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
random(matrix1x4,matrix1x4).lower          | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
random(matrix1x4,matrix1x4).upper          | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
rotate(float)                              | (Not user code)      | [n/a]                | void      |    0 | 
rotate(float).amount                       | (Not user code)      | (Not user code)      | float     |    0 | Read
round(float)                               | (Not user code)      | [n/a]                | float     |    0 | 
round(float).value                         | (Not user code)      | (Not user code)      | float     |    0 | Read
round(matrix1x2)                           | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
round(matrix1x2).value                     | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
round(matrix1x3)                           | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
round(matrix1x3).value                     | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
round(matrix1x4)                           | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
round(matrix1x4).value                     | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
setrotation(float)                         | (Not user code)      | [n/a]                | void      |    0 | 
setrotation(float).value                   | (Not user code)      | (Not user code)      | float     |    0 | Read
setspeed(float)                            | (Not user code)      | [n/a]                | void      |    0 | 
setspeed(float).value                      | (Not user code)      | (Not user code)      | float     |    0 | Read
sin(float)                                 | (Not user code)      | [n/a]                | float     |    0 | 
sin(float).value                           | (Not user code)      | (Not user code)      | float     |    0 | Read
sin(matrix1x2)                             | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
sin(matrix1x2).value                       | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
sin(matrix1x3)                             | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
sin(matrix1x3).value                       | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
sin(matrix1x4)                             | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
sin(matrix1x4).value                       | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
spawn()                                    | (Not user code)      | [n/a]                | void      |    2 | 
spawnposition                              | (Not user code)      | [n/a]                | matrix1x2 |    2 | Read
spawnrelative                              | (Not user code)      | [n/a]                | float     |    2 | Read
spawnrotation                              | (Not user code)      | [n/a]                | float     |    2 | Read
spawnspeed                                 | (Not user code)      | [n/a]                | float     |    2 | Read
startscript(string,float)                  | (Not user code)      | [n/a]                | void      |    0 | 
startscript(string,float).passed_value     | (Not user code)      | (Not user code)      | float     |    0 | Read
startscript(string,float).script_id        | (Not user code)      | (Not user code)      | string    |    0 | Read
startscript(string)                        | (Not user code)      | [n/a]                | void      |    0 | 
startscript(string).script_id              | (Not user code)      | (Not user code)      | string    |    0 | Read
startscriptmany(string,float)              | (Not user code)      | [n/a]                | void      |    0 | 
startscriptmany(string,float).passed_value | (Not user code)      | (Not user code)      | float     |    0 | Read
startscriptmany(string,float).script_id    | (Not user code)      | (Not user code)      | string    |    0 | Read
startscriptmany(string)                    | (Not user code)      | [n/a]                | void      |    0 | 
startscriptmany(string).script_id          | (Not user code)      | (Not user code)      | string    |    0 | Read
tan(float)                                 | (Not user code)      | [n/a]                | float     |    0 | 
tan(float).value                           | (Not user code)      | (Not user code)      | float     |    0 | Read
tan(matrix1x2)                             | (Not user code)      | [n/a]                | matrix1x2 |    0 | 
tan(matrix1x2).value                       | (Not user code)      | (Not user code)      | matrix1x2 |    0 | Read
tan(matrix1x3)                             | (Not user code)      | [n/a]                | matrix1x3 |    0 | 
tan(matrix1x3).value                       | (Not user code)      | (Not user code)      | matrix1x3 |    0 | Read
tan(matrix1x4)                             | (Not user code)      | [n/a]                | matrix1x4 |    0 | 
tan(matrix1x4).value                       | (Not user code)      | (Not user code)      | matrix1x4 |    0 | Read
usepivot                                   | (Not user code)      | [n/a]                | float     |    2 | Read
wait(float)                                | (Not user code)      | [n/a]                | void      |    0 | 
wait(float).seconds                        | (Not user code)      | (Not user code)      | float     |    0 | Read
", includeIntrinsics: true);
    }
}