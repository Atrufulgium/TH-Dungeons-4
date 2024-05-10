using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class SimpleAssignmentRewriterTests {

        [TestMethod]
        public void Test() => TestHelpers.AssertGeneratesTree(@"
float a;
a = 3;
a += 3;
", @"
[root]
    <variable declaration>   float a
    [expression]             a = 3
    [expression]             a = (a + 3)
", compactTree: true, new SimpleAssignmentRewriter());

    }
}