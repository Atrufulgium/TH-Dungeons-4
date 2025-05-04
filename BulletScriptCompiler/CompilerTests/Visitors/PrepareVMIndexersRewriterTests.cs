using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class PrepareVMIndexersRewriterTests {

        [TestMethod]
        public void Test1D() => TestHelpers.AssertGeneratesTree(@"
float a = 0;
matrix m = [1 2; 3 4];
a = m[3];
a = m[a * 2 + 1];
", @"
[root]
    [variable declaration]   float a = 0
    [variable declaration]   matrix m = [ 1 2; 3 4]
    [expression]             a = (m)[ ((floor((3 / 2)) * 2) + (3 % 2))]
    [expression]             a = (m)[ ((floor((((a * 2) + 1) / 2)) * 2) + (((a * 2) + 1) % 2))]
", compactTree: true, new PrepareVMIndexersRewriter());

        [TestMethod]
        public void Test1DFolded() => TestHelpers.AssertGeneratesTree(@"
float a = 0;
matrix m = [1 2; 3 4];
a = m[3];
a = m[a * 2 + 1];
", @"
[root]
    [variable declaration]   float a = 0
    [variable declaration]   matrix m = [ 1 2; 3 4]
    [expression]             a = (m)[ 3]
    [expression]             a = (m)[ ((floor((((a * 2) + 1) / 2)) * 2) + (((a * 2) + 1) % 2))]
", compactTree: true, new PrepareVMIndexersRewriter(), new ConstantFoldRewriter());

        [TestMethod]
        public void Test1DSizes() => TestHelpers.AssertGeneratesTree(@"
float a = 0;
matrix m1 = [1 2 3; 4 5 6];
a = m1[4];
matrix m2 = [1 2 3 4; 5 6 7 8];
a = m2[4];
matrix m3 = [1 2; 3 4; 5 6; 7 8];
a = m3[4];
", @"
[root]
    [variable declaration]   float a = 0
    [variable declaration]   matrix m1 = [ 1 2 3; 4 5 6]
    [expression]             a = (m1)[ ((floor((4 / 3)) * 4) + (4 % 3))]
    [variable declaration]   matrix m2 = [ 1 2 3 4; 5 6 7 8]
    [expression]             a = (m2)[ ((floor((4 / 4)) * 4) + (4 % 4))]
    [variable declaration]   matrix m3 = [ 1 2; 3 4; 5 6; 7 8]
    [expression]             a = (m3)[ ((floor((4 / 2)) * 4) + (4 % 2))]
", compactTree: true, new PrepareVMIndexersRewriter());

        [TestMethod]
        public void Test1DSizesFolded() => TestHelpers.AssertGeneratesTree(@"
float a = 0;
matrix m1 = [1 2 3; 4 5 6];
a = m1[4];
matrix m2 = [1 2 3 4; 5 6 7 8];
a = m2[4];
matrix m3 = [1 2; 3 4; 5 6; 7 8];
a = m3[4];
", @"
[root]
    [variable declaration]   float a = 0
    [variable declaration]   matrix m1 = [ 1 2 3; 4 5 6]
    [expression]             a = (m1)[ 5]
    [variable declaration]   matrix m2 = [ 1 2 3 4; 5 6 7 8]
    [expression]             a = (m2)[ 4]
    [variable declaration]   matrix m3 = [ 1 2; 3 4; 5 6; 7 8]
    [expression]             a = (m3)[ 8]
", compactTree: true, new PrepareVMIndexersRewriter(), new ConstantFoldRewriter());

        [TestMethod]
        public void Test2D2x2() => TestHelpers.AssertGeneratesTree(@"
float a = 0;
matrix m = [1 2; 3 4];
a = m[a 1];
", @"
[root]
    [variable declaration]   float a = 0
    [variable declaration]   matrix m = [ 1 2; 3 4]
    [expression]             a = (m)[ ((floor(a) * 2) + 1)]
", compactTree: true, new PrepareVMIndexersRewriter());

        [TestMethod]
        public void Test2dOtherSizes() => TestHelpers.AssertGeneratesTree(@"
float a = 0;
matrix m1 = [1 2 3; 4 5 6];
a = m1[a 1];
matrix m2 = [1 2 3 4; 5 6 7 8];
a = m2[1 a];
matrix m3 = [1 2; 3 4; 5 6; 7 8];
a = m3[a 1];
", @"
[root]
    [variable declaration]   float a = 0
    [variable declaration]   matrix m1 = [ 1 2 3; 4 5 6]
    [expression]             a = (m1)[ ((floor(a) * 4) + 1)]
    [variable declaration]   matrix m2 = [ 1 2 3 4; 5 6 7 8]
    [expression]             a = (m2)[ ((floor(1) * 4) + a)]
    [variable declaration]   matrix m3 = [ 1 2; 3 4; 5 6; 7 8]
    [expression]             a = (m3)[ ((floor(a) * 4) + 1)]
", compactTree: true, new PrepareVMIndexersRewriter());

    }
}