using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletEngine;
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
    // checks nor dispose safety checks anymore.
    // This struct is partial with `VMJob.cs` that accesses these members.
    public unsafe partial struct VM : IDisposable {

        /// <summary>
        /// The maximum instruction size. For more information, see
        /// <see cref="opAndCooldown"/>.
        /// </summary>
        public const uint MAX_INSTRS = 0xffffu;

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
        /// The current instruction (bottom half), and cooldown (top half).
        /// <br?>
        /// This limites the instruction count and ticks cooldown to 65536.
        /// </summary>
        uint* opAndCooldown;
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
        internal UnsafeList<BulletReference> affectedBullets;

        /// <summary>
        /// During execution, certain things cannot be done strictly in the VM
        /// itself. These become commands put into this list.
        /// <br/>
        /// When consuming these commands, be sure to also clear this lift
        /// afterwards.
        /// </summary>
        internal UnsafeList<Command> outputCommands;

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
            if (floatMemSize < 12)
                throw new ArgumentOutOfRangeException(nameof(floatMemSize), "By specification, any vm uses at least 12 variables.");
            if (instructions.Length > MAX_INSTRS)
                throw new ArgumentException("The VM only supports programs with up to 65536 instructions.", nameof(instructions));
            // TODO: OOB-validation.
            this.instructions = new(instructions, Allocator.Persistent);

            floatMemory = new(floatMemSize, Allocator.Persistent);
            floatMemory[2] = 1; // By spec, `soawbsoeed` starts as `1`.
            floatMemory[3] = 1; // By spec, `soawbrekatuve` starts as `true`.
            opAndCooldown = opRef;
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
            opAndCooldown = opRef;
            *opAndCooldown = *vm.opAndCooldown;
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
        /// Sets the cooldown of this VM to the given number of ticks.
        /// </summary>
        internal void SetCooldown(ushort duration) {
            *opAndCooldown = (*opAndCooldown & MAX_INSTRS) + duration * MAX_INSTRS;
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
