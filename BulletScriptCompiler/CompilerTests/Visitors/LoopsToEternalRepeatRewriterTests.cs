using Atrufulgium.BulletScript.Compiler.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.Visitors.Tests {

    [TestClass]
    public class LoopsToEternalRepeatRewriterTests {

        [TestMethod]
        public void TestEternalRepeat() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
repeat {
    i = 4;
    break;
}
", @"
[root]
statements:
    [local declaration]
    declaration:
        [variable declaration]
        identifier:
            [identifier name]
            name:
                i
        type:
            float
        initializer:
            [literal float]
            value:
                3
    [repeat loop]
    count:
        [none]
    body:
        [block]
        statements:
            [expression statement]
            statement:
                [assignment]
                lhs:
                    [identifier name]
                    name:
                        i
                op:
                    =
                rhs:
                    [literal float]
                    value:
                        4
            [break]
", new LoopsToEternalRepeatRewriter());

        [TestMethod]
        public void TestRepeat() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
repeat (i) {
    i = 4;
    break;
}
", @"
[root]
    [variable declaration]   float i = 3
    [variable declaration]   float looptemp#0 = i
    [repeat loop]            repeat 
            [expression]             (looptemp#0--)
            [if]                     if ((looptemp#0 >= 0))
                    [expression]             i = 4
                    [break]                 
                    [continue]              
            [break]
", compactTree: true, new LoopsToEternalRepeatRewriter());

        [TestMethod]
        public void TestWhile() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
while (i == 3) {
    i = 4;
    break;
}
", @"
[root]
    [variable declaration]   float i = 3
    [repeat loop]            repeat 
            [if]                     if ((i == 3))
                    [expression]             i = 4
                    [break]                 
                    [continue]              
            [break]
", compactTree: true, new LoopsToEternalRepeatRewriter());

        [TestMethod]
        public void TestFor() => TestHelpers.AssertGeneratesTree(@"
float i = 3;
for (float j = 3; i == j; j++) {
    i = 4;
    break;
}
", @"
[root]
    [variable declaration]   float i = 3
    [variable declaration]   float j = 3
    [variable declaration]   float looptemp#0 = 1
    [repeat loop]            repeat 
            [if]                     if (looptemp#0)
                    [expression]             looptemp#0 = 0
            else
                    [expression]             (j++)
            [if]                     if ((i == j))
                    [expression]             i = 4
                    [break]                 
                    [continue]              
            [break]
", compactTree: true, new LoopsToEternalRepeatRewriter());

        [TestMethod]
        public void TestNested() => TestHelpers.AssertGeneratesTree(@"
repeat (10) {
    for (float i = 0; i < 9; i++) {
        while (i < 8) {
            repeat (i - 7) {
                i = 6;
            }
        }
    }
}
", @"
[root]
    [variable declaration]   float looptemp#2 = 10
    [repeat loop]            repeat 
            [expression]             (looptemp#2--)
            [if]                     if ((looptemp#2 >= 0))
                    [variable declaration]   float i = 0
                    [variable declaration]   float looptemp#1 = 1
                    [repeat loop]            repeat 
                            [if]                     if (looptemp#1)
                                    [expression]             looptemp#1 = 0
                            else
                                    [expression]             (i++)
                            [if]                     if ((i < 9))
                                    [repeat loop]            repeat 
                                            [if]                     if ((i < 8))
                                                    [variable declaration]   float looptemp#0 = (i - 7)
                                                    [repeat loop]            repeat 
                                                            [expression]             (looptemp#0--)
                                                            [if]                     if ((looptemp#0 >= 0))
                                                                    [expression]             i = 6
                                                                    [continue]              
                                                            [break]                 
                                                    [continue]              
                                            [break]                 
                                    [continue]              
                            [break]                 
                    [continue]              
            [break]
", compactTree: true, new LoopsToEternalRepeatRewriter());
    }
}