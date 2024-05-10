using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class FlattenArithmeticRewriterTests {

        [TestMethod]
        public void TestSimpleBinop() => TestHelpers.AssertGeneratesTree(@"
float a = 1 + 2 + 3;
", @"
[root]
    <variable declaration>   float arithmetic#result#0
    [expression]             arithmetic#result#0 = (2 + 3)
    [variable declaration]   float a = (1 + arithmetic#result#0)
", compactTree: true, new FlattenArithmeticRewriter());

        [TestMethod]
        public void TestSimplePrefix() => TestHelpers.AssertGeneratesTree(@"
float a = -1;
", @"
[root]
    [variable declaration]   float a = (-1)
", compactTree: true, new FlattenArithmeticRewriter());

        [TestMethod]
        public void TestBinopAndPrefix() => TestHelpers.AssertGeneratesTree(@"
float a = -1 + 2 + 3;
", @"
[root]
    <variable declaration>   float arithmetic#result#0
    [expression]             arithmetic#result#0 = (-1)
    <variable declaration>   float arithmetic#result#1
    [expression]             arithmetic#result#1 = (2 + 3)
    [variable declaration]   float a = (arithmetic#result#0 + arithmetic#result#1)
", compactTree: true, new FlattenArithmeticRewriter());


        [TestMethod]
        public void TestMatrix() => TestHelpers.AssertGeneratesTree(@"
float a = -1;
matrix m = [1 a; a+a 0-1];
", @"
[root]
    [variable declaration]   float a = (-1)
    <variable declaration>   float arithmetic#result#0
    [expression]             arithmetic#result#0 = (a + a)
    <variable declaration>   float arithmetic#result#1
    [expression]             arithmetic#result#1 = (0 - 1)
    [variable declaration]   matrix m = [ 1 a; arithmetic#result#0 arithmetic#result#1]
", compactTree: true, new FlattenArithmeticRewriter());

        [TestMethod]
        public void TestIndex1() => TestHelpers.AssertGeneratesTree(@"
matrix m = [1 2; 3 4];
float a = m[1; 0+1];
", @"
[root]
    [variable declaration]   matrix m = [ 1 2; 3 4]
    <variable declaration>   float arithmetic#result#0
    [expression]             arithmetic#result#0 = (0 + 1)
    [variable declaration]   float a = (m)[ 1; arithmetic#result#0]
", compactTree: true, new FlattenArithmeticRewriter());

        [TestMethod]
        public void TestIndex2() => TestHelpers.AssertGeneratesTree(@"
matrix m = [1 2; 3 4];
float a = (m + m)[1];
", @"
[root]
    [variable declaration]   matrix m = [ 1 2; 3 4]
    <variable declaration>   matrix2x2 arithmetic#result#0
    [expression]             arithmetic#result#0 = (m + m)
    [variable declaration]   float a = (arithmetic#result#0)[ 1]
", compactTree: true, new FlattenArithmeticRewriter());

        // TODO: Does this fuck up function calls?
        // TODO: index problem: the index matrix, the entire thing, is replaced with a variable
    }
}