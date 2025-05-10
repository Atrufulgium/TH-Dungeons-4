using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Atrufulgium.BulletScript.Compiler.Tests {
    [TestClass]
    public class CompilerTests {

        unsafe static void TestCode(string code, string bytecode) {
            var res = Compiler.Compile(code);
            res.TryGetBytecodeOutput(out var output);
            if (output == null) {
                Assert.Fail(string.Join('\n',res.Diagnostics.Select(d => d.ToString())));
                return;
            }

            StringBuilder actual = new();
            actual.AppendLine($"String memory ({output.Strings.Length}):");
            foreach (var (s,i) in output.Strings.Select((s,i) => (s,i))) {
                actual.AppendLine($"{i,4} | \"{s}\"");
            }

            actual.AppendLine($"Initial memory ({output.Memory.Length}):");
            for (int i4 = 0; i4 < output.Memory.Length; i4 += 4) {
                var (f1, f2, f3, f4) = (output.Memory[i4], output.Memory[i4 + 1], output.Memory[i4 + 2], output.Memory[i4 + 3]);
                actual.AppendLine($"{i4,4} | {f1,6:0.0} {f2,8:0.0}   {f3,8:0.0}   {f4,8:0.0}");
            }

            actual.AppendLine($"Bytecode ({output.OpCodes.Length}):");
            foreach (var (b,i) in output.OpCodes.Select((b,i) => (b,i))) {
                actual.AppendLine($"{i,4} | {b.Item1,6:0.0} {b.Item2,10:0.000} {b.Item3,10:0.000} {b.Item4,10:0.000}");
            }

            Assert.AreEqual('\n' + bytecode.Trim(), '\n' + actual.ToString().Trim());
        }

        // Wow this is so similar to my handwritten assembly, it's scary
        // The only difference is the introduction of variable #15
        [TestMethod]
        public void TestPipeline()
            => TestCode(@"
float t;
spawnspeed = 0.12;
spawnposition = [0.5; 0.5];
repeat {
    t += 0.025f;
    spawnrotation += sin(t) + 0.25f;
    spawn();
    spawnrotation += 0.25f;
    spawn();
    spawnrotation += 0.25f;
    spawn();
    spawnrotation += 0.25f;
    spawn();
    wait(5);
}
", @"
String memory (1):
   0 | ""error""
Initial memory (32):
   0 |    0.0      0.0        1.0        1.0
   4 |    0.0      0.0        0.0        0.0
   8 |    0.0      0.0        0.0        0.0
  12 |    0.0      0.0        0.0        0.0
  16 |    0.0      0.0        0.0        0.0
  20 |    0.0      0.0        0.0        0.0
  24 |    0.0      0.0        0.0        0.0
  28 |    0.0      0.0        0.0        0.0
Bytecode (16):
   0 |   10.0      2.000      0.120      0.000
   1 |   10.0      4.000      0.500      0.000
   2 |   10.0      5.000      0.500      0.000
   3 |   81.0     13.000      0.025     13.000
   4 |   98.0     14.000     13.000      0.000
   5 |   81.0     15.000      0.250     14.000
   6 |   82.0      1.000      1.000     15.000
   7 |   34.0      0.000      0.000      0.000
   8 |   81.0      1.000      0.250      1.000
   9 |   34.0      0.000      0.000      0.000
  10 |   81.0      1.000      0.250      1.000
  11 |   34.0      0.000      0.000      0.000
  12 |   81.0      1.000      0.250      1.000
  13 |   34.0      0.000      0.000      0.000
  14 |   21.0      5.000      0.000      0.000
  15 |   19.0      2.000      0.000      0.000
");
    }
}
