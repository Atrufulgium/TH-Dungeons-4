using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletField;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletScriptVM {
    /// <summary>
    /// Represents a single VM executing bulletscript code.
    /// <br/>
    /// The code can affect one or multiple objects.
    /// </summary>
    // Note: UnsafeX instead of NativeX because we're going to be working with
    // lists of VMs. This makes Unity unhappy and I don't get parallel safety
    // checks anymore.
    public unsafe partial struct VM : IDisposable {
        /// <summary>
        /// The instruction list of the VM. See the documentation.
        /// </summary>
        UnsafeArray<float4> instructions;
        /// <summary>
        /// The memory of the VM. Roughly divided into "built-in" vars,
        /// "user-defined" vars, and "control-flow" vars.
        /// </summary>
        UnsafeArray<float> floatMemory;
        /// <summary>
        /// The predefined strings of the VM. In code, these are referenced by
        /// only their index.
        /// </summary>
        UnsafeArray<FixedString128Bytes> stringMemory;
        /// <summary>
        /// The current instruction.
        /// </summary>
        uint* op;
        /// <summary>
        /// The index from which <see cref="floatMemory"/> contains all control
        /// flow variables.
        /// </summary>
        readonly uint controlFlowMemoryStart;
        /// <summary>
        /// The random state this VM uses.
        /// </summary>
        Unity.Mathematics.Random* rng;

        /// <summary>
        /// All bullets that react to the output of this VM.
        /// </summary>
        UnsafeList<BulletReference> affectedBullets;

        /// <summary>
        /// During execution, certain things cannot be done strictly in the VM
        /// itself. These become commands put into this list.
        /// </summary>
        UnsafeList<Command> outputCommands;

        /// <summary>
        /// Create a VM with specified instructions, memory layout, and strings
        /// memory. In addition, a list of affected bullets is provided as the
        /// VM otherwise won't to a lot.
        /// <br/>
        /// Finally, two pointers must be specified. The lifetimes of the two
        /// pointers and the affectedBullets list are the responsibility of the
        /// caller.
        /// </summary>
        public unsafe VM(
            float4[] instructions,
            int floatMemSize,
            int floatMemControlflowBlockStart,
            ICollection<string> strings,
            UnsafeList<BulletReference> affectedBullets,
            uint* opRef,
            Unity.Mathematics.Random* rngRef
        ) {
            if (floatMemSize < 32)
                throw new ArgumentOutOfRangeException(nameof(floatMemSize), "By specification, any vm uses at least 32 variables.");
            // TODO: OOB-validation.
            this.instructions = new(instructions, Allocator.Persistent);

            floatMemory = new(floatMemSize, Allocator.Persistent);
            floatMemory[8] = 1; // By spec, `autoclear` starts as `true`.
            floatMemory[13] = 1; // By spec, `harmsplayers` starts as `true`.
            floatMemory[15] = 1; // By spec, `spawnspeed` starts as `1`.
            floatMemory[16] = 1; // By spec, `spawnrelative` starts as `true`.
            op = opRef;
            controlFlowMemoryStart = (uint)floatMemControlflowBlockStart;
            
            stringMemory = new(strings.Select(s => new FixedString128Bytes(s)).ToArray(), Allocator.Persistent);

            rng = rngRef;
            outputCommands = new(128, Allocator.Persistent);
            this.affectedBullets = affectedBullets;
        }

        // TODO: maybe one with a ref arg.
        // OTOH, this isn't called *that* often.
        /// <summary>
        /// Makes an exact deep copy of a VM with different affected bullets.
        /// <br/>
        /// Two pointers must be specified. The lifetimes of the two
        /// pointers and the affectedBullets list are the responsibility of the
        /// caller.
        /// </summary>
        public VM(
            VM vm,
            UnsafeList<BulletReference> affectedBullets,
            uint* opRef,
            Unity.Mathematics.Random* rngRef
        ) {
            // As we're based off an existing VM, there is no need to do
            // validation, as the other vm is already valid.
            // No choice but to copy the memory over...
            // TODO: Some form of ref counting to reuse data.
            // This applies to `instructions`, `stringMemory`, and `op`.
            instructions = vm.instructions.Clone(Allocator.Persistent);
            floatMemory = vm.floatMemory.Clone(Allocator.Persistent); // no
            stringMemory = vm.stringMemory.Clone(Allocator.Persistent);
            op = opRef;
            *op = *vm.op;
            controlFlowMemoryStart = vm.controlFlowMemoryStart;
            rng = rngRef;
            uint seed = 0;
            while (seed == 0)
                seed = vm.rng->NextUInt();
            *rng = new(seed);
            this.affectedBullets = affectedBullets;
            outputCommands = new(128, Allocator.Persistent);
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
            stringMemory.Dispose();
            outputCommands.Dispose();
            // AffectedBullets is not our responsibility, but the callers'
        }
    }
}
