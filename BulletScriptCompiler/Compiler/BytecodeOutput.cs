using Atrufulgium.BulletScript.Compiler.HighLevelOpCodes;
using Atrufulgium.BulletScript.Compiler.Visitors;

namespace Atrufulgium.BulletScript.Compiler {
    /// <summary>
    /// Represents everything the VM needs to run this BulletScript code.
    /// See the extended documentation for more info.
    /// </summary>
    public class BytecodeOutput {
        /// <summary>
        /// All opcodes and their args.
        /// </summary>
        public readonly (float, float, float, float)[] OpCodes;
        /// <summary>
        /// A sufficiently large pre-initialized memory.
        /// </summary>
        public readonly float[] Memory;
        /// <summary>
        /// All strings this bytecode references.
        /// </summary>
        public readonly string[] Strings;

        internal BytecodeOutput(EmitWalker emitter) {
            // We have three passes:
            // (1) Prepare the data the HLOPs need for conversion into LLOPs
            // (2) Actually do this conversion, which is easy
            // (3) Prepare the VM memory.
            var opcodes = emitter.OPCodes;

            Dictionary<string, float> explicitGotoTargets = new();
            Dictionary<string, float> explicitVariableIDs = new(emitter.ExplicitVariableIDs);
            Dictionary<string, float> explicitStringIDs = new() {
                { "error", 0 }
            };

            // First find all goto labels and strings
            int instruction = 0;
            var gotoLabelOpcode = HLOP.GotoLabel("").opcode;
            foreach (var op in opcodes) {
                // Goto
                // (Subtract 1 because the IP increases after every op)
                if (op.opcode == gotoLabelOpcode)
                    explicitGotoTargets.Add(((InstructionRef)op.arg1).Label, instruction - 1);
                if (op.opcode >= 0) // (negative HLOPs don't map to LLOPs)
                    instruction++;

                // Strings
                foreach (var arg in op) {
                    if (arg is StringRef str
                        && !explicitStringIDs.ContainsKey(str.id)) {
                        explicitStringIDs.Add(str.id, explicitStringIDs.Count);
                    }
                }
            }

            // Now go through all opcodes to create the bytecode array
            // (shut up bytecode is a state of mind, `float4code` just doesn't
            //  have the same ring to it)
            List<(float, float, float, float)> llops = new();
            foreach (var op in opcodes) {
                var res = op.ToLowLevel(explicitGotoTargets, explicitVariableIDs, explicitStringIDs);
                if (res != null)
                    llops.Add(res.Value);
            }
            OpCodes = llops.ToArray();

            // Now add all memory: floats
            // Bad software design: Be sure to keep this in sync with
            /// <see cref="Semantics.IntrinsicData"/>
            /// <see cref="EmitWalker.ExplicitVariableIDs"/>
            Memory = new float[(int)explicitVariableIDs.Values.Max()];
            Memory[2] = 1f; // `spawnspeed` inits to 1
            Memory[3] = 1f; // `spawnrelative` inits to 1

            // Now add all memory: strings
            // (When is c# getting a built-in bidict?)
            SortedDictionary<int, string> reverseStringIDs = new();
            foreach (var kv in explicitStringIDs) {
                reverseStringIDs.Add((int)kv.Value, kv.Key);
            }
            Strings = reverseStringIDs.Select(kv => kv.Value).ToArray();
        }
    }
}
