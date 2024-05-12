using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class AcknowledgeIntrinsicsRewriterTests {

        [TestMethod]
        public void Test() => TestHelpers.AssertGeneratesTree(@"
float a;
a = sin(230);
a = atan2(a, a);
a *= sin(a);
wait(a);
", @"
[root]
    <variable declaration>   float a
    <intr. invocation assi.> a = sin(230)
    <intr. invocation assi.> a = atan2(a,a)
    <variable declaration>   float global#intrinsic#temp#float
    <intr. invocation assi.> global#intrinsic#temp#float = sin(a)
    [expression]             a *= global#intrinsic#temp#float
    <intr. invocation stat.> wait(a)
", compactTree: true, new AcknowledgeIntrinsicsRewriter());

    }
}