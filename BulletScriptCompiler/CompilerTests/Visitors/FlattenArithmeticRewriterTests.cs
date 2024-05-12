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
    <variable declaration>   float arithmetic#result#float#0
    [expression]             arithmetic#result#float#0 = (2 + 3)
    [variable declaration]   float a = (1 + arithmetic#result#float#0)
", compactTree: true, new FlattenArithmeticRewriter());

        [TestMethod]
        public void TestSimpleBinop2() => TestHelpers.AssertGeneratesTree(@"
float a = 1 + 2 + 3;
float a = 1 + 2 + 3;
", @"
[root]
    <variable declaration>   float arithmetic#result#float#0
    [expression]             arithmetic#result#float#0 = (2 + 3)
    [variable declaration]   float a = (1 + arithmetic#result#float#0)
    <variable declaration>   float arithmetic#result#float#0
    [expression]             arithmetic#result#float#0 = (2 + 3)
    [variable declaration]   float a = (1 + arithmetic#result#float#0)
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
    <variable declaration>   float arithmetic#result#float#0
    [expression]             arithmetic#result#float#0 = (-1)
    <variable declaration>   float arithmetic#result#float#1
    [expression]             arithmetic#result#float#1 = (2 + 3)
    [variable declaration]   float a = (arithmetic#result#float#0 + arithmetic#result#float#1)
", compactTree: true, new FlattenArithmeticRewriter());


        [TestMethod]
        public void TestMatrix() => TestHelpers.AssertGeneratesTree(@"
float a = -1;
matrix m = [1 a; a+a 0-1];
", @"
[root]
    [variable declaration]   float a = (-1)
    <variable declaration>   float arithmetic#result#float#0
    [expression]             arithmetic#result#float#0 = (a + a)
    <variable declaration>   float arithmetic#result#float#1
    [expression]             arithmetic#result#float#1 = (0 - 1)
    [variable declaration]   matrix m = [ 1 a; arithmetic#result#float#0 arithmetic#result#float#1]
", compactTree: true, new FlattenArithmeticRewriter());

        [TestMethod]
        public void TestMatrix2() => TestHelpers.AssertGeneratesTree(@"
matrix m = [1 1];
m = m + [2 2];
", @"
[root]
    [variable declaration]   matrix m = [ 1 1]
    <variable declaration>   matrix1x2 arithmetic#result#matrix1x2#0
    [expression]             arithmetic#result#matrix1x2#0 = [ 2 2]
    [expression]             m = (m + arithmetic#result#matrix1x2#0)
", compactTree: true, new FlattenArithmeticRewriter());

        [TestMethod]
        public void TestIndex1() => TestHelpers.AssertGeneratesTree(@"
matrix m = [1 2; 3 4];
float a = m[1; 0+1];
", @"
[root]
    [variable declaration]   matrix m = [ 1 2; 3 4]
    <variable declaration>   float arithmetic#result#float#0
    [expression]             arithmetic#result#float#0 = (0 + 1)
    [variable declaration]   float a = (m)[ 1; arithmetic#result#float#0]
", compactTree: true, new FlattenArithmeticRewriter());

        [TestMethod]
        public void TestIndex2() => TestHelpers.AssertGeneratesTree(@"
matrix m = [1 2; 3 4];
float a = (m + m)[1];
", @"
[root]
    [variable declaration]   matrix m = [ 1 2; 3 4]
    <variable declaration>   matrix2x2 arithmetic#result#matrix2x2#0
    [expression]             arithmetic#result#matrix2x2#0 = (m + m)
    [variable declaration]   float a = (arithmetic#result#matrix2x2#0)[ 1]
", compactTree: true, new FlattenArithmeticRewriter());

        [TestMethod]
        public void TestInvocation() => TestHelpers.AssertGeneratesTree(@"
function float A(float value) { return value; }
function float B(float value) { return value; }
function float C(float value) { return value; }
function float D(float value) { return value; }
function float E(float value) { return value; }

function void main(float value) {
    float i = E(length([D(4) D(C(3) + B(A(1))) 5]));
    float j = C(B(A(1)));
}
", @"
[root]
    [method declaration]     float A(float value)
            [return]                 value
    [method declaration]     float B(float value)
            [return]                 value
    [method declaration]     float C(float value)
            [return]                 value
    [method declaration]     float D(float value)
            [return]                 value
    [method declaration]     float E(float value)
            [return]                 value
    [method declaration]     void main(float value)
            <variable declaration>   float arithmetic#result#float#0
            [expression]             arithmetic#result#float#0 = D(4)
            <variable declaration>   float arithmetic#result#float#1
            [expression]             arithmetic#result#float#1 = C(3)
            <variable declaration>   float arithmetic#result#float#2
            [expression]             arithmetic#result#float#2 = A(1)
            <variable declaration>   float arithmetic#result#float#3
            [expression]             arithmetic#result#float#3 = B(arithmetic#result#float#2)
            <variable declaration>   float arithmetic#result#float#4
            [expression]             arithmetic#result#float#4 = (arithmetic#result#float#1 + arithmetic#result#float#3)
            <variable declaration>   float arithmetic#result#float#5
            [expression]             arithmetic#result#float#5 = D(arithmetic#result#float#4)
            <variable declaration>   matrix1x3 arithmetic#result#matrix1x3#0
            [expression]             arithmetic#result#matrix1x3#0 = [ arithmetic#result#float#0 arithmetic#result#float#5 5]
            <variable declaration>   float arithmetic#result#float#6
            [expression]             arithmetic#result#float#6 = length(arithmetic#result#matrix1x3#0)
            [variable declaration]   float i = E(arithmetic#result#float#6)
            <variable declaration>   float arithmetic#result#float#0
            [expression]             arithmetic#result#float#0 = A(1)
            <variable declaration>   float arithmetic#result#float#1
            [expression]             arithmetic#result#float#1 = B(arithmetic#result#float#0)
            [variable declaration]   float j = C(arithmetic#result#float#1)
", compactTree: true, new FlattenArithmeticRewriter());

        // I actually forgot to test this thing leaves the simplest stuff untouched.
        [TestMethod]
        public void TestSimplerBinop1() => TestHelpers.AssertGeneratesTree(@"
float a = 1 + 2;
", @"
[root]
    [variable declaration]   float a = (1 + 2)
", compactTree: true, new FlattenArithmeticRewriter());

        [TestMethod]
        public void TestSimplerBinop2() => TestHelpers.AssertGeneratesTree(@"
float a;
a = 1 + 2;
", @"
[root]
    <variable declaration>   float a
    [expression]             a = (1 + 2)
", compactTree: true, new FlattenArithmeticRewriter());
    }
}