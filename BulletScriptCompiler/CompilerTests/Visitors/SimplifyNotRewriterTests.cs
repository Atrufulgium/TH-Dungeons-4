using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class SimplifyNotRewriterTests {

        [TestMethod]
        public void Test() => TestHelpers.AssertGeneratesTree(@"
float a = 0;
float b = 0;
a = !(a == b);
a = !(a != b);
a = !(a > b);
a = !(a < b);
a = !(a >= b);
a = !(a <= b);
", @"
[root]
    [variable declaration]   float a = 0
    [variable declaration]   float b = 0
    [expression]             a = (a != b)
    [expression]             a = (a == b)
    [expression]             a = (a <= b)
    [expression]             a = (a >= b)
    [expression]             a = (a < b)
    [expression]             a = (a > b)
", compactTree: true, new SimplifyNotRewriter());

    }
}