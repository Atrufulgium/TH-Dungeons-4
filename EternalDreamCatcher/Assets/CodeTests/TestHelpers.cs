using NUnit.Framework;

namespace Atrufulgium.EternalDreamCatcher.Tests {
    internal static class TestHelpers {
        public static void AssertTrimmedStringsEqual(string expected, string actual) {
            expected = expected.Replace("\r\n", "\n").Replace("\n\r", "\n").Replace("\r", "\n").Trim();
            actual = actual.Replace("\r\n", "\n").Replace("\n\r", "\n").Replace("\r", "\n").Trim();
            Assert.AreEqual(expected, actual, $"Expected full string:\n{expected}\n\nActual full string:\n{actual}\n\n");
        }
    }
}
