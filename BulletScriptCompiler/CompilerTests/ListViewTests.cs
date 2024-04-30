using Atrufulgium.BulletScript.Compiler.Helpers;
using CompilerTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Tests {

    [TestClass]
    public class ListViewTests {

        [TestMethod]
        public void ListViewTest1() {
            List<int> ints = new() { 1, 2, 3, 4, 5, 6, 7, 8 };
            TestHelpers.AssertCollectionsEqual(
                ints.Skip(2).SkipLast(3),
                ints.GetView(2..^3)
            );
        }

        [TestMethod]
        public void ListViewTest2() {
            List<int> ints = new() { 1, 2, 3, 4, 5, 6, 7, 8 };
            TestHelpers.AssertCollectionsEqual(
                ints.Skip(8).SkipLast(9),
                ints.GetView(8..^9)
            );
        }

        [TestMethod]
        public void ListViewTest3() {
            List<int> ints = new() { 1, 2, 3, 4, 5, 6, 7, 8 };
            var view = ints.GetView(2..^3);
            Assert.AreEqual(
                ints[4],
                view[2]
            );

            Assert.AreEqual(
                ints[4],
                view[^1]
            );

            ints.Insert(0, 0);

            Assert.AreEqual(
                ints[4],
                view[2]
            );

            Assert.AreEqual(
                ints[4],
                view[^2] // Note that the collection grew one so we need to step one more back as this is "from end"
            );
        }

        [TestMethod]
        public void ListViewTest4() {
            List<int> ints = new() { 1, 2, 3, 4, 5, 6, 7, 8 };
            // Next line only difference with previous test lol
            var view = ints.GetView(..^1).GetView(1..).GetView(1..^1).GetView(..^1);
            Assert.AreEqual(
                ints[4],
                view[2]
            );

            Assert.AreEqual(
                ints[4],
                view[^1]
            );

            ints.Insert(0, 0);

            Assert.AreEqual(
                ints[4],
                view[2]
            );

            Assert.AreEqual(
                ints[4],
                view[^2]
            );
        }

        // Forgot a case where Count would return negative.
        [TestMethod]
        public void ListViewTest5() {
            List<int> ints = new() { 1, 2, 3, 4, 5, 6, 7, 8 };
            var view = ints.GetView(7..^7);
            TestHelpers.AssertCollectionsEqual(
                Array.Empty<int>(),
                view
            );
            Assert.AreEqual(0, view.Count);
        }
    }
}