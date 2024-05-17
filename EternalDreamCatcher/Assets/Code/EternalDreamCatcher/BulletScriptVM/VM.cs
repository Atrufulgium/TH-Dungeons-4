using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletScriptVM {
    /// <summary>
    /// Represents a single VM executing bulletscript code.
    /// <br/>
    /// The code can affect one or multiple objects.
    /// </summary>
    public partial class VM : IDisposable {
        /// <summary>
        /// The instruction list of the VM. See the documentation.
        /// </summary>
        NativeArray<float4> instructions;
        /// <summary>
        /// The memory of the VM. Roughly divided into "built-in" vars,
        /// "user-defined" vars, and "control-flow" vars.
        /// </summary>
        NativeArray<float> floatMemory;
        /// <summary>
        /// The predefined strings of the VM. In code, these are referenced by
        /// only their index.
        /// </summary>
        readonly string[] stringMemory;
        /// <summary>
        /// The current instruction.
        /// </summary>
        NativeReference<uint> op;
        /// <summary>
        /// The index from which <see cref="floatMemory"/> contains all control
        /// flow variables.
        /// </summary>
        readonly uint controlFlowMemoryStart;
        /// <summary>
        /// The random state this VM uses.
        /// </summary>
        Unity.Mathematics.Random rng;

        /// <summary>
        /// During execution, certain things cannot be done strictly in the VM
        /// itself. These become commands put into this list.
        /// </summary>
        NativeList<Command> outputCommands;

        public VM(float4[] instructions, int floatMemSize, int floatMemControlflowBlockStart, IEnumerable<string> strings, Unity.Mathematics.Random rng = default) {
            if (floatMemSize < 32)
                throw new ArgumentOutOfRangeException(nameof(floatMemSize), "By specification, any vm uses at least 32 variables.");
            // TODO: OOB-validation.
            this.instructions = new(instructions, Allocator.Persistent);
            floatMemory = new(floatMemSize, Allocator.Persistent);
            floatMemory[8] = 1; // By spec, `autoclear` starts as `true`.
            floatMemory[13] = 1; // By spec, `harmsplayers` starts as `true`.
            floatMemory[15] = 1; // By spec, `spawnspeed` starts as `1`.
            floatMemory[16] = 1; // By spec, `spawnrelative` starts as `true`.
            op = new(0, Allocator.Persistent);
            controlFlowMemoryStart = (uint)floatMemControlflowBlockStart;
            stringMemory = strings.ToArray();
            this.rng = rng;
            outputCommands = new(Allocator.Persistent);
        }

        /// <summary>
        /// Returns and clears all commands.
        /// <br/>
        /// Remark: Do <b>not</b> use <c>break;</c> with this.
        /// </summary>
        internal IEnumerable<Command> ConsumeCommands() {
            foreach (var i in outputCommands)
                yield return i;
            outputCommands.Clear();
        }

        public void Dispose() {
            instructions.Dispose();
            floatMemory.Dispose();
            op.Dispose();
            outputCommands.Dispose();
        }
    }
}
