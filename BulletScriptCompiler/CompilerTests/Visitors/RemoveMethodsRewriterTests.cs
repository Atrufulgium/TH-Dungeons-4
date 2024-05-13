using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class RemoveMethodsRewriterTests {

        [TestMethod]
        public void TestSimple() => TestHelpers.AssertGeneratesTree(@"
float a = 6;
function void A() { a = 3; C(); }
function void B() { A(); C(); }
function void C() { a = 5; }
", @"
[root]
    [variable declaration]   float a = 6
    <variable declaration>   float A()#entry
    <variable declaration>   float B()#entry
    <variable declaration>   float C()#entry
    <goto>                   goto the end
    <goto label>             A():
    [expression]             a = 3
    [expression]             C()#entry = 1
    <goto>                   goto C()
    <goto label>             C()#return-to-entry#1:
    <variable declaration>   float global#returntest
    [expression]             global#returntest = (A()#entry == 1)
    <conditional goto>       if global#returntest goto A()#return-to-entry#1
    <goto label>             B():
    [expression]             A()#entry = 1
    <goto>                   goto A()
    <goto label>             A()#return-to-entry#1:
    [expression]             C()#entry = 2
    <goto>                   goto C()
    <goto label>             C()#return-to-entry#2:
    <goto>                   goto the end
    <goto label>             C():
    [expression]             a = 5
    <variable declaration>   float global#returntest
    [expression]             global#returntest = (C()#entry == 1)
    <conditional goto>       if global#returntest goto C()#return-to-entry#1
    [expression]             global#returntest = (C()#entry == 2)
    <conditional goto>       if global#returntest goto C()#return-to-entry#2
    <goto label>             the end:
", compactTree: true, new RemoveMethodsRewriter());

        [TestMethod]
        public void TestMainAndReturn() => TestHelpers.AssertGeneratesTree(@"
float a = 6;
function void A() { a = 3; return; C(); }
function void main(float value) { A(); C(); }
function void C() { a = 5; }
", @"
[root]
    [variable declaration]   float a = 6
    <variable declaration>   float A()#entry
    <variable declaration>   float main(float)#entry
    <variable declaration>   float C()#entry
    <goto label>             ##main(float)##:
    [expression]             A()#entry = 1
    <goto>                   goto A()
    <goto label>             A()#return-to-entry#1:
    [expression]             C()#entry = 2
    <goto>                   goto C()
    <goto label>             C()#return-to-entry#2:
    <goto>                   goto the end
    <goto label>             A():
    [expression]             a = 3
    <goto>                   goto A()#return
    [expression]             C()#entry = 1
    <goto>                   goto C()
    <goto label>             C()#return-to-entry#1:
    <goto label>             A()#return:
    <variable declaration>   float global#returntest
    [expression]             global#returntest = (A()#entry == 1)
    <conditional goto>       if global#returntest goto A()#return-to-entry#1
    <goto label>             C():
    [expression]             a = 5
    <variable declaration>   float global#returntest
    [expression]             global#returntest = (C()#entry == 1)
    <conditional goto>       if global#returntest goto C()#return-to-entry#1
    [expression]             global#returntest = (C()#entry == 2)
    <conditional goto>       if global#returntest goto C()#return-to-entry#2
    <goto label>             the end:
", compactTree: true, new RemoveMethodsRewriter());
    }
}