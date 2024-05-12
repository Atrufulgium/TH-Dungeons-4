using Atrufulgium.BulletScript.Compiler.Helpers;
using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Atrufulgium.BulletScript.Compiler.Visitors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Tests {

    [TestClass]
    public class EmitHLOPTests {

        static void TestSuccess(string code, string result) {
            var visitors = Compiler.StandardCompilationOrder;
            TestHelpers.AssertCompiles(code, visitors);
            var emitWalker = visitors.OfType<EmitWalker>().First();
            TestHelpers.AssertTrimmedStringsEqual(result, string.Join('\n', emitWalker.OPCodes));
        }

        [TestMethod]
        public void EmitBasicArithmeticBinaryCommutative() => TestSuccess(@"
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
    }
}