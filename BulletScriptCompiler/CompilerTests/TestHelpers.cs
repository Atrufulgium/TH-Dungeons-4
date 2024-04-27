using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;

namespace CompilerTests {
    internal static class TestHelpers {
        public static void AssertCollectionsEqual(ICollection expected, ICollection actual) {
            try {
                CollectionAssert.AreEqual(expected, actual);
            } catch (AssertFailedException) {
                int minLength = Math.Min(expected.Count, actual.Count);
                string msg = "\n";
                object[] exp = new object[100];
                object[] act = new object[100];
                expected.CopyTo(exp, 0);
                actual.CopyTo(act, 0);
                int i;
                for (i = 0; i < minLength && i < 100; i++) {
                    msg += $"{i,2} [Exp] {exp[i]}\n   [Act] {act[i]}\n\n";
                }
                if (expected.Count < actual.Count && expected.Count < 100) {
                    for (; i < actual.Count && i < 100; i++) {
                        msg += $"{i,2} [Exp] [n/a]\n   [Act] {act[i]}\n\n";
                    }
                }
                if (actual.Count < expected.Count && actual.Count < 100) {
                    for (; i < expected.Count && i < 100; i++) {
                        msg += $"{i,2} [Exp] {exp[i]}\n   [Act] [n/a]\n\n";
                    }
                }
                if (minLength >= 100)
                    msg += "...\n(Displayed 100 items.)\n";
                if (expected.Count != actual.Count)
                    msg += $"(Expected count: {expected.Count})\n(Actual count: {actual.Count})";
                Assert.Fail(msg);
            }
        }
    }
}
