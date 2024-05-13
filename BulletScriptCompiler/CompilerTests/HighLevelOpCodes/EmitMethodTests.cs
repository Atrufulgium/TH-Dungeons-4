using static Atrufulgium.BulletScript.Compiler.Tests.Helpers.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.HighLevelOpCodes.Tests {

    [TestClass]
    public class EmitMethodTests {

        // This tests way too many things at once
        // - you can smoothly sail from initialisation into `main`
        // - compiler important labels and variables are correct
        // - method extraction in general works as in the syntax tree version
        // - methods go to the end as expected
        [TestMethod]
        public void Test() => TestEmittedOpcodes(@"
float my_number = 3;
function float A(float v) { return v + my_number; }
function void on_message(float v) { main(v); }
function void main(float v) { spawnspeed = A(2); }
", @"
[op]             Set | [f]        my_number | [f]                3 | --------------------
[op]       GotoLabel | [i]  ##main(float)## | -------------------- | --------------------
[op]             Set | [f]       A(float)#v | [f]                2 | --------------------
[op]             Set | [f]  A-float()#entry | [f]                1 | --------------------
[op]            Jump | [i]        A-float() | -------------------- | --------------------
[op]       GotoLabel | [i] A-f...to-entry#1 | -------------------- | --------------------
[op]             Set | [f]       spawnspeed | [f] A-f...t()#return | --------------------
[op]           Equal | [f] glo...returntest | [f]                1 | [f] mai...oat)#entry
[op] JumpConditional | [i] mai...to-entry#1 | [f] glo...returntest | --------------------
[op]            Jump | [i]          the end | -------------------- | --------------------
[op]       GotoLabel | [i]        A-float() | -------------------- | --------------------
[op]             Add | [f] A-f...t()#return | [f]       A(float)#v | [f]        my_number
[op]           Equal | [f] glo...returntest | [f]                1 | [f]  A-float()#entry
[op] JumpConditional | [i] A-f...to-entry#1 | [f] glo...returntest | --------------------
[op]       GotoLabel | [i] ##o...e(float)## | -------------------- | --------------------
[op]             Set | [f]  ##main(float)## | [f] ##o...e(float)## | --------------------
[op]             Set | [f] mai...oat)#entry | [f]                1 | --------------------
[op]            Jump | [i]  ##main(float)## | -------------------- | --------------------
[op]       GotoLabel | [i] mai...to-entry#1 | -------------------- | --------------------
[op]       GotoLabel | [i]          the end | -------------------- | --------------------
");

    }
}