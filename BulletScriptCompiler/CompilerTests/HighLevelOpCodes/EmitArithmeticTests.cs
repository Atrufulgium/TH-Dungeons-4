using static Atrufulgium.BulletScript.Compiler.Tests.Helpers.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.HighLevelOpCodes.Tests {

    [TestClass]
    public class EmitArithmeticTests {
        // Note: indexing is not yet supported and as such, has no tests

        [TestMethod]
        public void EmitBasicArithmeticBinaryCommutative() => TestEmittedOpcodes(@"
float a;
a = a + a;
a = 1 + a;
a = a + 1;
a = 1 + 2;
a = a * a;
a = 1 * a;
a = a * 1;
a = 1 * 2;
a = a & a;
a = a & 1;
a = 1 & a;
a = 1 & 1;
a = a | a;
a = a | 1;
a = 1 | a;
a = 1 | 1;
a = a == a;
a = a == 1;
a = 1 == a;
a = 1 == 1;
a = a != a;
a = a != 1;
a = 1 != a;
a = 1 != 1;
", @"
[op]             Add | [f]                a | [f]                a | [f]                a
[op]             Add | [f]                a | [f]                1 | [f]                a
[op]             Add | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                3 | --------------------
[op]             Mul | [f]                a | [f]                a | [f]                a
[op]             Mul | [f]                a | [f]                1 | [f]                a
[op]             Mul | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                2 | --------------------
[op]             Mul | [f]                a | [f]                a | [f]                a
[op]             Mul | [f]                a | [f]                1 | [f]                a
[op]             Mul | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                1 | --------------------
[op]             Add | [f]                a | [f]                a | [f]                a
[op]             Add | [f]                a | [f]                1 | [f]                a
[op]             Add | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                1 | --------------------
[op]           Equal | [f]                a | [f]                a | [f]                a
[op]           Equal | [f]                a | [f]                1 | [f]                a
[op]           Equal | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                1 | --------------------
[op]           Equal | [f]                a | [f]                a | [f]                a
[op]             Not | [f]                a | [f]                a | --------------------
[op]           Equal | [f]                a | [f]                1 | [f]                a
[op]             Not | [f]                a | [f]                a | --------------------
[op]           Equal | [f]                a | [f]                1 | [f]                a
[op]             Not | [f]                a | [f]                a | --------------------
[op]             Set | [f]                a | [f]                0 | --------------------
");

        [TestMethod]
        public void EmitBasicArithmeticBinaryNonCommutative() => TestEmittedOpcodes(@"
float a;
a = a - a;
a = a - 1;
a = 1 - a;
a = 1 - 1;
a = a / a;
a = a / 1;
a = 1 / a;
a = 1 / 1;
a = a % a;
a = a % 1;
a = 1 % a;
a = 1 % 1;
a = a^a;
a = a^1;
a = 1^a;
a = 1^1;
a = a^2;
a = a < a;
a = a < 1;
a = 1 < a;
a = 1 < 1;
a = a <= a;
a = a <= 1;
a = 1 <= a;
a = 1 <= 1;
a = a > a;
a = a > 1;
a = 1 > a;
a = 1 > 1;
a = a >= a;
a = a >= 1;
a = 1 >= a;
a = 1 >= 1;
", @"
[op]             Sub | [f]                a | [f]                a | [f]                a
[op]             Sub | [f]                a | [f]                a | [f]                1
[op]             Sub | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                0 | --------------------
[op]             Div | [f]                a | [f]                a | [f]                a
[op]             Div | [f]                a | [f]                a | [f]                1
[op]             Div | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                1 | --------------------
[op]             Mod | [f]                a | [f]                a | [f]                a
[op]             Mod | [f]                a | [f]                a | [f]                1
[op]             Mod | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                0 | --------------------
[op]             Pow | [f]                a | [f]                a | [f]                a
[op]             Pow | [f]                a | [f]                a | [f]                1
[op]             Pow | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                1 | --------------------
[op]          Square | [f]                a | [f]                a | --------------------
[op]        LessThan | [f]                a | [f]                a | [f]                a
[op]        LessThan | [f]                a | [f]                a | [f]                1
[op]        LessThan | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                0 | --------------------
[op]   LessThanEqual | [f]                a | [f]                a | [f]                a
[op]   LessThanEqual | [f]                a | [f]                a | [f]                1
[op]   LessThanEqual | [f]                a | [f]                1 | [f]                a
[op]             Set | [f]                a | [f]                1 | --------------------
[op]        LessThan | [f]                a | [f]                a | [f]                a
[op]        LessThan | [f]                a | [f]                1 | [f]                a
[op]        LessThan | [f]                a | [f]                a | [f]                1
[op]             Set | [f]                a | [f]                0 | --------------------
[op]   LessThanEqual | [f]                a | [f]                a | [f]                a
[op]   LessThanEqual | [f]                a | [f]                1 | [f]                a
[op]   LessThanEqual | [f]                a | [f]                a | [f]                1
[op]             Set | [f]                a | [f]                1 | --------------------
");

        [TestMethod]
        public void EmitBasicArithmeticUnary() => TestEmittedOpcodes(@"
float a = 0;
a = !a;
a = !1;
a = !0;
a = -a;
a = -1;
", @"
[op]             Set | [f]                a | [f]                0 | --------------------
[op]             Not | [f]                a | [f]                a | --------------------
[op]             Set | [f]                a | [f]                0 | --------------------
[op]             Set | [f]                a | [f]                1 | --------------------
[op]             Sub | [f]                a | [f]                0 | [f]                a
[op]             Set | [f]                a | [f]               -1 | --------------------
");

        [TestMethod]
        public void EmitFunkyArtihmetic() => TestEmittedOpcodes(@"
float a = 3;
float b = 4;
a = sin(a + b * a ^ sin(-b+5));
", @"
[op]             Set | [f]                a | [f]                3 | --------------------
[op]             Set | [f]                b | [f]                4 | --------------------
[op]             Sub | [f] ari...lt#float#0 | [f]                0 | [f]                b
[op]             Add | [f] ari...lt#float#1 | [f]                5 | [f] ari...lt#float#0
[op]             Sin | [f] ari...lt#float#2 | [f] ari...lt#float#1 | --------------------
[op]             Pow | [f] ari...lt#float#3 | [f]                a | [f] ari...lt#float#2
[op]             Mul | [f] ari...lt#float#4 | [f]                b | [f] ari...lt#float#3
[op]             Add | [f] ari...lt#float#5 | [f]                a | [f] ari...lt#float#4
[op]             Sin | [f]                a | [f] ari...lt#float#5 | --------------------
");

        [TestMethod]
        public void EmitAssignIntrinsicVariable() => TestEmittedOpcodes(@"
autoclear = !autoclear;
", @"
[op]             Not | [f]        autoclear | [f]        autoclear | --------------------
");

        [TestMethod]
        public void EmitString() => TestEmittedOpcodes(@"
string s = ""hi"";
string t = s;
float a = s == t;
a = s != t;
a = s == ""hi"";
a = s != ""hi"";
a = ""hi"" == s;
a = ""hi"" != s;
a = ""hi"" == ""hi"";
a = ""hi"" != ""hi"";
loadbackground(s);
", @"
[op]       SetString | [s]                s | [s]             ""hi"" | --------------------
[op]       SetString | [s]                t | [s]                s | --------------------
[op]     EqualString | [f]                a | [s]                s | [s]                t
[op]     EqualString | [f]                a | [s]                s | [s]                t
[op]             Not | [f]                a | [f]                a | --------------------
[op]     EqualString | [f]                a | [s]                s | [s]             ""hi""
[op]     EqualString | [f]                a | [s]                s | [s]             ""hi""
[op]             Not | [f]                a | [f]                a | --------------------
[op]     EqualString | [f]                a | [s]             ""hi"" | [s]                s
[op]     EqualString | [f]                a | [s]             ""hi"" | [s]                s
[op]             Not | [f]                a | [f]                a | --------------------
[op]             Set | [f]                a | [f]                1 | --------------------
[op]             Set | [f]                a | [f]                0 | --------------------
[op]  LoadBackground | [s]                s | -------------------- | --------------------
");

        [TestMethod]
        public void EmitMatrixArithmeticUnary1() => TestEmittedOpcodes(@"
matrix1x3 m = [1 2 3];
m = -m;
m = !m;
", @"
[op]             Set | [f]              m+0 | [f]                1 | --------------------
[op]             Set | [f]              m+1 | [f]                2 | --------------------
[op]             Set | [f]              m+2 | [f]                3 | --------------------
[op]         Negate4 | [f]                m | [f]                m | --------------------
[op]            Not4 | [f]                m | [f]                m | --------------------
");

        // Remember: 2x2 is a special case
        [TestMethod]
        public void EmitMatrixArithmeticUnary2() => TestEmittedOpcodes(@"
matrix2x2 m = [1 2; 3 4];
m = -m;
m = !m;
", @"
[op]             Set | [f]              m+0 | [f]                1 | --------------------
[op]             Set | [f]              m+1 | [f]                2 | --------------------
[op]             Set | [f]              m+2 | [f]                3 | --------------------
[op]             Set | [f]              m+3 | [f]                4 | --------------------
[op]         Negate4 | [f]                m | [f]                m | --------------------
[op]            Not4 | [f]                m | [f]                m | --------------------
");

        [TestMethod]
        public void EmitMatrixArithmeticUnary3() => TestEmittedOpcodes(@"
matrix3x2 m = [1 2; 3 4; 5 6];
m = -m;
m = !m;
", @"
[op]             Set | [f]              m+0 | [f]                1 | --------------------
[op]             Set | [f]              m+1 | [f]                2 | --------------------
[op]             Set | [f]              m+4 | [f]                3 | --------------------
[op]             Set | [f]              m+5 | [f]                4 | --------------------
[op]             Set | [f]              m+8 | [f]                5 | --------------------
[op]             Set | [f]              m+9 | [f]                6 | --------------------
[op]         Negate4 | [f]                m | [f]                m | --------------------
[op]         Negate4 | [f]              m+4 | [f]              m+4 | --------------------
[op]         Negate4 | [f]              m+8 | [f]              m+8 | --------------------
[op]            Not4 | [f]                m | [f]                m | --------------------
[op]            Not4 | [f]              m+4 | [f]              m+4 | --------------------
[op]            Not4 | [f]              m+8 | [f]              m+8 | --------------------
");

        // TODO: matrices aren't supported as a module yet.
        // stop being lazy and implement scalar multiplication already.
        [TestMethod]
        public void EmitMatrixArithmeticBinary() => TestEmittedOpcodes(@"
matrix2x3 m1 = [1 2 3; 4 5 6];
matrix2x3 m2 = m1;
m1 = m1 + m2;
m1 = m1 - m2;
m1 = m1 * m2;
m1 = m1 / m2;
m1 = m1 % m2;
m1 = m1^m2;
m1 = m1&m2;
m1 = m1|m2;
m1 = m1 == m2;
m1 = m1 != m2;
m1 = m1 < m2;
", @"
[op]             Set | [f]             m1+0 | [f]                1 | --------------------
[op]             Set | [f]             m1+1 | [f]                2 | --------------------
[op]             Set | [f]             m1+2 | [f]                3 | --------------------
[op]             Set | [f]             m1+4 | [f]                4 | --------------------
[op]             Set | [f]             m1+5 | [f]                5 | --------------------
[op]             Set | [f]             m1+6 | [f]                6 | --------------------
[op]            Set4 | [f]               m2 | [f]               m1 | --------------------
[op]            Set4 | [f]             m2+4 | [f]             m1+4 | --------------------
[op]            Add4 | [f]               m1 | [f]               m1 | [f]               m2
[op]            Add4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
[op]            Sub4 | [f]               m1 | [f]               m1 | [f]               m2
[op]            Sub4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
[op]            Mul4 | [f]               m1 | [f]               m1 | [f]               m2
[op]            Mul4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
[op]            Div4 | [f]               m1 | [f]               m1 | [f]               m2
[op]            Div4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
[op]            Mod4 | [f]               m1 | [f]               m1 | [f]               m2
[op]            Mod4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
[op]            Pow4 | [f]               m1 | [f]               m1 | [f]               m2
[op]            Pow4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
[op]            Mul4 | [f]               m1 | [f]               m1 | [f]               m2
[op]            Mul4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
[op]            Add4 | [f]               m1 | [f]               m1 | [f]               m2
[op]            Add4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
[op]          Equal4 | [f]               m1 | [f]               m1 | [f]               m2
[op]          Equal4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
[op]          Equal4 | [f]               m1 | [f]               m1 | [f]               m2
[op]          Equal4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
[op]            Not4 | [f]               m1 | [f]               m1 | --------------------
[op]            Not4 | [f]             m1+4 | [f]             m1+4 | --------------------
[op]       LessThan4 | [f]               m1 | [f]               m1 | [f]               m2
[op]       LessThan4 | [f]             m1+4 | [f]             m1+4 | [f]             m2+4
");

        // TODO: matrix associativity is actually interesting here as we may be
        // able to choose a path with matrices with fewer rows.
        [TestMethod]
        public void EmitMatrixMultiplication() => TestEmittedOpcodes(@"
matrix2x1 m = ([1 2 3; 4 5 6] * [1 2; 3 4; 5 6]) * [1 2];
matrix2x1 m = [1 2 3; 4 5 6] * ([1 2; 3 4; 5 6] * [1 2]);
", @"
[op]             Set | [f] ari...rix2x3#0+0 | [f]                1 | --------------------
[op]             Set | [f] ari...rix2x3#0+1 | [f]                2 | --------------------
[op]             Set | [f] ari...rix2x3#0+2 | [f]                3 | --------------------
[op]             Set | [f] ari...rix2x3#0+4 | [f]                4 | --------------------
[op]             Set | [f] ari...rix2x3#0+5 | [f]                5 | --------------------
[op]             Set | [f] ari...rix2x3#0+6 | [f]                6 | --------------------
[op]             Set | [f] ari...rix3x2#0+0 | [f]                1 | --------------------
[op]             Set | [f] ari...rix3x2#0+1 | [f]                2 | --------------------
[op]             Set | [f] ari...rix3x2#0+4 | [f]                3 | --------------------
[op]             Set | [f] ari...rix3x2#0+5 | [f]                4 | --------------------
[op]             Set | [f] ari...rix3x2#0+8 | [f]                5 | --------------------
[op]             Set | [f] ari...rix3x2#0+9 | [f]                6 | --------------------
[op]    MatrixMul232 | [f] ari...atrix2x2#0 | [f] ari...atrix2x3#0 | [f] ari...atrix3x2#0
[op]             Set | [f] ari...rix1x2#0+0 | [f]                1 | --------------------
[op]             Set | [f] ari...rix1x2#0+1 | [f]                2 | --------------------
[op]    MatrixMul221 | [f]                m | [f] ari...atrix2x2#0 | [f] ari...atrix1x2#0
[op]             Set | [f] ari...rix2x3#0+0 | [f]                1 | --------------------
[op]             Set | [f] ari...rix2x3#0+1 | [f]                2 | --------------------
[op]             Set | [f] ari...rix2x3#0+2 | [f]                3 | --------------------
[op]             Set | [f] ari...rix2x3#0+4 | [f]                4 | --------------------
[op]             Set | [f] ari...rix2x3#0+5 | [f]                5 | --------------------
[op]             Set | [f] ari...rix2x3#0+6 | [f]                6 | --------------------
[op]             Set | [f] ari...rix3x2#0+0 | [f]                1 | --------------------
[op]             Set | [f] ari...rix3x2#0+1 | [f]                2 | --------------------
[op]             Set | [f] ari...rix3x2#0+4 | [f]                3 | --------------------
[op]             Set | [f] ari...rix3x2#0+5 | [f]                4 | --------------------
[op]             Set | [f] ari...rix3x2#0+8 | [f]                5 | --------------------
[op]             Set | [f] ari...rix3x2#0+9 | [f]                6 | --------------------
[op]             Set | [f] ari...rix1x2#0+0 | [f]                1 | --------------------
[op]             Set | [f] ari...rix1x2#0+1 | [f]                2 | --------------------
[op]    MatrixMul321 | [f] ari...atrix1x3#0 | [f] ari...atrix3x2#0 | [f] ari...atrix1x2#0
[op]    MatrixMul231 | [f]                m | [f] ari...atrix2x3#0 | [f] ari...atrix1x3#0
");

        [TestMethod]
        public void EmitIndexReads1() => TestEmittedOpcodes(@"
matrix2x1 m = [1 2];
float a = m[0];
a = m[a];
", @"
[op]             Set | [f]              m+0 | [f]                1 | --------------------
[op]             Set | [f]              m+1 | [f]                2 | --------------------
[op]      IndexedGet | [f]                a | [f]                m | [f]                0
[op]      IndexedGet | [f]                a | [f]                m | [f]                a
");

        [TestMethod]
        public void EmitIndexReads2() => TestEmittedOpcodes(@"
matrix2x2 m = [1 2; 3 4];
float a = m[0 0];
a = m[0 a];
", @"
[op]             Set | [f]              m+0 | [f]                1 | --------------------
[op]             Set | [f]              m+1 | [f]                2 | --------------------
[op]             Set | [f]              m+2 | [f]                3 | --------------------
[op]             Set | [f]              m+3 | [f]                4 | --------------------
[op]      IndexedGet | [f]                a | [f]                m | [f]                0
[op]             Add | [f] ari...lt#float#0 | [f]                0 | [f]                a
[op]      IndexedGet | [f]                a | [f]                m | [f] ari...lt#float#0
");

        [TestMethod]
        public void EmitIndexReads3() => TestEmittedOpcodes(@"
matrix3x2 m = [1 2; 3 4; 5 6];
float a = m[0 0];
a = m[0 a];
", @"
[op]             Set | [f]              m+0 | [f]                1 | --------------------
[op]             Set | [f]              m+1 | [f]                2 | --------------------
[op]             Set | [f]              m+4 | [f]                3 | --------------------
[op]             Set | [f]              m+5 | [f]                4 | --------------------
[op]             Set | [f]              m+8 | [f]                5 | --------------------
[op]             Set | [f]              m+9 | [f]                6 | --------------------
[op]      IndexedGet | [f]                a | [f]                m | [f]                0
[op]             Add | [f] ari...lt#float#0 | [f]                0 | [f]                a
[op]      IndexedGet | [f]                a | [f]                m | [f] ari...lt#float#0
");

        [TestMethod]
        public void EmitIndexReads4() => TestEmittedOpcodes(@"
matrix2x2 m = [1 2; 3 4];
float a = m[0];
a = m[a];
", @"
[op]             Set | [f]              m+0 | [f]                1 | --------------------
[op]             Set | [f]              m+1 | [f]                2 | --------------------
[op]             Set | [f]              m+2 | [f]                3 | --------------------
[op]             Set | [f]              m+3 | [f]                4 | --------------------
[op]      IndexedGet | [f]                a | [f]                m | [f]                0
[op]             Div | [f] ari...lt#float#0 | [f]                a | [f]                2
[op]           Floor | [f] ari...lt#float#1 | [f] ari...lt#float#0 | --------------------
[op]             Mul | [f] ari...lt#float#2 | [f]                2 | [f] ari...lt#float#1
[op]             Mod | [f] ari...lt#float#3 | [f]                a | [f]                2
[op]             Add | [f] ari...lt#float#4 | [f] ari...lt#float#2 | [f] ari...lt#float#3
[op]      IndexedGet | [f]                a | [f]                m | [f] ari...lt#float#4
");

        [TestMethod]
        public void EmitIndexReads5() => TestEmittedOpcodes(@"
matrix3x2 m = [1 2; 3 4; 5 6];
float a = m[0];
a = m[a];
", @"
[op]             Set | [f]              m+0 | [f]                1 | --------------------
[op]             Set | [f]              m+1 | [f]                2 | --------------------
[op]             Set | [f]              m+4 | [f]                3 | --------------------
[op]             Set | [f]              m+5 | [f]                4 | --------------------
[op]             Set | [f]              m+8 | [f]                5 | --------------------
[op]             Set | [f]              m+9 | [f]                6 | --------------------
[op]      IndexedGet | [f]                a | [f]                m | [f]                0
[op]             Div | [f] ari...lt#float#0 | [f]                a | [f]                2
[op]           Floor | [f] ari...lt#float#1 | [f] ari...lt#float#0 | --------------------
[op]             Mul | [f] ari...lt#float#2 | [f]                4 | [f] ari...lt#float#1
[op]             Mod | [f] ari...lt#float#3 | [f]                a | [f]                2
[op]             Add | [f] ari...lt#float#4 | [f] ari...lt#float#2 | [f] ari...lt#float#3
[op]      IndexedGet | [f]                a | [f]                m | [f] ari...lt#float#4
");

        [TestMethod]
        public void EmitNonvoidIntrinsic() => TestEmittedOpcodes(@"
float a = floor(3);
a = floor(a);
a = atan2(a, a);
a = atan2(3, a);
", @"
[op]             Set | [f]                a | [f]                3 | --------------------
[op]           Floor | [f]                a | [f]                a | --------------------
[op]           Atan2 | [f]                a | [f]                a | [f]                a
[op]           Atan2 | [f]                a | [f]                3 | [f]                a
");

    }
}