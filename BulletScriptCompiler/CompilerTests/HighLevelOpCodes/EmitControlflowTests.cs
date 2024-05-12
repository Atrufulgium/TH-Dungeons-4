using static Atrufulgium.BulletScript.Compiler.Tests.Helpers.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.HighLevelOpCodes.Tests {

    [TestClass]
    public class EmitControlflowTests {

        // yeah i could save 1 instruction in the "both if and else block" case
        // not that relevant
        [TestMethod]
        public void TestIf() => TestEmittedOpcodes(@"
if (autoclear) {
    harmsenemies = true;
} else {
    if (harmsplayers) {
        clearimmune = true;
    }
}
", @"
[op]             Not | [f]   global#if#temp | [f]        autoclear | --------------------
[op] JumpConditional | [i] fal...ch-label-1 | [f]   global#if#temp | --------------------
[op]             Set | [f]     harmsenemies | [f]                1 | --------------------
[op]            Jump | [i]     done-label-0 | -------------------- | --------------------
[op]       GotoLabel | [i] fal...ch-label-1 | -------------------- | --------------------
[op]             Not | [f]   global#if#temp | [f]     harmsplayers | --------------------
[op] JumpConditional | [i]     done-label-2 | [f]   global#if#temp | --------------------
[op]             Set | [f]      clearimmune | [f]                1 | --------------------
[op]       GotoLabel | [i]     done-label-2 | -------------------- | --------------------
[op]       GotoLabel | [i]     done-label-0 | -------------------- | --------------------
");

        [TestMethod]
        public void TestRepeat() => TestEmittedOpcodes(@"
repeat {
    break;
}

repeat (10) {
    continue;
}
", @"
[op]             Set | [f]       looptemp#0 | [f]               10 | --------------------
[op]       GotoLabel | [i] con...ue-label-2 | -------------------- | --------------------
[op]             Sub | [f]       looptemp#0 | [f]       looptemp#0 | [f]                1
[op]        LessThan | [f]   global#if#temp | [f]                0 | [f]       looptemp#0
[op]             Not | [f]   global#if#temp | [f]   global#if#temp | --------------------
[op] JumpConditional | [i]     done-label-4 | [f]   global#if#temp | --------------------
[op]            Jump | [i]    break-label-3 | -------------------- | --------------------
[op]       GotoLabel | [i]     done-label-4 | -------------------- | --------------------
[op]            Jump | [i] con...ue-label-2 | -------------------- | --------------------
[op]       GotoLabel | [i]    break-label-3 | -------------------- | --------------------
");

        // The bytecode is a bit jank but eh. It is correct.
        [TestMethod]
        public void TestFor() => TestEmittedOpcodes(@"
for (float i = 0; i < 10; i++) {
    if (i > 5) {
        break;
    }
}
", @"
[op]             Set | [f]                i | [f]                0 | --------------------
[op]             Set | [f]       looptemp#0 | [f]                1 | --------------------
[op]       GotoLabel | [i] con...ue-label-0 | -------------------- | --------------------
[op]             Not | [f]   global#if#temp | [f]       looptemp#0 | --------------------
[op] JumpConditional | [i] fal...ch-label-3 | [f]   global#if#temp | --------------------
[op]             Set | [f]       looptemp#0 | [f]                0 | --------------------
[op]            Jump | [i]     done-label-2 | -------------------- | --------------------
[op]       GotoLabel | [i] fal...ch-label-3 | -------------------- | --------------------
[op]             Add | [f]                i | [f]                1 | [f]                i
[op]       GotoLabel | [i]     done-label-2 | -------------------- | --------------------
[op]        LessThan | [f]   global#if#temp | [f]                i | [f]               10
[op] JumpConditional | [i]     done-label-4 | [f]   global#if#temp | --------------------
[op]            Jump | [i]    break-label-1 | -------------------- | --------------------
[op]       GotoLabel | [i]     done-label-4 | -------------------- | --------------------
[op]   LessThanEqual | [f]   global#if#temp | [f]                i | [f]                5
[op] JumpConditional | [i]     done-label-6 | [f]   global#if#temp | --------------------
[op]            Jump | [i]    break-label-1 | -------------------- | --------------------
[op]       GotoLabel | [i]     done-label-6 | -------------------- | --------------------
[op]            Jump | [i] con...ue-label-0 | -------------------- | --------------------
[op]       GotoLabel | [i]    break-label-1 | -------------------- | --------------------
");

        [TestMethod]
        public void TestWhile() => TestEmittedOpcodes(@"
float i = 0;
while (i < 10) {
    i++;
}
", @"
[op]             Set | [f]                i | [f]                0 | --------------------
[op]       GotoLabel | [i] con...ue-label-0 | -------------------- | --------------------
[op]        LessThan | [f]   global#if#temp | [f]                i | [f]               10
[op] JumpConditional | [i]     done-label-2 | [f]   global#if#temp | --------------------
[op]            Jump | [i]    break-label-1 | -------------------- | --------------------
[op]       GotoLabel | [i]     done-label-2 | -------------------- | --------------------
[op]             Add | [f]                i | [f]                1 | [f]                i
[op]            Jump | [i] con...ue-label-0 | -------------------- | --------------------
[op]       GotoLabel | [i]    break-label-1 | -------------------- | --------------------
");
    }
}