using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class ConstantFoldRewriterTests {

        [TestMethod]
        public void TestSimple() => TestHelpers.AssertGeneratesTree(@"
float a;
a = 2 + 3;
a = 2 - 3;
a = 2 * 3;
a = 2 / 3;
a = 2 % 3;
a = 2 ^ 3;
a = 2 & 3;
a = 2 | 3;
a = 2 == 3;
a = 2 != 3;
a = 2 >= 3;
a = 2 <= 3;
a = 2 > 3;
a = 2 < 3;
a = -2;
a = !2;
", @"
[root]
    <variable declaration>   float a
    [expression]             a = 5
    [expression]             a = -1
    [expression]             a = 6
    [expression]             a = 0.6666667
    [expression]             a = 2
    [expression]             a = 8
    [expression]             a = 1
    [expression]             a = 1
    [expression]             a = 0
    [expression]             a = 1
    [expression]             a = 0
    [expression]             a = 1
    [expression]             a = 0
    [expression]             a = 1
    [expression]             a = -2
    [expression]             a = 0
", compactTree: true, new ConstantFoldRewriter());

        [TestMethod]
        public void TestCompound() => TestHelpers.AssertGeneratesTree(@"
float a;
a = 1 + 2 * 3 / 4 % 5 ^ 6;
", @"
[root]
    <variable declaration>   float a
    [expression]             a = 2.5
", compactTree: true, new ConstantFoldRewriter());

        [TestMethod]
        public void TestCompoundIntrinsics() => TestHelpers.AssertGeneratesTree(@"
float a;
a = sin(cos(tan(asin(acos(atan(ceil(floor(round(abs(length(distance(2,3))))))))))));
", @"
[root]
    <variable declaration>   float a
    [expression]             a = 0.5846703
", compactTree: true, new ConstantFoldRewriter());

    }
}