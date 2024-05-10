using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class AcknowledgeSimpleAssignmentsRewriterTests {

        [TestMethod]
        public void Test() => TestHelpers.AssertGeneratesTree(@"
float a;
matrix m = [0 0];
a = a;
a = 3;
a = -a;
a = -3;
a = a+a;
a = a+3;
a = 3+a;
a = m[a];
a = m[3];
m = [a 3];
m = [a:3];
", @"
[root]
    <variable declaration>   float a
    [variable declaration]   matrix m = [ 0 0]
    <simple assignment>      a = a
    <simple assignment>      a = 3
    <simple assignment>      a = (-a)
    <simple assignment>      a = (-3)
    <simple assignment>      a = (a + a)
    <simple assignment>      a = (a + 3)
    <simple assignment>      a = (3 + a)
    <simple assignment>      a = (m)[ a]
    <simple assignment>      a = (m)[ 3]
    <simple assignment>      m = [ a 3]
    <simple assignment>      m = [a:3]
", compactTree: true, new AcknowledgeSimpleAssignmentsRewriter());

    }
}