using NUnit.Framework;
using System.Linq;

namespace Atrufulgium.EternalDreamCatcher.Base.Tests {
    public class PermutationTests {

        [Test]
        public void TestPermute() {
            // [0,1,2,3,..]
            using Permutation<int> p = new(10, Unity.Collections.Allocator.Temp);
            // [1,0,2,3,..]
            p.Swap(0, 1);
            // [2,0,1,3,..]
            p.Swap(0, 2);
            // [2,1,0,3,..]
            p.Swap(1, 2);
            var expected = new int[] { 2, 1, 0, 3 };
            var actualInput = new int[] { 0, 1, 2, 3 };
            CollectionAssert.AreEqual(expected, actualInput.Select(i => p.Permute(i)));
        }
    }
}

