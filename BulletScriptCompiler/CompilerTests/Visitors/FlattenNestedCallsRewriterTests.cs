using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class FlattenNestedCallsRewriterTests {

        [TestMethod]
        public void Test() => TestHelpers.AssertGeneratesTree(@"
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
            [variable declaration]   float invocation#result#float#0 = D(4)
            [variable declaration]   float invocation#result#float#1 = C(3)
            [variable declaration]   float invocation#result#float#2 = A(1)
            [variable declaration]   float invocation#result#float#3 = B(invocation#result#float#2)
            [variable declaration]   float invocation#result#float#4 = D((invocation#result#float#1 + invocation#result#float#3))
            [variable declaration]   float invocation#result#float#5 = length([ invocation#result#float#0 invocation#result#float#4 5])
            [variable declaration]   float i = E(invocation#result#float#5)
            [variable declaration]   float invocation#result#float#0 = A(1)
            [variable declaration]   float invocation#result#float#1 = B(invocation#result#float#0)
            [variable declaration]   float j = C(invocation#result#float#1)
", compactTree: true, new FlattenNestedCallsRewriter());

    }
}