using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class ThatsUncalledForRewriterTests {

        [TestMethod]
        public void Test() => TestHelpers.AssertGeneratesTree(@"
function void A() { B(); }
function void B() { C(); }
function void C() { D(); }
function void D() { }
function void ActuallyCalled() { }
function void main(float value) { ActuallyCalled(); }
", @"
[root]
    [method declaration]     void ActuallyCalled()
            [none]
    [method declaration]     void main(float value)
            [expression]             ActuallyCalled()
", compactTree: true, new ThatsUncalledForRewriter());

    }
}