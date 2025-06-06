using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;

using static Atrufulgium.EternalDreamCatcher.Tests.TestHelpers;

namespace Atrufulgium.EternalDreamCatcher.BulletScriptVM.Tests {
    public class VMTests {

        /// <summary>
        /// Tests whether a small vm with 32 floats of memory, some list of
        /// instructions, and no strings, generates the expected commands.
        /// </summary>
        unsafe void Test(float4[] instructions, string expectedCommands) {
            uint* uintRef = stackalloc uint[1];
            Unity.Mathematics.Random* rngRef = stackalloc Unity.Mathematics.Random[1];
            using var vm = new VM(instructions, 32, 32, Array.Empty<string>(), default, uintRef, rngRef);
            
            vm.RunMain();

            // UnsafeList implements IEnumerable with NotImplementException. Loveit.
            List<Command> safeCommands = new();
            for (int i = 0; i < vm.outputCommands.Length; i++)
                safeCommands.Add(vm.outputCommands[i]);
            
            AssertTrimmedStringsEqual(
                expectedCommands,
                string.Join('\n', safeCommands)
            );
        }

        unsafe float Int(int value) => *(float*)&value;
        const float NA = float.NaN;

        [Test]
        public void Test10Messages() => Test(new float4[] {
// mem[0] = 10f
new(Int(10), Int(0), 10f, NA),
// SendMessage(mem[0])
// label: MESSAGE
new(Int(32), Int(0), NA, NA),
// mem[0]--
new(Int(84), Int(0), Int(0), 1),
// mem[1] = 0 < mem[0]
new(Int(4), Int(1), 0f, Int(0)),
// if mem[1] == true, goto MESSAGE
new(Int(20), Int(1-1), Int(1), NA), // (recall the -1 with jumps)
}, @"
             SendMessage |           10 | ------------ | ------------
             SendMessage |            9 | ------------ | ------------
             SendMessage |            8 | ------------ | ------------
             SendMessage |            7 | ------------ | ------------
             SendMessage |            6 | ------------ | ------------
             SendMessage |            5 | ------------ | ------------
             SendMessage |            4 | ------------ | ------------
             SendMessage |            3 | ------------ | ------------
             SendMessage |            2 | ------------ | ------------
             SendMessage |            1 | ------------ | ------------
");
    }
}

