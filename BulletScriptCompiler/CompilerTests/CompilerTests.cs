using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Atrufulgium.BulletScript.Compiler.Tests {
    [TestClass]
    public class CompilerTests {

        static void TestCode(string code, string bytecode) {
            var res = Compiler.Compile(code);
            res.TryGetBytecodeOutput(out var output);
            if (output == null) {
                Assert.Fail(string.Join('\n',res.Diagnostics.Select(d => d.ToString())));
                return;
            }

            var expected = '\n' + string.Join('\n', bytecode.Split('\n').Select(s => s.Trim())) + '\n';
            var actual = '\n' + string.Join('\n', bytecode.Split('\n').Select(s => s.Trim())) + '\n';

            Assert.AreEqual(expected, actual);
        }

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
Initial memory (20):
   0 |    0.0      0.0        1.0        1.0
   4 |    0.0      0.0        0.0        0.0
   8 |    0.0      0.0        0.0        0.0
  12 |    0.0      0.0        0.0        0.0
Local:                                      
  16 |    0.0      0.0        0.0        0.0
Bytecode (16):
   0 |   10        2          0.120      0    
   1 |   10        4          0.500      0    
   2 |   10        5          0.500      0    
   3 |   81       13          0.025     13    
   4 |   98       17         13          0    
   5 |   81       16          0.250     17    
   6 |   82        1          1         16    
   7 |   34        0          0          0    
   8 |   81        1          0.250      1    
   9 |   34        0          0          0    
  10 |   81        1          0.250      1    
  11 |   34        0          0          0    
  12 |   81        1          0.250      1    
  13 |   34        0          0          0    
  14 |   68        5.000      0          0    
  15 |   19        2          0          0
");
    }
}
