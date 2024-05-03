﻿using Atrufulgium.BulletScript.Compiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;

namespace CompilerTests {
    internal static class TestHelpers {
        public static void AssertICollectionsEqual(ICollection expected, ICollection actual) {
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

        public static void AssertCollectionsEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
            => AssertICollectionsEqual(expected.ToList(), actual.ToList());

        public static void AssertNoErrorDiagnostics(IEnumerable<Diagnostic> diagnostics) {
            var errorDiags = diagnostics.Where(d => d.DiagnosticLevel == DiagnosticLevel.Error);
            if (!errorDiags.Any())
                return;
            string msg = "";
            foreach (var err in errorDiags) {
                msg += "\n" + err.ToString();
            }
            Assert.Fail(msg);
        }

        /// <summary>
        /// Whether a collection of diagnostics contains a given diagnostic.
        /// <br/>
        /// <paramref name="errorLine"/> is offset by 1 to match the one-indexed
        /// nature of "lines".
        /// </summary>
        public static void AssertContainsDiagnostic(
            IEnumerable<Diagnostic> diagnostics,
            string errorID,
            int errorLine
        ) {
            foreach (var diag in diagnostics) {
                if (diag.ID == errorID && diag.Location.line == errorLine + 1)
                    return;
            }
            string msg = $"Did not encounter error \"{errorID}\" at line {errorLine + 1}.";
            if (diagnostics.Any()) {
                msg += " Errors:\n";
                foreach (var diag in diagnostics) {
                    msg += $"{diag}\n";
                }
            } else {
                msg += " No errors at all.";
            }
            Assert.Fail(msg);
        }

        public static void AssertTrimmedStringsEqual(string expected, string actual) {
            expected = expected.ReplaceLineEndings().Trim();
            actual = actual.ReplaceLineEndings().Trim();
            // (newlines for nicer test output)
            Assert.AreEqual("\n" + expected + "\n", "\n" + actual + "\n");
        }
    }
}
